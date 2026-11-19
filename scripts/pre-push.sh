#!/usr/bin/env bash
# Gates that run before every push: build, format, analysis, every test and
# vulnerable packages. It is the same command the `.husky/pre-push` hook runs;
# it can also be launched by hand with `npm run verify`.
#
# After the build, the format/analyzer pass, the test run and the vulnerable
# package audit are independent: they run concurrently and their output is
# replayed in a stable order, so the wall-clock time is the slowest lane instead
# of the sum. A single `dotnet format` invocation runs whitespace, style and
# analyzers over one Roslyn workspace (three subcommands would load it three
# times); `--verify-no-changes` keeps it read-only.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"
LOG_DIR=$(mktemp -d "${TMPDIR:-/tmp}/cleanwebapi-verify.XXXXXX")
trap 'rm -rf "$LOG_DIR"' EXIT

cd "$ROOT_DIR"

echo "==> Build"
dotnet build "$SOLUTION"

phases="format tests vulnerable"
pids=""

# Runs one phase into its own log and records its duration; the duration and the
# log are read after all lanes finish, so the output is never interleaved.
launch() {
    name=$1
    shift
    echo "==> $name (started)"
    ( SECONDS=0
      "$@" >"$LOG_DIR/$name.log" 2>&1
      status=$?
      echo "$SECONDS" >"$LOG_DIR/$name.seconds"
      exit "$status"
    ) &
    pids="$pids $!"
}

# IL2026/IL3050 are false positives of `dotnet format analyzers` in minimal
# APIs: the request-delegate source generator does not run in that mode.
# The real AOT gate is `dotnet publish` (see AGENTS.md).
echo "==> Format, style and analyzers (started)"
launch format dotnet format "$SOLUTION" --verify-no-changes --no-restore \
    --exclude-diagnostics IL2026 IL3050

echo "==> Tests (unit, component and functional)"
launch tests dotnet test --solution "$SOLUTION" --no-build

echo "==> Vulnerable packages"
launch vulnerable bash "$SCRIPT_DIR/check-vulnerable-packages.sh"

status=0
for pid in $pids; do
    if ! wait "$pid"; then
        status=1
    fi
done

for name in $phases; do
    seconds=$(cat "$LOG_DIR/$name.seconds" 2>/dev/null || echo "?")
    echo
    echo "===== $name (${seconds}s) ====="
    if [ -s "$LOG_DIR/$name.log" ]; then
        cat "$LOG_DIR/$name.log"
    else
        echo "(no output)"
    fi
done

if [ "$status" -ne 0 ]; then
    echo
    echo "==> Pre-push FAILED" >&2
    exit "$status"
fi

echo
echo "==> Pre-push OK"
