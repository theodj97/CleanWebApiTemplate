#!/usr/bin/env bash
# Falla si `dotnet list package --vulnerable` encuentra algún paquete
# vulnerable, incluidos los transitivos. `dotnet list` devuelve 0 aunque haya
# hallazgos, así que el código de salida no sirve como gate.
set -euo pipefail

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
SOLUTION="CleanWebApiTemplate.slnx"

cd "$ROOT_DIR"

echo "Comprobando paquetes vulnerables (incluidos transitivos)..."

table=$(dotnet list "$SOLUTION" package --vulnerable --include-transitive)
json=$(dotnet list "$SOLUTION" package --vulnerable --include-transitive --format json)

# Si hay vulnerabilidades, el JSON contiene objetos dentro de "vulnerabilities".
# Se aplana para que el formato (una o varias líneas) no afecte a la detección.
flat=$(printf '%s' "$json" | tr -d '\n\r')
if printf '%s' "$flat" | grep -Eq '"vulnerabilities"[[:space:]]*:[[:space:]]*\[[[:space:]]*\{'; then
    printf '%s\n' "$table" >&2
    echo "Hay paquetes vulnerables: actualiza o elimina la dependencia antes de pushear." >&2
    exit 1
fi

echo "Sin paquetes vulnerables."
