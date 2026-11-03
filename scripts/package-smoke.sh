#!/usr/bin/env bash
# Builds the template NuGet package, inspects its content, installs it in an isolated
# template hive, generates a project, builds it and runs the container-free tests.
#
# Usage: bash scripts/package-smoke.sh [--keep] [--no-aot]
#   --keep    keeps the work directory for inspection.
#   --no-aot  skips the Native AOT publish (the slowest step).
#
# Requirements: .NET SDK, git, unzip (for the package content check), network for NuGet restore.
# Docker is not required: only unit, architecture and sidecar tests run.
set -euo pipefail

KEEP=0
AOT=1
for arg in "$@"; do
    case "$arg" in
        --keep) KEEP=1 ;;
        --no-aot) AOT=0 ;;
        *)
            echo "Unknown option: $arg" >&2
            echo "Usage: bash scripts/package-smoke.sh [--keep] [--no-aot]" >&2
            exit 2
            ;;
    esac
done

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
PACK_PROJECT="$ROOT_DIR/build/CleanWebApiTemplate.Template.csproj"

PACKAGE_ID=$(sed -n 's:.*<PackageId>\(.*\)</PackageId>.*:\1:p' "$PACK_PROJECT" | head -n 1)
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PACK_PROJECT" | head -n 1)
[ -n "$PACKAGE_ID" ] || { echo "PackageId not found in $PACK_PROJECT" >&2; exit 1; }
[ -n "$VERSION" ] || { echo "Version not found in $PACK_PROJECT" >&2; exit 1; }

case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) RID=linux-x64 ;;
    Linux-aarch64 | Linux-arm64) RID=linux-arm64 ;;
    Darwin-x86_64) RID=osx-x64 ;;
    Darwin-arm64) RID=osx-arm64 ;;
    *) RID=linux-x64 ;;
esac

WORK_DIR=$(mktemp -d "${TMPDIR:-/tmp}/package-smoke.XXXXXX")
ARTIFACTS="$WORK_DIR/artifacts"
HIVE="$WORK_DIR/hive"
OUT="$WORK_DIR/out"
mkdir -p "$ARTIFACTS" "$OUT"

