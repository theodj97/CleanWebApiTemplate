#!/usr/bin/env bash
# Instala la versión fijada de gitleaks en .tools/gitleaks (ignorado por git).
# gitleaks no se distribuye como tool de .NET: se descarga el binario oficial
# de GitHub Releases y se verifica su SHA-256.
set -euo pipefail

VERSION="8.30.1"

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
TOOLS_DIR="$ROOT_DIR/.tools/gitleaks"

if command -v curl >/dev/null 2>&1; then :; else
    echo "curl es necesario para descargar gitleaks." >&2
    exit 1
fi

case "$(uname -s)" in
    Linux*) os=linux ;;
    Darwin*) os=darwin ;;
    MINGW*|MSYS*|CYGWIN*) os=windows ;;
    *)
        echo "Sistema operativo no soportado: $(uname -s)" >&2
        exit 1
        ;;
esac

case "$(uname -m)" in
    x86_64|amd64) arch=x64 ;;
    aarch64|arm64) arch=arm64 ;;
    *)
        echo "Arquitectura no soportada: $(uname -m)" >&2
        exit 1
        ;;
esac

if [ "$os" = "windows" ]; then
    asset="gitleaks_${VERSION}_${os}_${arch}.zip"
    bin_name="gitleaks.exe"
else
    asset="gitleaks_${VERSION}_${os}_${arch}.tar.gz"
    bin_name="gitleaks"
fi

bin="$TOOLS_DIR/$bin_name"

if [ -x "$bin" ] && [ "$("$bin" version 2>/dev/null || true)" = "$VERSION" ]; then
    echo "gitleaks $VERSION ya está instalado en $bin"
    exit 0
fi

base_url="https://github.com/gitleaks/gitleaks/releases/download/v${VERSION}"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

echo "Descargando gitleaks $VERSION ($os/$arch)..."
curl -fsSL -o "$tmp/$asset" "$base_url/$asset"
curl -fsSL -o "$tmp/checksums.txt" "$base_url/gitleaks_${VERSION}_checksums.txt"

expected=$(awk -v asset="$asset" '$2 == asset { print $1 }' "$tmp/checksums.txt")
if [ -z "$expected" ]; then
    echo "No se encontró el checksum de $asset." >&2
    exit 1
fi

if command -v sha256sum >/dev/null 2>&1; then
    actual=$(sha256sum "$tmp/$asset" | awk '{ print $1 }')
else
    actual=$(shasum -a 256 "$tmp/$asset" | awk '{ print $1 }')
fi

if [ "$expected" != "$actual" ]; then
    echo "Checksum inválido para $asset (esperado $expected, obtenido $actual)." >&2
    exit 1
fi

mkdir -p "$TOOLS_DIR"

if [ "$os" = "windows" ]; then
    if command -v unzip >/dev/null 2>&1; then
        unzip -o -q "$tmp/$asset" "$bin_name" -d "$TOOLS_DIR"
    else
        powershell.exe -NoProfile -Command \
            "Expand-Archive -LiteralPath '$(cygpath -w "$tmp/$asset")' -DestinationPath '$(cygpath -w "$TOOLS_DIR")' -Force"
    fi
else
    tar -xzf "$tmp/$asset" -C "$TOOLS_DIR" "$bin_name"
fi

chmod +x "$bin"
"$bin" version
echo "gitleaks instalado en $bin"
