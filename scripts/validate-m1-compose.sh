#!/bin/sh

set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/.." && pwd)
cd "$repository_root"
sanitizer_script="$repository_root/scripts/sanitize-ci-output.sh"

review_tmp=$(mktemp -d)
stack_started=0
run_succeeded=0
compose_project_prefix="${COMPOSE_PROJECT_NAME:-user-profile-m4}"
COMPOSE_PROJECT_NAME="${compose_project_prefix}-smoke-$$"
export COMPOSE_PROJECT_NAME
failure_artifacts_root="${SMOKE_FAILURE_ARTIFACTS_DIR:-$repository_root/artifacts/ci/smoke}"
failure_artifacts_dir="$failure_artifacts_root/$COMPOSE_PROJECT_NAME"
mkdir -p "$failure_artifacts_dir"
printf '%s\n' "$COMPOSE_PROJECT_NAME" >"$failure_artifacts_dir/compose-project.txt"

collect_failure_diagnostics() {
  failure_status=$1

  printf 'project=%s\nexit_status=%s\nstack_started=%s\n' \
    "$COMPOSE_PROJECT_NAME" "$failure_status" "$stack_started" \
    >"$failure_artifacts_dir/context.txt"
  docker compose config --services \
    >"$failure_artifacts_dir/compose-services.txt" 2>&1 || true
  docker compose config --images \
    >"$failure_artifacts_dir/compose-images.txt" 2>&1 || true

  if [ "$stack_started" -eq 1 ]; then
    docker compose ps --all \
      >"$failure_artifacts_dir/compose-ps.txt" 2>&1 || true
    docker compose logs --no-color api web \
      >"$review_tmp/failure-logs" 2>&1 || true
    sh "$sanitizer_script" \
      <"$review_tmp/failure-logs" \
      >"$failure_artifacts_dir/compose.log"
  fi
}

cleanup() {
  cleanup_status=$?
  teardown_status=0
  trap - EXIT HUP INT TERM

  if [ "$run_succeeded" -eq 0 ]; then
    collect_failure_diagnostics "$cleanup_status" || true
  fi

  if [ "$stack_started" -eq 1 ]; then
    teardown_output=''
    set +e
    teardown_output=$(docker compose down --volumes --remove-orphans 2>&1)
    teardown_status=$?
    set -e

    if [ "$teardown_status" -ne 0 ]; then
      printf '%s\n' "$teardown_output" |
        sh "$sanitizer_script" >"$failure_artifacts_dir/teardown.log"
    fi
  fi

  rm -f -- \
    "$review_tmp/body" \
    "$review_tmp/failure-logs" \
    "$review_tmp/headers" \
    "$review_tmp/invalid-login-body" \
    "$review_tmp/logs" \
    "$review_tmp/nginx" \
    "$review_tmp/oversized" \
    "$review_tmp/request"
  rmdir "$review_tmp" 2>/dev/null || true

  if [ "$teardown_status" -ne 0 ]; then
    printf 'M1+M2+M3+M4 Compose teardown returned %s\n' "$teardown_status" >&2
  fi

  final_status=$(sh "$repository_root/scripts/resolve-cleanup-status.sh" \
    "$cleanup_status" "$teardown_status")
  exit "$final_status"
}

