#!/usr/bin/env bash
# Smoke test del template de `dotnet new`. Instala este directorio en un hive de
# templates temporal (no toca la lista global), genera dos proyectos y comprueba
# que el resultado es un proyecto limpio, compilable, testeable y publicable AOT.
#
# Uso: bash scripts/template-smoke.sh [--keep] [--no-aot]
#   --keep    conserva el directorio de salida para inspeccionarlo.
#   --no-aot  omite el `dotnet publish` Native AOT (el paso más lento).
#
# Requisitos: .NET SDK, git. No necesita Docker: solo ejecuta tests sin contenedores.
# Este script se excluye del proyecto generado (.template.config/template.json).
set -euo pipefail

KEEP=0
AOT=1
for arg in "$@"; do
    case "$arg" in
        --keep) KEEP=1 ;;
        --no-aot) AOT=0 ;;
        *)
            echo "Opción desconocida: $arg" >&2
            echo "Uso: bash scripts/template-smoke.sh [--keep] [--no-aot]" >&2
            exit 2
            ;;
    esac
done

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")

WORK_DIR=$(mktemp -d "${TMPDIR:-/tmp}/template-smoke.XXXXXX")
HIVE="$WORK_DIR/hive"
OUT="$WORK_DIR/out"

cleanup() {
    if [ "$KEEP" -eq 1 ]; then
        echo "Salida conservada en $WORK_DIR"
    else
        rm -rf "$WORK_DIR"
    fi
}
trap cleanup EXIT

failures=0
fail() {
    echo "  ✗ $1" >&2
    failures=$((failures + 1))
}
ok() {
    echo "  ✓ $1"
}

case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) RID=linux-x64 ;;
    Linux-aarch64 | Linux-arm64) RID=linux-arm64 ;;
    Darwin-x86_64) RID=osx-x64 ;;
    Darwin-arm64) RID=osx-arm64 ;;
    *) RID=linux-x64 ;;
esac

echo "==> Instalación del template (hive aislado: $HIVE)"
dotnet new install "$ROOT_DIR" --debug:custom-hive "$HIVE"

echo "==> Generación: Sample (con restore) y Sample2 (--no-restore)"
dotnet new clean-webapi -n Sample -o "$OUT/Sample" \
    --debug:custom-hive "$HIVE" --allow-scripts yes
dotnet new clean-webapi -n Sample2 -o "$OUT/Sample2" \
    --debug:custom-hive "$HIVE" --no-restore

SAMPLE="$OUT/Sample"

echo "==> Contenido generado"
for path in README.md .gitignore Sample.slnx AGENTS.md .config/dotnet-tools.json; do
    if [ -e "$SAMPLE/$path" ]; then ok "existe $path"; else fail "falta $path"; fi
done

for path in coverage-report .agents .tools .task-skill skills-lock.json docs/audit \
    README.template.md scripts/template-smoke.sh .template.config .husky/_; do
    if [ -e "$SAMPLE/$path" ]; then fail "no debería existir $path"; else ok "no existe $path"; fi
done

if grep -q '^# Sample$' "$SAMPLE/README.md"; then
    ok "README.md con el nombre del proyecto"
else
    fail "README.md no empieza por '# Sample'"
fi

leftover_names=$(find "$SAMPLE" -path '*/.git' -prune -o -path '*/bin' -prune -o -path '*/obj' -prune \
    -o -iname '*cleanwebapitemplate*' -print)
if [ -z "$leftover_names" ]; then
    ok "ningún nombre de fichero contiene CleanWebApiTemplate"
else
    fail "nombres sin sustituir:"$'\n'"$leftover_names"
fi

leftover_content=$(grep -rIil --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj \
    'cleanwebapitemplate' "$SAMPLE" || true)
if [ -z "$leftover_content" ]; then
    ok "ningún fichero contiene CleanWebApiTemplate"
else
    fail "contenido sin sustituir:"$'\n'"$leftover_content"
