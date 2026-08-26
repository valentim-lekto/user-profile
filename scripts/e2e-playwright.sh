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

stack_started=0
run_failed=1

cleanup() {
  if [ "$stack_started" -eq 1 ]; then
    if [ "$run_failed" -eq 1 ]; then
      docker compose --profile e2e ps --all \
        >"$E2E_ARTIFACTS_DIR/compose-ps.txt" 2>&1 || true
      docker compose --profile e2e logs --no-color api web-e2e \
        >"$E2E_ARTIFACTS_DIR/compose.log" 2>&1 || true
    fi

    docker compose --profile e2e down --volumes --remove-orphans >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT HUP INT TERM

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
