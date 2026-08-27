#!/bin/sh

set -eu

[ "$#" -eq 2 ] || exit 64

primary_status=$1
teardown_status=$2

if [ "$primary_status" -ne 0 ]; then
  printf '%s\n' "$primary_status"
else
  printf '%s\n' "$teardown_status"
fi
