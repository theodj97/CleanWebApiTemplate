#!/usr/bin/env bash
# Fails when `dotnet list package --vulnerable` finds any vulnerable package,
# including transitive ones. `dotnet list` exits 0 even when it reports
# findings, so its exit code is not a usable gate.
#
# A single JSON invocation carries the same information as the table format;
# the table is only requested again when the gate fails, to keep the common
# path to one NuGet query.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"

cd "$ROOT_DIR"

echo "Checking vulnerable packages (including transitive ones)..."

json=$(dotnet list "$SOLUTION" package --vulnerable --include-transitive --format json)

# When there are vulnerabilities, the JSON contains objects inside
# "vulnerabilities". It is flattened so the formatting (one or more lines) does
# not affect the detection.
flat=$(printf '%s' "$json" | tr -d '\n\r')
if printf '%s' "$flat" | grep -Eq '"vulnerabilities"[[:space:]]*:[[:space:]]*\[[[:space:]]*\{'; then
    dotnet list "$SOLUTION" package --vulnerable --include-transitive >&2 || true
    echo "There are vulnerable packages: update or remove the dependency before pushing." >&2
    exit 1
fi

echo "No vulnerable packages."