fail() {
  printf 'M1+M2+M3+M4 Compose validation failed: %s\n' "$1" >&2
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

trap cleanup EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

docker compose \
  --profile backend-tests \
  --profile mutation-tests \
  --profile frontend-tests \
  --profile contract-tests \
  --profile e2e \
  config --quiet

rendered_compose=$(docker compose config)
printf '%s\n' "$rendered_compose" |
  grep -Fq 'ConnectionStrings__Default: Data Source=/data/user-profile.db;Default Timeout=5' ||
  fail 'SQLite lock timeout must be 5 seconds and remain below the proxy timeout'

compose_services=$(docker compose \
  --profile backend-tests \
  --profile mutation-tests \
  --profile frontend-tests \
  --profile contract-tests \
  --profile e2e \
  config --services | sort)
expected_compose_services=$(printf '%s\n' \
  'api' \
  'backend-tests' \
  'contract-tests' \
  'e2e' \
  'frontend-tests' \
  'mutation-tests' \
  'web' \
  'web-e2e')
[ "$compose_services" = "$expected_compose_services" ] ||
  fail "unexpected Compose services: $compose_services"

compose_images=$(docker compose \
  --profile backend-tests \
  --profile mutation-tests \
  --profile frontend-tests \
  --profile contract-tests \
  --profile e2e \
  config --images | sort -u)
expected_compose_images=$(printf '%s\n' \
  'ruby:3.4.10-slim-bookworm' \
  'user-profile-api:0.1.0' \
  'user-profile-backend-tests:0.1.0' \
  'user-profile-e2e-tests:0.1.0' \
  'user-profile-frontend-tests:0.1.0' \
  'user-profile-mutation-tests:0.1.0' \
  'user-profile-web:0.1.0')
[ "$compose_images" = "$expected_compose_images" ] ||
  fail "unexpected Compose images: $compose_images"

backend_froms=$(grep '^FROM ' src/backend/UserProfile.Api/Dockerfile)
expected_backend_froms=$(printf '%s\n' \
  'FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build' \
  'FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS test' \
  'FROM test AS mutation-test' \
  'FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final')
[ "$backend_froms" = "$expected_backend_froms" ] || fail 'unexpected backend FROM stages'

frontend_froms=$(grep '^FROM ' src/frontend/user-profile-web/Dockerfile)
expected_frontend_froms=$(printf '%s\n' \
  'FROM node:24.19.0-bookworm-slim AS dependencies' \
  'FROM dependencies AS test' \
  'FROM dependencies AS build' \
  'FROM nginx:1.30.4-alpine3.24-slim')
[ "$frontend_froms" = "$expected_frontend_froms" ] || fail 'unexpected frontend FROM stages'

e2e_froms=$(grep '^FROM ' tests/e2e/Dockerfile)
[ "$e2e_froms" = 'FROM mcr.microsoft.com/playwright:v1.62.0-noble' ] ||
  fail 'unexpected Playwright FROM stage'

grep -Eq '^[[:space:]]+image:[[:space:]]+ruby:3\.4\.10-slim-bookworm$' \
  compose.yaml || fail 'unexpected Ruby contract-validator image'

grep -Fqx 'strict-allow-scripts=true' \
  src/frontend/user-profile-web/.npmrc || fail 'npm install-script allowlist is not strict'
grep -Fqx 'RUN test "$(npm --version)" = "11.17.0" && npm ci' \
  src/frontend/user-profile-web/Dockerfile || fail 'unexpected npm runtime or install command'

if grep -Fq 'coverlet.collector' \
  tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj \
  tests/backend/UserProfile.Api.IntegrationTests/packages.lock.json; then
  fail 'unused coverage collector remains in the backend test dependency graph'
fi

workflow_uses=$(sed -n \
  -e 's/^[[:space:]]*-[[:space:]]*uses:[[:space:]]*//p' \
  -e 's/^[[:space:]]*uses:[[:space:]]*//p' \
  .github/workflows/ci.yml \
  .github/workflows/mutation.yml | sort -u)
expected_workflow_uses=$(printf '%s\n' \
  'actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2' \
  'actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1')
[ "$workflow_uses" = "$expected_workflow_uses" ] ||
  fail "unexpected or unpinned workflow Actions: $workflow_uses"

checkout_credentials_count=$(awk '
  /^[[:space:]]*- uses: actions\/checkout@/ { in_checkout = 1; next }
  in_checkout && /^[[:space:]]*- (uses:|name:)/ { in_checkout = 0 }
  in_checkout && /^[[:space:]]+persist-credentials:[[:space:]]+false([[:space:]]*#.*)?$/ {
    count++
  }
  END { print count + 0 }
' .github/workflows/ci.yml .github/workflows/mutation.yml)
checkout_count=$(grep -hEc '^[[:space:]]*- uses: actions/checkout@' \
  .github/workflows/ci.yml .github/workflows/mutation.yml | \
  awk '{ total += $1 } END { print total + 0 }')
[ "$checkout_credentials_count" -eq "$checkout_count" ] ||
  fail 'checkout persists Git credentials'

grep -Fq '"version": "4.16.0"' .config/dotnet-tools.json ||
  fail 'dotnet-stryker is not fixed at 4.16.0'
[ -x scripts/run-mutation-tests.sh ] || fail 'mutation runner is not executable'
[ -x scripts/validate-mutation-report.sh ] || fail 'mutation report gate is not executable'
grep -Fq 'CMD ["/src/scripts/run-mutation-tests.sh", "--output", "/artifacts", "--skip-version-check"]' \
  src/backend/UserProfile.Api/Dockerfile ||
  fail 'mutation target does not run the report gate'
grep -Fq '"mutation-level": "standard"' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
  fail 'Stryker mutation level is not standard'
grep -Fq '"coverage-analysis": "perTest"' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
  fail 'Stryker coverage analysis is not perTest'
grep -Fq '"concurrency": 2' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
  fail 'Stryker does not use exactly two workers'
grep -Fq '"additional-timeout": 5000' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
  fail 'Stryker additional timeout is not five seconds'
grep -Fq '"break-on-initial-test-failure": true' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
  fail 'Stryker does not fail on an initially red suite'
for threshold in high low break; do
  grep -Fq "\"$threshold\": 97" \
    tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
    fail "unexpected Stryker $threshold threshold"
done
for reporter in progress html json; do
  grep -Fq "\"$reporter\"" \
    tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json ||
    fail "missing Stryker $reporter reporter"
done
if grep -Eq '"ignore-(mutations|methods)"' \
  tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json; then
  fail 'Stryker contains a global mutation ignore'
fi

mutate_files=$(sed -n '/"mutate": \[/,/^[[:space:]]*\]/{
  s/^[[:space:]]*"\([^"]*\.cs\)"[,]*$/\1/p
}' tests/backend/UserProfile.Api.IntegrationTests/stryker-config.json)
expected_mutate_files=$(printf '%s\n' \
  'Features/Auth/AuthController.cs' \
  'Features/Auth/LoginRequest.cs' \
  'Features/Auth/RegisterRequest.cs' \
  'Features/Profile/ProfileController.cs' \
  'Features/Profile/ChangePasswordRequest.cs' \
  'Features/Profile/UpdateProfileRequest.cs' \
  'Security/JwtBearerConfiguration.cs' \
  'Security/JwtTokenIssuer.cs' \
  'Configuration/JwtOptions.cs' \
  'Data/DatabaseHealthCheck.cs' \
  'Data/UserConfiguration.cs')
