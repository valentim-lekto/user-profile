#!/bin/sh
set -eu

dotnet stryker "$@"
/src/scripts/validate-mutation-report.sh /artifacts/reports/mutation-report.json
