#!/usr/bin/env bash
# Scans the staged changes with gitleaks. If the binary is not installed, it
# downloads it into .tools/gitleaks with the version pinned in install-gitleaks.sh.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
bin="$ROOT_DIR/.tools/gitleaks/gitleaks"

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) bin="$bin.exe" ;;
esac

if [ ! -x "$bin" ]; then
    bash "$SCRIPT_DIR/install-gitleaks.sh"
fi

cd "$ROOT_DIR"
exec "$bin" git --staged --redact --no-banner --config "$ROOT_DIR/.gitleaks.toml"
