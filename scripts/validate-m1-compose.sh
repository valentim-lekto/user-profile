#!/bin/sh

set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/.." && pwd)
cd "$repository_root"

review_tmp=$(mktemp -d)
stack_started=0
COMPOSE_PROJECT_NAME="user-profile-m3-smoke-$$"
export COMPOSE_PROJECT_NAME

cleanup() {
  if [ "$stack_started" -eq 1 ]; then
    docker compose down --volumes --remove-orphans >/dev/null 2>&1 || true
  fi

  rm -f -- \
    "$review_tmp/body" \
    "$review_tmp/headers" \
    "$review_tmp/invalid-login-body" \
    "$review_tmp/logs" \
    "$review_tmp/nginx" \
    "$review_tmp/oversized" \
    "$review_tmp/request"
  rmdir "$review_tmp" 2>/dev/null || true
}

fail() {
  printf 'M1+M2+M3 Compose validation failed: %s\n' "$1" >&2
  exit 1
}

request() {
  request_path=$1
  expected_status=$2
  expected_media_type=$3
  shift 3

  actual_status=$(curl --silent --show-error --max-time 30 \
    --output "$review_tmp/body" \
    --dump-header "$review_tmp/headers" \
    --write-out '%{http_code}' \
    "$@" \
    "http://127.0.0.1:8080$request_path")

  [ "$actual_status" = "$expected_status" ] ||
    fail "$request_path returned $actual_status instead of $expected_status"

  actual_media_type=$(sed -n 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//p' "$review_tmp/headers" |
    tr -d '\r' |
    sed -n '1{s/[[:space:]]*;.*$//;p;}')

  [ "$actual_media_type" = "$expected_media_type" ] ||
    fail "$request_path returned media type $actual_media_type instead of $expected_media_type"
}

post_json() {
  post_path=$1
  post_status=$2
  post_body=$3

  printf '%s' "$post_body" >"$review_tmp/request"
  request "$post_path" "$post_status" 'application/problem+json' \
    --request POST \
    --header 'Content-Type: application/json' \
    --data-binary "@$review_tmp/request"
}