[ "$mutate_files" = "$expected_mutate_files" ] ||
  fail 'unexpected Stryker mutation allowlist'

grep -Fq 'cron: "0 6 * * 1"' .github/workflows/mutation.yml ||
  fail 'unexpected mutation workflow schedule'
if grep -Eq '^[[:space:]]+(push|pull_request):' \
  .github/workflows/mutation.yml; then
  fail 'mutation testing unexpectedly blocks push or pull requests'
fi

sanitizer_marker='SYNTHETIC_CI_SECRET_MARKER'
sanitizer_jwt='eyJzeW50aGV0aWMiOiJ0ZXN0In0.eyJzdWIiOiJ0ZXN0In0.c3ludGhldGljLXNpZ25hdHVyZQ'
sanitized_probe=$(printf '%s\n' \
  "Authorization: Bearer $sanitizer_marker" \
  "{\"password\":\"$sanitizer_marker\",\"accessToken\":\"$sanitizer_jwt\"}" \
  "password=$sanitizer_marker Jwt__SigningKey=$sanitizer_marker" |
  sh "$sanitizer_script")
if printf '%s' "$sanitized_probe" | grep -Fq "$sanitizer_marker" ||
  printf '%s' "$sanitized_probe" | grep -Fq "$sanitizer_jwt"; then
  fail 'CI log sanitizer retained a synthetic secret marker'