cleanup() {
    if [ "$KEEP" -eq 1 ]; then
        echo "Work directory kept at $WORK_DIR"
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

echo "==> Packing $PACKAGE_ID $VERSION"
dotnet pack "$PACK_PROJECT" -c Release -o "$ARTIFACTS"

NUPKG="$ARTIFACTS/$PACKAGE_ID.$VERSION.nupkg"
if [ -f "$NUPKG" ]; then
    ok "package: $NUPKG"
else
    fail "package not found: $NUPKG"
    exit 1
fi

echo "==> Inspecting package content"
if command -v unzip >/dev/null 2>&1; then
    package_files=$(unzip -Z1 "$NUPKG")
    has_file() { printf '%s\n' "$package_files" | grep -Fxq "$1"; }

    for required in \
        "content/.template.config/template.json" \
        "content/.template.config/dotnetcli.host.json" \
        "content/README.template.md" \
        "content/AGENTS.md" \
        "content/global.json" \
        "content/package.json" \
        "content/src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj" \
        "content/tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj" \
        "content/local/aspire/CleanWebApiTemplate.AppHost/CleanWebApiTemplate.AppHost.csproj" \
        "content/scripts/pre-push.sh"; do
        if has_file "$required"; then ok "package contains $required"; else fail "package is missing $required"; fi
    done

    for forbidden in \
        "content/build/" \
        "content/.git/" \
        "content/.agents/" \
        "content/.tools/" \
        "content/coverage-report/" \
        "content/docs/audit/" \
        "content/skills-lock.json" \
        "content/README.md" \
        "content/README.nuget.md" \
        "content/CHANGELOG.md" \
        "content/LICENSE" \
        "content/scripts/template-smoke.sh" \
        "content/scripts/package-smoke.sh"; do
        if printf '%s\n' "$package_files" | grep -Fq "$forbidden"; then
            fail "package should not contain $forbidden"
        else
            ok "package excludes $forbidden"
        fi
    done

    for root_file in "README.md" "LICENSE" "CHANGELOG.md"; do
        if has_file "$root_file"; then ok "package root contains $root_file"; else fail "package root is missing $root_file"; fi
    done
else
    echo "  ! unzip not found; skipping package content inspection" >&2
fi

echo "==> Installing the package in an isolated hive"
dotnet new install "$NUPKG" --debug:custom-hive "$HIVE"

echo "==> Template discovery"
if dotnet new list clean-webapi --debug:custom-hive "$HIVE" | grep -q "Clean Web API Template"; then
    ok "template is listed by 'dotnet new list'"
else
    fail "template is not listed by 'dotnet new list'"
fi

if dotnet new clean-webapi --help --debug:custom-hive "$HIVE" | grep -q -- "--no-restore"; then
    ok "template help lists --no-restore"
else
    fail "template help does not list --no-restore"
fi

echo "==> Generating Sample from the package"
dotnet new clean-webapi -n Sample -o "$OUT/Sample" \
    --debug:custom-hive "$HIVE" --allow-scripts yes

SAMPLE="$OUT/Sample"

echo "==> Generated content"
for path in README.md .gitignore Sample.slnx AGENTS.md .config/dotnet-tools.json \
    src/Sample.Api/Sample.Api.csproj \
    tests/Sample.Api.UnitTests/Sample.Api.UnitTests.csproj \
    local/aspire/Sample.AppHost/Sample.AppHost.csproj; do
    if [ -e "$SAMPLE/$path" ]; then ok "exists $path"; else fail "missing $path"; fi
done

for path in build artifacts CHANGELOG.md LICENSE README.nuget.md .agents .tools \
    coverage-report docs/audit README.template.md .template.config \
    scripts/template-smoke.sh scripts/package-smoke.sh; do
    if [ -e "$SAMPLE/$path" ]; then fail "should not exist $path"; else ok "does not exist $path"; fi
done

if grep -q '^# Sample$' "$SAMPLE/README.md"; then
    ok "README.md uses the project name"
else
    fail "README.md does not start with '# Sample'"
fi

leftover_names=$(find "$SAMPLE" -path '*/.git' -prune -o -path '*/bin' -prune -o -path '*/obj' -prune \
    -o -iname '*cleanwebapitemplate*' -print)
if [ -z "$leftover_names" ]; then
    ok "no file name contains CleanWebApiTemplate"
else
    fail "file names not replaced:"$'\n'"$leftover_names"
fi

leftover_content=$(grep -rIil --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj \
    'cleanwebapitemplate' "$SAMPLE" || true)
if [ -z "$leftover_content" ]; then
    ok "no file content contains CleanWebApiTemplate"
else
    fail "content not replaced:"$'\n'"$leftover_content"
fi

if [ "$failures" -ne 0 ]; then
    echo "Package smoke test: $failures check(s) failed." >&2
    exit 1
fi

cd "$SAMPLE"

echo "==> Build"
dotnet build Sample.slnx

echo "==> Container-free tests (unit, architecture and local sidecar)"
dotnet test --project tests/Sample.Api.UnitTests/Sample.Api.UnitTests.csproj --no-build
dotnet test --project tests/Sample.ArchitectureTests/Sample.ArchitectureTests.csproj --no-build
dotnet test --project local/openapi/Sample.OpenApiExamples.UnitTests/Sample.OpenApiExamples.UnitTests.csproj --no-build

if [ "$AOT" -eq 1 ]; then
    echo "==> Publish Native AOT ($RID)"
    dotnet publish src/Sample.Api/Sample.Api.csproj -c Release -r "$RID" --self-contained
else
    echo "==> Native AOT publish skipped (--no-aot)"
fi

if [ "$failures" -ne 0 ]; then
    echo "Package smoke test: $failures check(s) failed." >&2
    exit 1
fi

echo "==> Uninstalling $PACKAGE_ID from the isolated hive"
dotnet new uninstall "$PACKAGE_ID" --debug:custom-hive "$HIVE" || \
    echo "  ! uninstall from the isolated hive failed (the hive is temporary)" >&2

echo "==> Package smoke test OK"
