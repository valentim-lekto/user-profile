#!/bin/sh

set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/.." && pwd)
cd "$repository_root"

review_tmp=$(mktemp -d)
stack_started=0

cleanup() {
  if [ "$stack_started" -eq 1 ]; then
    docker compose down >/dev/null 2>&1 || true
  fi

  rm -f -- "$review_tmp/body" "$review_tmp/headers" "$review_tmp/nginx"
  rmdir "$review_tmp" 2>/dev/null || true
}

fail() {
  printf 'M1 Compose validation failed: %s\n' "$1" >&2
  exit 1
}

request() {
  request_path=$1
  expected_status=$2
  expected_media_type=$3

  actual_status=$(curl --silent --show-error --max-time 15 \
    --output "$review_tmp/body" \
    --dump-header "$review_tmp/headers" \
    --write-out '%{http_code}' \
    "http://127.0.0.1:8080$request_path")

  [ "$actual_status" = "$expected_status" ] ||
    fail "$request_path returned $actual_status instead of $expected_status"

  actual_media_type=$(sed -n 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//p' "$review_tmp/headers" |
    tr -d '\r' |
    sed -n '1{s/[[:space:]]*;.*$//;p;}')

  [ "$actual_media_type" = "$expected_media_type" ] ||
    fail "$request_path returned media type $actual_media_type instead of $expected_media_type"
}

trap cleanup EXIT HUP INT TERM

docker compose config --quiet

compose_images=$(docker compose config --images | sort)
expected_compose_images=$(printf '%s\n' 'user-profile-api:0.1.0' 'user-profile-web:0.1.0')
[ "$compose_images" = "$expected_compose_images" ] ||
  fail "unexpected Compose images: $compose_images"

grep -Fqx 'FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build' \
  src/backend/UserProfile.Api/Dockerfile || fail 'unexpected .NET SDK image'
grep -Fqx 'FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final' \
  src/backend/UserProfile.Api/Dockerfile || fail 'unexpected ASP.NET runtime image'
grep -Fqx 'FROM node:24.19.0-bookworm-slim AS build' \
  src/frontend/user-profile-web/Dockerfile || fail 'unexpected Node image'
grep -Fqx 'FROM nginx:1.30.4-alpine3.24-slim' \
  src/frontend/user-profile-web/Dockerfile || fail 'unexpected Nginx image'

if grep -Eq '^FROM .*:(latest|stable|lts)([[:space:]]|$)' \
  src/backend/UserProfile.Api/Dockerfile src/frontend/user-profile-web/Dockerfile; then
  fail 'floating Docker image tag found'
fi

if grep -Eq '^[A-Za-z_][A-Za-z0-9_]*=.+$' .env.example; then
  fail '.env.example contains a usable value'
fi

stack_started=1
docker compose up --build --detach --wait --wait-timeout "${M1_COMPOSE_WAIT_TIMEOUT:-300}"

api_container_id=$(docker compose ps -q api)
published_api_ports=$(docker inspect --format \
  '{{range $port, $bindings := .NetworkSettings.Ports}}{{if $bindings}}{{$port}} {{end}}{{end}}' \
  "$api_container_id")
[ -z "$published_api_ports" ] || fail "API publishes a host port: $published_api_ports"

api_user=$(docker compose exec -T api id -u)
[ "$api_user" != '0' ] || fail 'API runs as root'

request '/' '200' 'text/html'
grep -Fq '<app-root' "$review_tmp/body" || fail 'SPA shell was not served'

request '/review-smoke-route' '200' 'text/html'
grep -Fq '<app-root' "$review_tmp/body" || fail 'SPA fallback was not served'

request '/health' '200' 'application/json'
grep -Fq '"status":"Healthy"' "$review_tmp/body" || fail 'health body is unexpected'

request '/swagger/index.html' '200' 'text/html'
request '/swagger/v1/swagger.json' '200' 'application/json'
grep -Fq '"/health"' "$review_tmp/body" || fail 'runtime OpenAPI omits /health'

request '/api/not-implemented' '404' 'application/problem+json'
grep -Fq '"status":404' "$review_tmp/body" || fail '404 body is not ProblemDetails'

docker compose exec -T web nginx -T >"$review_tmp/nginx" 2>&1
grep -Eq 'error_page[[:space:]]+502[[:space:]]+504[[:space:]]+=[[:space:]]+@service_unavailable;' \
  "$review_tmp/nginx" || fail 'Nginx does not map both 502 and 504'

docker compose stop api >/dev/null
request '/health' '503' 'application/problem+json'
grep -Fq '"status":503' "$review_tmp/body" || fail 'upstream failure body is not ProblemDetails'

printf 'M1 Compose OK: same origin, internal API, health, Swagger, 404 and upstream 503 verified\n'
