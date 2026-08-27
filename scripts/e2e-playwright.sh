#!/bin/sh

set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_dir/.." && pwd)
cd "$repository_root"

compose_project_prefix="${COMPOSE_PROJECT_NAME:-user-profile-e2e}"
COMPOSE_PROJECT_NAME="${compose_project_prefix}-e2e-$$"
export COMPOSE_PROJECT_NAME

artifacts_root="${E2E_ARTIFACTS_DIR:-$repository_root/artifacts/e2e}"
E2E_ARTIFACTS_DIR="$artifacts_root/$COMPOSE_PROJECT_NAME"
export E2E_ARTIFACTS_DIR
mkdir -p "$E2E_ARTIFACTS_DIR"
printf '%s\n' "$COMPOSE_PROJECT_NAME" >"$E2E_ARTIFACTS_DIR/compose-project.txt"

stack_started=0
run_failed=1

cleanup() {
  cleanup_status=$?
  teardown_status=0
  trap - EXIT HUP INT TERM

  if [ "$stack_started" -eq 1 ]; then
    if [ "$run_failed" -eq 1 ]; then
      printf 'project=%s\nexit_status=%s\n' \
        "$COMPOSE_PROJECT_NAME" "$cleanup_status" \
        >"$E2E_ARTIFACTS_DIR/context.txt"
      docker compose --profile e2e config --services \
        >"$E2E_ARTIFACTS_DIR/compose-services.txt" 2>&1 || true
      docker compose --profile e2e config --images \
        >"$E2E_ARTIFACTS_DIR/compose-images.txt" 2>&1 || true
      docker compose --profile e2e ps --all \
        >"$E2E_ARTIFACTS_DIR/compose-ps.txt" 2>&1 || true
      docker compose --profile e2e logs --no-color api web-e2e \
        2>&1 |
        sh "$repository_root/scripts/sanitize-ci-output.sh" \
          >"$E2E_ARTIFACTS_DIR/compose.log" || true
    fi

    teardown_output=''
    set +e
    teardown_output=$(docker compose --profile e2e down --volumes --remove-orphans 2>&1)
    teardown_status=$?
    set -e

    if [ "$teardown_status" -ne 0 ]; then
      printf '%s\n' "$teardown_output" |
        sh "$repository_root/scripts/sanitize-ci-output.sh" \
          >"$E2E_ARTIFACTS_DIR/teardown.log"
    fi
  fi

  if [ "$teardown_status" -ne 0 ]; then
    printf 'E2E teardown returned %s\n' "$teardown_status" >&2
  fi

  final_status=$(sh "$repository_root/scripts/resolve-cleanup-status.sh" \
    "$cleanup_status" "$teardown_status")
  exit "$final_status"
}

trap cleanup EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

docker compose --profile e2e config --quiet
stack_started=1
docker compose --profile e2e up --build --detach --wait --wait-timeout "${E2E_COMPOSE_WAIT_TIMEOUT:-300}" web-e2e

set +e
docker compose --profile e2e run --build --rm --no-deps e2e
e2e_status=$?
set -e

if [ "$e2e_status" -eq 0 ]; then
  run_failed=0
fi

exit "$e2e_status"