wait_for_health() {
  health_attempt=0
  while [ "$health_attempt" -lt 60 ]; do
    if curl --silent --fail --max-time 3 \
      --output /dev/null http://127.0.0.1:8080/health; then
      return 0
    fi

    health_attempt=$((health_attempt + 1))
    sleep 2
  done

  fail 'health did not recover after recreating the API'
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

if grep -Eq '\$(request|request_uri|args|query_string|request_body|http_authorization)([^A-Za-z0-9_]|$)' \
  src/frontend/user-profile-web/nginx.conf; then
  fail 'Nginx logging references request data that can contain credentials'
fi

if grep -Eq '"Microsoft\.AspNetCore\.Hosting\.Diagnostics"[[:space:]]*:[[:space:]]*"(Trace|Debug|Information)"' \
  src/backend/UserProfile.Api/appsettings.json; then
  fail 'ASP.NET request diagnostics can log query strings at the configured level'
fi

grep -Eq 'client_max_body_size[[:space:]]+1m;' \
  src/frontend/user-profile-web/nginx.conf || fail 'Nginx request-body limit is not explicit'
grep -Eq 'error_page[[:space:]]+413[[:space:]]+=[[:space:]]+@payload_too_large;' \
  src/frontend/user-profile-web/nginx.conf || fail 'Nginx does not map 413 to ProblemDetails'

stack_started=1
docker compose up --build --detach --wait --wait-timeout "${M1_COMPOSE_WAIT_TIMEOUT:-300}"

data_volume="${COMPOSE_PROJECT_NAME}_user-profile-data"
docker volume inspect "$data_volume" >/dev/null 2>&1 ||
  fail 'the isolated SQLite volume was not created'

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
grep -Fq '"/api/auth/register"' "$review_tmp/body" || fail 'runtime OpenAPI omits registration'
grep -Fq '"/api/auth/login"' "$review_tmp/body" || fail 'runtime OpenAPI omits login'
grep -Fq '"/api/profile"' "$review_tmp/body" || fail 'runtime OpenAPI omits profile'
grep -Fq '"bearerAuth"' "$review_tmp/body" || fail 'runtime OpenAPI omits Bearer authentication'

request '/api/not-implemented' '404' 'application/problem+json'
grep -Fq '"status":404' "$review_tmp/body" || fail '404 body is not ProblemDetails'

smoke_suffix="$(date +%s)-$$"
smoke_email="m3-smoke-$smoke_suffix@example.test"
smoke_password="M3-smoke-$smoke_suffix-Aa1!"
registration_payload=$(printf \
  '{"name":"M3 Smoke","email":"%s","password":"%s","passwordConfirmation":"%s"}' \
  "$smoke_email" "$smoke_password" "$smoke_password")

printf '%s' "$registration_payload" >"$review_tmp/request"
request '/api/auth/register' '201' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
grep -Fq '"message"' "$review_tmp/body" || fail 'registration response is unexpected'

post_json '/api/auth/register' '400' '{}'
grep -Fq '"status":400' "$review_tmp/body" || fail 'invalid registration is not ProblemDetails'

uppercase_email=$(printf '%s' "$smoke_email" | tr '[:lower:]' '[:upper:]')
duplicate_payload=$(printf \
  '{"name":"M3 Duplicate","email":"  %s  ","password":"%s","passwordConfirmation":"%s"}' \
  "$uppercase_email" "$smoke_password" "$smoke_password")
post_json '/api/auth/register' '409' "$duplicate_payload"
grep -Fq '"status":409' "$review_tmp/body" || fail 'duplicate registration is not ProblemDetails'

login_payload=$(printf \
  '{"email":"  %s  ","password":"%s"}' \
  "$uppercase_email" "$smoke_password")
printf '%s' "$login_payload" >"$review_tmp/request"
request '/api/auth/login' '200' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
access_token=$(sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p' "$review_tmp/body")
[ -n "$access_token" ] || fail 'valid login did not return an access token'

missing_email_payload=$(printf \
  '{"email":"missing-%s@example.test","password":"%s"}' \
  "$smoke_suffix" "$smoke_password")
printf '%s' "$missing_email_payload" >"$review_tmp/request"
request '/api/auth/login' '401' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
cp "$review_tmp/body" "$review_tmp/invalid-login-body"
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'invalid login omits the Bearer challenge'

wrong_password_payload=$(printf \
  '{"email":"%s","password":"wrong-%s"}' \
  "$smoke_email" "$smoke_suffix")
printf '%s' "$wrong_password_payload" >"$review_tmp/request"
request '/api/auth/login' '401' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
cmp -s "$review_tmp/body" "$review_tmp/invalid-login-body" ||
  fail 'unknown email and wrong password return different bodies'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'wrong-password login omits the Bearer challenge'

request '/api/profile' '401' 'application/problem+json'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'unauthenticated profile omits the Bearer challenge'

request '/api/profile' '401' 'application/problem+json' \
  --header 'Authorization: Bearer synthetically-invalid-token'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'invalid-token profile omits the Bearer challenge'

request '/api/profile?userId=00000000-0000-0000-0000-000000000000' '200' 'application/json' \
  --header "Authorization: Bearer $access_token" \
  --header 'X-User-Id: 00000000-0000-0000-0000-000000000000'
grep -Fq "\"email\":\"$smoke_email\"" "$review_tmp/body" ||
  fail 'profile was not resolved from the authenticated subject'
grep -Fq '"id"' "$review_tmp/body" || fail 'profile response omits id'
grep -Fq '"name":"M3 Smoke"' "$review_tmp/body" || fail 'profile response omits name'
if grep -Eiq 'password|normalizedEmail|createdAt|updatedAt' "$review_tmp/body"; then
  fail 'profile response exposes an internal field'
fi

printf '%s' 'not-json' >"$review_tmp/request"
request '/api/auth/register' '415' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: text/plain' \
  --data-binary "@$review_tmp/request"
grep -Fq '"status":415' "$review_tmp/body" || fail 'unsupported media type is not ProblemDetails'

query_marker="M3_QUERY_MARKER_$smoke_suffix"
body_marker="M3_BODY_MARKER_$smoke_suffix"
header_marker="M3_HEADER_MARKER_$smoke_suffix"
marker_payload=$(printf \
  '{"name":"M3 Marker","email":"marker-%s@example.test","password":"%s","passwordConfirmation":"different"}' \
  "$smoke_suffix" "$body_marker")
printf '%s' "$marker_payload" >"$review_tmp/request"
request "/api/auth/register?password=$query_marker" '400' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $header_marker" \
  --data-binary "@$review_tmp/request"

sleep 1
docker compose logs --no-color >"$review_tmp/logs"
if grep -Fq -- "$query_marker" "$review_tmp/logs" ||
  grep -Fq -- "$body_marker" "$review_tmp/logs" ||
  grep -Fq -- "$header_marker" "$review_tmp/logs" ||
  grep -Fq -- "$access_token" "$review_tmp/logs"; then
  fail 'a synthetic query, body, header or JWT marker was exposed in container logs'
fi

dd if=/dev/zero of="$review_tmp/oversized" bs=1048577 count=1 2>/dev/null
request '/api/auth/register' '413' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/oversized"
grep -Fq '"status":413' "$review_tmp/body" || fail 'oversized request is not ProblemDetails'

docker compose exec -T web nginx -T >"$review_tmp/nginx" 2>&1
grep -Eq 'error_page[[:space:]]+502[[:space:]]+504[[:space:]]+=[[:space:]]+@service_unavailable;' \
  "$review_tmp/nginx" || fail 'Nginx does not map both 502 and 504'
grep -Eq 'error_page[[:space:]]+413[[:space:]]+=[[:space:]]+@payload_too_large;' \
  "$review_tmp/nginx" || fail 'rendered Nginx config does not map 413'

docker compose up --detach --force-recreate api >/dev/null
wait_for_health
post_json '/api/auth/register' '409' "$duplicate_payload"
grep -Fq '"status":409' "$review_tmp/body" ||
  fail 'registration did not persist after recreating the API'

request '/api/profile' '401' 'application/problem+json' \
  --header "Authorization: Bearer $access_token"
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'old process-token rejection omitted the Bearer challenge'

printf '%s' "$login_payload" >"$review_tmp/request"
request '/api/auth/login' '200' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
recreated_access_token=$(sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p' "$review_tmp/body")
[ -n "$recreated_access_token" ] ||
  fail 'persisted credentials could not create a new session after recreating the API'
[ "$recreated_access_token" != "$access_token" ] ||
  fail 'recreating the API did not rotate the process-local Development token'

request '/api/profile' '200' 'application/json' \
  --header "Authorization: Bearer $recreated_access_token"
grep -Fq "\"email\":\"$smoke_email\"" "$review_tmp/body" ||
  fail 'the persisted profile could not be read after recreating the API'

docker compose stop api >/dev/null
request '/health' '503' 'application/problem+json'
grep -Fq '"status":503' "$review_tmp/body" || fail 'upstream failure body is not ProblemDetails'

printf '%s\n' \
  'M1+M2+M3 Compose OK: same origin, registration, login, protected profile, auth failures, persistence, 413/415, safe logs and upstream 503 verified'
