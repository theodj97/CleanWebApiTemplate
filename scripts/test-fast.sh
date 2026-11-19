#!/usr/bin/env bash
# Fast inner loop: unit, architecture and OpenAPI sidecar tests, no Docker.
# It builds once and runs the three assemblies concurrently, so a change is
# verified in a few seconds without starting PostgreSQL or OpenBao.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"
LOG_DIR=$(mktemp -d "${TMPDIR:-/tmp}/cleanwebapi-test-fast.XXXXXX")
trap 'rm -rf "$LOG_DIR"' EXIT

cd "$ROOT_DIR"

echo "==> Build"
dotnet build "$SOLUTION" -v q

phases="unit architecture openapi"
pids=""

launch() {
    name=$1
    shift
    echo "==> $name (started)"
    ( "$@" >"$LOG_DIR/$name.log" 2>&1 ) &
    pids="$pids $!"
}

launch unit dotnet test --project tests/CleanWebApiTemplate.UnitTests/CleanWebApiTemplate.UnitTests.csproj --no-build
launch architecture dotnet test --project tests/CleanWebApiTemplate.ArchitectureTests/CleanWebApiTemplate.ArchitectureTests.csproj --no-build
launch openapi dotnet test --project local/openapi/CleanWebApiTemplate.OpenApiExamples.UnitTests/CleanWebApiTemplate.OpenApiExamples.UnitTests.csproj --no-build

status=0
for pid in $pids; do
    if ! wait "$pid"; then
        status=1
    fi
done

for name in $phases; do
    echo
    echo "===== $name ====="
    cat "$LOG_DIR/$name.log"
done

if [ "$status" -ne 0 ]; then
    echo
    echo "==> Fast tests FAILED" >&2
    exit "$status"
fi

echo
echo "==> Fast tests OK"