fi
printf '%s' "$sanitized_probe" | grep -Fq '[REDACTED]' ||
  fail 'CI log sanitizer did not emit a redaction marker'

[ "$(sh scripts/resolve-cleanup-status.sh 0 17)" = '17' ] ||
  fail 'teardown failure does not fail a successful run'
[ "$(sh scripts/resolve-cleanup-status.sh 23 17)" = '23' ] ||
  fail 'teardown failure overwrites the primary failure'

if grep -Eq '^FROM .*:(latest|stable|lts)([[:space:]]|$)' \
  src/backend/UserProfile.Api/Dockerfile \
  src/frontend/user-profile-web/Dockerfile \
  tests/e2e/Dockerfile; then
  fail 'floating Docker image tag found'
fi

if grep -Eq '^[[:space:]]+image:[[:space:]]+.*:(latest|stable|lts)([[:space:]]|$)' \
  compose.yaml; then
  fail 'floating Compose image tag found'
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
grep -Eq 'proxy_connect_timeout[[:space:]]+2s;' \
  src/frontend/user-profile-web/nginx.conf || fail 'Nginx connect timeout is not explicit'
grep -Eq 'proxy_read_timeout[[:space:]]+30s;' \
  src/frontend/user-profile-web/nginx.conf || fail 'Nginx response timeout is not explicit'
grep -Eq 'error_page[[:space:]]+413[[:space:]]+=[[:space:]]+@payload_too_large;' \
  src/frontend/user-profile-web/nginx.conf || fail 'Nginx does not map 413 to ProblemDetails'

stack_started=1
docker compose up --build --detach --wait --wait-timeout "${M1_COMPOSE_WAIT_TIMEOUT:-300}"

data_volume="${COMPOSE_PROJECT_NAME}_user-profile-data"
docker volume inspect "$data_volume" >/dev/null 2>&1 ||
  fail 'the isolated SQLite volume was not created'

web_container_id=$(docker compose ps -q web)
published_web_bindings=$(docker inspect --format \
  '{{range (index .NetworkSettings.Ports "8080/tcp")}}{{printf "%s:%s\n" .HostIp .HostPort}}{{end}}' \
  "$web_container_id")
[ "$published_web_bindings" = '127.0.0.1:8080' ] ||
  fail "web publishes outside the expected loopback binding: $published_web_bindings"

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

request '/review-missing-asset.js' '404' 'text/html'
if grep -Fq '<app-root' "$review_tmp/body"; then
  fail 'missing asset was served through the SPA fallback'
fi

request '/review-missing-asset.webmanifest' '404' 'text/html'
if grep -Fq '<app-root' "$review_tmp/body"; then
  fail 'missing non-script asset was served through the SPA fallback'
fi

request '/health' '200' 'application/json'
grep -Fq '"status":"Healthy"' "$review_tmp/body" || fail 'health body is unexpected'

request '/swagger/index.html' '200' 'text/html'
request '/swagger/v1/swagger.json' '200' 'application/json'
grep -Fq '"/health"' "$review_tmp/body" || fail 'runtime OpenAPI omits /health'
grep -Fq '"/api/auth/register"' "$review_tmp/body" || fail 'runtime OpenAPI omits registration'
grep -Fq '"/api/auth/login"' "$review_tmp/body" || fail 'runtime OpenAPI omits login'
grep -Fq '"/api/profile"' "$review_tmp/body" || fail 'runtime OpenAPI omits profile'
grep -Fq '"/api/profile/password"' "$review_tmp/body" ||
  fail 'runtime OpenAPI omits password change'