fi

if grep -q '"name": "sample"' "$SAMPLE/package.json"; then
    ok "package.json usa el nombre en minúsculas"
else
    fail "package.json no usa 'sample' como nombre"
fi

secrets_id() {
    sed -n 's:.*<UserSecretsId>\(.*\)</UserSecretsId>.*:\1:p' "$1"
}

# GUIDs del template (los que cada generación debe sustituir).
template_api_id=$(secrets_id "$ROOT_DIR/src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj")
template_apphost_id=$(secrets_id "$ROOT_DIR/local/aspire/CleanWebApiTemplate.AppHost/CleanWebApiTemplate.AppHost.csproj")

api_id=$(secrets_id "$SAMPLE/src/Sample.Api/Sample.Api.csproj")
apphost_id=$(secrets_id "$SAMPLE/local/aspire/Sample.AppHost/Sample.AppHost.csproj")
api2_id=$(secrets_id "$OUT/Sample2/src/Sample2.Api/Sample2.Api.csproj")
apphost2_id=$(secrets_id "$OUT/Sample2/local/aspire/Sample2.AppHost/Sample2.AppHost.csproj")

guid_re='^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
for id in "$api_id" "$apphost_id" "$api2_id" "$apphost2_id"; do
    if [[ "$id" =~ $guid_re ]]; then ok "UserSecretsId válido: $id"; else fail "UserSecretsId inválido: '$id'"; fi
    if [ "$id" = "$template_api_id" ] || [ "$id" = "$template_apphost_id" ]; then
        fail "UserSecretsId no regenerado: $id"
    fi
done

unique_ids=$(printf '%s\n' "$api_id" "$apphost_id" "$api2_id" "$apphost2_id" | sort -u | wc -l | tr -d ' ')
if [ "$unique_ids" -eq 4 ]; then
    ok "UserSecretsId distintos por proyecto y por generación"
else
    fail "UserSecretsId repetidos entre proyectos o generaciones"
fi

for hook in .husky/pre-commit .husky/commit-msg .husky/pre-push scripts/pre-push.sh scripts/commit-msg.sh \
    scripts/coverage.sh scripts/gitleaks-staged.sh scripts/install-gitleaks.sh scripts/check-vulnerable-packages.sh; do
    if [ -x "$SAMPLE/$hook" ]; then ok "ejecutable: $hook"; else fail "no ejecutable: $hook"; fi
done

if [ "$failures" -ne 0 ]; then
    echo "Smoke test del template: $failures comprobaciones fallidas." >&2
    exit 1
fi

cd "$SAMPLE"

echo "==> Hooks (git init + dotnet husky install)"
git init --quiet
dotnet tool restore
dotnet husky install
if [ "$(git config --get core.hooksPath)" = ".husky" ] && [ -f .husky/_/husky.sh ]; then
    ok "Husky instalado (core.hooksPath=.husky)"
else
    fail "Husky no quedó instalado"
fi

echo "==> Build"
dotnet build Sample.slnx

echo "==> Tests sin contenedores (unit, architecture y sidecar local)"
dotnet test --project tests/Sample.Api.UnitTests/Sample.Api.UnitTests.csproj --no-build
dotnet test --project tests/Sample.ArchitectureTests/Sample.ArchitectureTests.csproj --no-build
dotnet test --project local/openapi/Sample.OpenApiExamples.UnitTests/Sample.OpenApiExamples.UnitTests.csproj --no-build

if [ "$AOT" -eq 1 ]; then
    echo "==> Publish Native AOT ($RID)"
    dotnet publish src/Sample.Api/Sample.Api.csproj -c Release -r "$RID" --self-contained
else
    echo "==> Publish Native AOT omitido (--no-aot)"
fi

if [ "$failures" -ne 0 ]; then
    echo "Smoke test del template: $failures comprobaciones fallidas." >&2
    exit 1
fi

echo "==> Smoke test del template OK"
