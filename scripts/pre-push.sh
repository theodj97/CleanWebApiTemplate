#!/usr/bin/env bash
# Gates that run before every push: build, format, analysis, every test and
# vulnerable packages. It is the same command the `.husky/pre-push` hook runs;
# it can also be launched by hand with `npm run verify`.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"

cd "$ROOT_DIR"

echo "==> Build"
dotnet build "$SOLUTION"

echo "==> Format (whitespace)"
dotnet format whitespace "$SOLUTION" --verify-no-changes --no-restore

echo "==> Format (style)"
dotnet format style "$SOLUTION" --verify-no-changes --no-restore

# IL2026/IL3050 are false positives of `dotnet format analyzers` in minimal
# APIs: the request-delegate source generator does not run in that mode.
# The real AOT gate is `dotnet publish` (see AGENTS.md).
echo "==> Analysis (analyzers)"
dotnet format analyzers "$SOLUTION" --verify-no-changes --no-restore \
    --exclude-diagnostics IL2026 IL3050

echo "==> Tests (unit, component and functional)"
dotnet test --solution "$SOLUTION" --no-build

echo "==> Vulnerable packages"
bash "$SCRIPT_DIR/check-vulnerable-packages.sh"

echo "==> Pre-push OK"