grep -Fq '"bearerAuth"' "$review_tmp/body" || fail 'runtime OpenAPI omits Bearer authentication'

request '/api/not-implemented' '404' 'application/problem+json'
grep -Fq '"status":404' "$review_tmp/body" || fail '404 body is not ProblemDetails'

request '/api/not-implemented.json' '404' 'application/problem+json'
grep -Fq '"status":404' "$review_tmp/body" || fail 'API path with extension bypassed the proxy'

smoke_suffix="$(date +%s)-$$"
smoke_email="m4-smoke-$smoke_suffix@example.test"
smoke_password="M4-smoke-$smoke_suffix-Aa1!"
registration_payload=$(printf \
  '{"name":"M4 Smoke","email":"%s","password":"%s","passwordConfirmation":"%s"}' \
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
  '{"name":"M4 Duplicate","email":"  %s  ","password":"%s","passwordConfirmation":"%s"}' \
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

request '/api/profile' '401' 'application/problem+json' \
  --request PUT \
  --header 'Content-Type: application/json' \
  --data '{"name":"Anonymous Update","email":"anonymous@example.test"}'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'unauthenticated profile update omits the Bearer challenge'

request '/api/profile/password' '401' 'application/problem+json' \
  --request PUT \
  --header 'Content-Type: application/json' \
  --data '{"currentPassword":"old-value","newPassword":"new-value","newPasswordConfirmation":"new-value"}'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'unauthenticated password update omits the Bearer challenge'

request '/api/profile/password' '401' 'application/problem+json' \
  --request PUT \
  --header 'Authorization: Bearer synthetically-invalid-token' \
  --header 'Content-Type: application/json' \
  --data '{"currentPassword":"old-value","newPassword":"new-value","newPasswordConfirmation":"new-value"}'
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'invalid-token password update omits the Bearer challenge'

request '/api/profile?userId=00000000-0000-0000-0000-000000000000' '200' 'application/json' \
  --header "Authorization: Bearer $access_token" \
  --header 'X-User-Id: 00000000-0000-0000-0000-000000000000'
grep -Fq "\"email\":\"$smoke_email\"" "$review_tmp/body" ||
  fail 'profile was not resolved from the authenticated subject'
grep -Fq '"id"' "$review_tmp/body" || fail 'profile response omits id'
grep -Fq '"name":"M4 Smoke"' "$review_tmp/body" || fail 'profile response omits name'
if grep -Eiq 'password|normalizedEmail|createdAt|updatedAt' "$review_tmp/body"; then
  fail 'profile response exposes an internal field'
fi

updated_email="m4-updated-$smoke_suffix@example.test"
profile_update_payload=$(printf \
  '{"name":"  M4 Updated  ","email":"  %s  "}' \
  "$updated_email")
printf '%s' "$profile_update_payload" >"$review_tmp/request"
request '/api/profile?userId=00000000-0000-0000-0000-000000000000' '200' 'application/json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'X-User-Id: 00000000-0000-0000-0000-000000000000' \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
grep -Fq '"name":"M4 Updated"' "$review_tmp/body" ||
  fail 'profile update did not trim and persist the name'
grep -Fq "\"email\":\"$updated_email\"" "$review_tmp/body" ||
  fail 'profile update did not trim and persist the email'
if grep -Eiq 'password|normalizedEmail|createdAt|updatedAt' "$review_tmp/body"; then
  fail 'profile update response exposes an internal field'
fi

invalid_profile_payload=$(printf \
  '{"name":"x","email":"invalid-mutation-%s@example.test"}' \
  "$smoke_suffix")
printf '%s' "$invalid_profile_payload" >"$review_tmp/request"
request '/api/profile' '400' 'application/problem+json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

request '/api/profile' '200' 'application/json' \
  --header "Authorization: Bearer $access_token"
grep -Fq '"name":"M4 Updated"' "$review_tmp/body" ||
  fail 'invalid profile update partially changed the name'
grep -Fq "\"email\":\"$updated_email\"" "$review_tmp/body" ||
  fail 'invalid profile update partially changed the email'

conflict_email="m4-conflict-$smoke_suffix@example.test"
conflict_password="M4-conflict-$smoke_suffix-Aa1!"
conflict_registration_payload=$(printf \
  '{"name":"M4 Conflict","email":"%s","password":"%s","passwordConfirmation":"%s"}' \
  "$conflict_email" "$conflict_password" "$conflict_password")
printf '%s' "$conflict_registration_payload" >"$review_tmp/request"
request '/api/auth/register' '201' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

conflict_uppercase_email=$(printf '%s' "$conflict_email" | tr '[:lower:]' '[:upper:]')
conflicting_profile_payload=$(printf \
  '{"name":"Must Not Persist","email":"  %s  "}' \
  "$conflict_uppercase_email")
printf '%s' "$conflicting_profile_payload" >"$review_tmp/request"
request '/api/profile' '409' 'application/problem+json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

request '/api/profile' '200' 'application/json' \
  --header "Authorization: Bearer $access_token"
grep -Fq '"name":"M4 Updated"' "$review_tmp/body" ||
  fail 'conflicting profile update partially changed the name'
grep -Fq "\"email\":\"$updated_email\"" "$review_tmp/body" ||
  fail 'conflicting profile update partially changed the email'

new_password="M4-new-$smoke_suffix-Aa2!"
wrong_current_payload=$(printf \
  '{"currentPassword":"wrong-%s","newPassword":"%s","newPasswordConfirmation":"%s"}' \
  "$smoke_suffix" "$new_password" "$new_password")
printf '%s' "$wrong_current_payload" >"$review_tmp/request"
request '/api/profile/password' '400' 'application/problem+json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

mismatched_password_payload=$(printf \
  '{"currentPassword":"%s","newPassword":"%s","newPasswordConfirmation":"different-%s"}' \
  "$smoke_password" "$new_password" "$smoke_suffix")
printf '%s' "$mismatched_password_payload" >"$review_tmp/request"
request '/api/profile/password' '400' 'application/problem+json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

old_credentials_after_failures=$(printf \
  '{"email":"%s","password":"%s"}' \
  "$updated_email" "$smoke_password")
printf '%s' "$old_credentials_after_failures" >"$review_tmp/request"
request '/api/auth/login' '200' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

valid_password_payload=$(printf \
  '{"currentPassword":"%s","newPassword":"%s","newPasswordConfirmation":"%s"}' \
  "$smoke_password" "$new_password" "$new_password")
printf '%s' "$valid_password_payload" >"$review_tmp/request"
request '/api/profile/password' '200' 'application/json' \
  --request PUT \
  --header "Authorization: Bearer $access_token" \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
grep -Fq '"message":"Password changed successfully."' "$review_tmp/body" ||
  fail 'password update response is unexpected'
if grep -Eiq '"(passwordHash|accessToken|currentPassword|newPassword|newPasswordConfirmation)"' \
  "$review_tmp/body"; then
  fail 'password update response exposes a sensitive field'
fi

printf '%s' "$old_credentials_after_failures" >"$review_tmp/request"
request '/api/auth/login' '401' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"

updated_login_payload=$(printf \
  '{"email":"  %s  ","password":"%s"}' \
  "$updated_email" "$new_password")
printf '%s' "$updated_login_payload" >"$review_tmp/request"
request '/api/auth/login' '200' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
post_change_access_token=$(sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p' "$review_tmp/body")
[ -n "$post_change_access_token" ] || fail 'new password did not create a session'

printf '%s' 'not-json' >"$review_tmp/request"
request '/api/auth/register' '415' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: text/plain' \
  --data-binary "@$review_tmp/request"
grep -Fq '"status":415' "$review_tmp/body" || fail 'unsupported media type is not ProblemDetails'

query_marker="M4_QUERY_MARKER_$smoke_suffix"
body_marker="M4_BODY_MARKER_$smoke_suffix"
header_marker="M4_HEADER_MARKER_$smoke_suffix"
marker_payload=$(printf \
  '{"name":"M4 Marker","email":"marker-%s@example.test","password":"%s","passwordConfirmation":"different"}' \
  "$smoke_suffix" "$body_marker")
printf '%s' "$marker_payload" >"$review_tmp/request"
request "/api/auth/register?password=$query_marker" '400' 'application/problem+json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --header "Authorization: Bearer $header_marker" \
  --data-binary "@$review_tmp/request"

docker compose logs --no-color >"$review_tmp/logs"
if grep -Fq -- "$query_marker" "$review_tmp/logs" ||
  grep -Fq -- "$body_marker" "$review_tmp/logs" ||
  grep -Fq -- "$header_marker" "$review_tmp/logs" ||
  grep -Fq -- "$smoke_password" "$review_tmp/logs" ||
  grep -Fq -- "$new_password" "$review_tmp/logs" ||
  grep -Fq -- "$access_token" "$review_tmp/logs" ||
  grep -Fq -- "$post_change_access_token" "$review_tmp/logs"; then
  fail 'a synthetic query, body, header, password or JWT marker was exposed in container logs'
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
persisted_profile_duplicate=$(printf \
  '{"name":"M4 Persisted Duplicate","email":"%s","password":"%s","passwordConfirmation":"%s"}' \
  "$updated_email" "$new_password" "$new_password")
post_json '/api/auth/register' '409' "$persisted_profile_duplicate"
grep -Fq '"status":409' "$review_tmp/body" ||
  fail 'updated profile did not persist after recreating the API'

request '/api/profile' '401' 'application/problem+json' \
  --header "Authorization: Bearer $post_change_access_token"
grep -Eiq '^WWW-Authenticate:[[:space:]]*Bearer' "$review_tmp/headers" ||
  fail 'old process-token rejection omitted the Bearer challenge'

printf '%s' "$updated_login_payload" >"$review_tmp/request"
request '/api/auth/login' '200' 'application/json' \
  --request POST \
  --header 'Content-Type: application/json' \
  --data-binary "@$review_tmp/request"
recreated_access_token=$(sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p' "$review_tmp/body")
[ -n "$recreated_access_token" ] ||
  fail 'persisted credentials could not create a new session after recreating the API'

request '/api/profile' '200' 'application/json' \
  --header "Authorization: Bearer $recreated_access_token"
grep -Fq "\"email\":\"$updated_email\"" "$review_tmp/body" ||
  fail 'the persisted profile could not be read after recreating the API'
grep -Fq '"name":"M4 Updated"' "$review_tmp/body" ||
  fail 'the persisted profile name was lost after recreating the API'

docker compose logs --no-color >"$review_tmp/logs"
if grep -Fq -- "$query_marker" "$review_tmp/logs" ||
  grep -Fq -- "$body_marker" "$review_tmp/logs" ||
  grep -Fq -- "$header_marker" "$review_tmp/logs" ||
  grep -Fq -- "$smoke_password" "$review_tmp/logs" ||
  grep -Fq -- "$new_password" "$review_tmp/logs" ||
  grep -Fq -- "$access_token" "$review_tmp/logs" ||
  grep -Fq -- "$post_change_access_token" "$review_tmp/logs" ||
  grep -Fq -- "$recreated_access_token" "$review_tmp/logs"; then
  fail 'a synthetic credential or marker was exposed in logs after recreating the API'
fi

docker compose stop api >/dev/null
request '/health' '503' 'application/problem+json'
grep -Fq '"status":503' "$review_tmp/body" || fail 'upstream failure body is not ProblemDetails'

run_succeeded=1
printf '%s\n' \
  'M1+M2+M3+M4 Compose OK: same origin, registration, login, profile/password updates, auth failures, persistence, 413/415, safe logs and upstream 503 verified'
