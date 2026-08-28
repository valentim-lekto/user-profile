#!/bin/sh
set -eu

report_path=${1:-/artifacts/reports/mutation-report.json}

if [ ! -s "$report_path" ]; then
  echo "Mutation report is missing or empty: $report_path" >&2
  exit 1
fi

for rejected_status in Timeout NoCoverage RuntimeError; do
  rejected_count=$(grep -o "\"status\"[[:space:]]*:[[:space:]]*\"$rejected_status\"" \
    "$report_path" | wc -l | tr -d ' ')
  if [ "$rejected_count" -ne 0 ]; then
    echo "Mutation report contains $rejected_count $rejected_status mutant(s)." >&2
    exit 1
  fi
done

compile_error_count=$(grep -o '"status"[[:space:]]*:[[:space:]]*"CompileError"' \
  "$report_path" | wc -l | tr -d ' ')
if [ "$compile_error_count" -ne 3 ]; then
  echo "Expected exactly 3 classified CompileError mutants, found $compile_error_count." >&2
  exit 1
fi

echo "Mutation report gate passed: no timeout, no coverage gap, no runtime error, 3 classified compile errors."
