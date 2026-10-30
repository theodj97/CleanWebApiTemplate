#!/usr/bin/env bash
# Gates que se ejecutan antes de cada push: build, formato, análisis, todos los
# tests y paquetes vulnerables. Es el mismo comando que ejecuta el hook
# `.husky/pre-push`; también se puede lanzar a mano con `npm run verify`.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"

cd "$ROOT_DIR"

echo "==> Build"
dotnet build "$SOLUTION"

echo "==> Formato (whitespace)"
dotnet format whitespace "$SOLUTION" --verify-no-changes --no-restore

echo "==> Formato (style)"
dotnet format style "$SOLUTION" --verify-no-changes --no-restore

# IL2026/IL3050 son falsos positivos de `dotnet format analyzers` en minimal
# APIs: el source generator de request delegates no se ejecuta en ese modo.
# El gate real de AOT es `dotnet publish` (ver AGENTS.md).
echo "==> Análisis (analyzers)"
dotnet format analyzers "$SOLUTION" --verify-no-changes --no-restore \
    --exclude-diagnostics IL2026 IL3050

echo "==> Tests (unit, component y functional)"
dotnet test --solution "$SOLUTION" --no-build

echo "==> Paquetes vulnerables"
bash "$SCRIPT_DIR/check-vulnerable-packages.sh"

echo "==> Pre-push OK"
