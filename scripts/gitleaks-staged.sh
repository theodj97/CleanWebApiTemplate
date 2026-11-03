#!/usr/bin/env bash
# Escanea los cambios en staging con gitleaks. Si el binario no está instalado,
# lo descarga en .tools/gitleaks con la versión fijada en install-gitleaks.sh.
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
