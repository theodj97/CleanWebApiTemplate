#!/usr/bin/env bash
# Smoke test for the `dotnet new` template. Installs this directory in a temporary template hive (the global
# list is untouched), generates two projects and checks that the result is a clean, buildable, testable and
# AOT-publishable project.
#
# It also injects the Project worked example from the clean-webapi skill and builds it, so the example cannot
# drift from the code.
#
# Usage: bash scripts/template-smoke.sh [--keep] [--no-aot]
#   --keep    keeps the output directory for inspection.
#   --no-aot  skips the Native AOT publish (the slowest step).
#
# Requirements: .NET SDK, git, python3. Docker is not required: only container-free tests run.
# This script is excluded from the generated project (.template.config/template.json).
set -euo pipefail

KEEP=0
AOT=1
for arg in "$@"; do
    case "$arg" in
        --keep) KEEP=1 ;;
        --no-aot) AOT=0 ;;
        *)
            echo "Unknown option: $arg" >&2
            echo "Usage: bash scripts/template-smoke.sh [--keep] [--no-aot]" >&2
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
        echo "Output kept at $WORK_DIR"
    else
        rm -rf "$WORK_DIR"
    fi
}
trap cleanup EXIT

failures=0
fail() {
    echo "  x $1" >&2
    failures=$((failures + 1))
}
ok() {
    echo "  + $1"
}

case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) RID=linux-x64 ;;
    Linux-aarch64 | Linux-arm64) RID=linux-arm64 ;;
    Darwin-x86_64) RID=osx-x64 ;;
    Darwin-arm64) RID=osx-arm64 ;;
    *) RID=linux-x64 ;;
esac

echo "==> Installing the template (isolated hive: $HIVE)"
dotnet new install "$ROOT_DIR" --debug:custom-hive "$HIVE"

echo "==> Generating: Sample (with restore) and Sample2 (--no-restore)"
dotnet new clean-webapi -n Sample -o "$OUT/Sample" \
    --debug:custom-hive "$HIVE" --allow-scripts yes
dotnet new clean-webapi -n Sample2 -o "$OUT/Sample2" \
    --debug:custom-hive "$HIVE" --no-restore

SAMPLE="$OUT/Sample"

echo "==> Generated content"
for path in README.md .gitignore Sample.slnx AGENTS.md GLOSSARY.md .config/dotnet-tools.json \
    .opencode/skills/clean-webapi/SKILL.md \
    .opencode/commands/feature.md \
    .opencode/agents/reviewer.md \
    docs/adr/0001-native-aot.md; do
    if [ -e "$SAMPLE/$path" ]; then ok "exists $path"; else fail "missing $path"; fi
done

for path in coverage-report .agents .tools .task-skill skills-lock.json docs/audit \
    .opencode/skills/template-maintainer \
    build artifacts CHANGELOG.md LICENSE README.nuget.md \
    README.template.md scripts/template-smoke.sh scripts/package-smoke.sh .template.config .husky/_; do
    if [ -e "$SAMPLE/$path" ]; then fail "should not exist $path"; else ok "does not exist $path"; fi
done

if grep -q '^# Sample$' "$SAMPLE/README.md"; then
    ok "README.md starts with the project name"
else
    fail "README.md does not start with '# Sample'"
fi

leftover_names=$(find "$SAMPLE" -path '*/.git' -prune -o -path '*/bin' -prune -o -path '*/obj' -prune \
    -o -iname '*cleanwebapitemplate*' -print)
if [ -z "$leftover_names" ]; then
    ok "no file name contains CleanWebApiTemplate"
else
    fail "unreplaced names:"$'\n'"$leftover_names"
fi

leftover_content=$(grep -rIil --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj \
    'cleanwebapitemplate' "$SAMPLE" || true)
if [ -z "$leftover_content" ]; then
    ok "no file content contains CleanWebApiTemplate"
else
    fail "unreplaced content:"$'\n'"$leftover_content"
fi

if grep -q '"name": "sample"' "$SAMPLE/package.json"; then
    ok "package.json uses the lower-case name"
else
    fail "package.json does not use 'sample' as its name"
fi

secrets_id() {
    sed -n 's:.*<UserSecretsId>\(.*\)</UserSecretsId>.*:\1:p' "$1"
}

# Template GUIDs (each generation must replace them).
template_api_id=$(secrets_id "$ROOT_DIR/src/CleanWebApiTemplate.Api/CleanWebApiTemplate.Api.csproj")
template_apphost_id=$(secrets_id "$ROOT_DIR/local/aspire/CleanWebApiTemplate.AppHost/CleanWebApiTemplate.AppHost.csproj")

api_id=$(secrets_id "$SAMPLE/src/Sample.Api/Sample.Api.csproj")
apphost_id=$(secrets_id "$SAMPLE/local/aspire/Sample.AppHost/Sample.AppHost.csproj")
api2_id=$(secrets_id "$OUT/Sample2/src/Sample2.Api/Sample2.Api.csproj")
apphost2_id=$(secrets_id "$OUT/Sample2/local/aspire/Sample2.AppHost/Sample2.AppHost.csproj")

guid_re='^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
for id in "$api_id" "$apphost_id" "$api2_id" "$apphost2_id"; do
    if [[ "$id" =~ $guid_re ]]; then ok "valid UserSecretsId: $id"; else fail "invalid UserSecretsId: '$id'"; fi
    if [ "$id" = "$template_api_id" ] || [ "$id" = "$template_apphost_id" ]; then
        fail "UserSecretsId not regenerated: $id"
    fi
done

unique_ids=$(printf '%s\n' "$api_id" "$apphost_id" "$api2_id" "$apphost2_id" | sort -u | wc -l | tr -d ' ')
if [ "$unique_ids" -eq 4 ]; then
    ok "UserSecretsId distinct per project and per generation"
else
    fail "repeated UserSecretsId across projects or generations"
fi

for hook in .husky/pre-commit .husky/commit-msg .husky/pre-push scripts/pre-push.sh scripts/commit-msg.sh \
    scripts/coverage.sh scripts/gitleaks-staged.sh scripts/install-gitleaks.sh scripts/check-vulnerable-packages.sh; do
    if [ -x "$SAMPLE/$hook" ]; then ok "executable: $hook"; else fail "not executable: $hook"; fi
done

if [ "$failures" -ne 0 ]; then
    echo "Template smoke test: $failures failed check(s)." >&2
    exit 1
fi

cd "$SAMPLE"

echo "==> Hooks (git init + dotnet husky install)"
git init --quiet
dotnet tool restore
dotnet husky install
if [ "$(git config --get core.hooksPath)" = ".husky" ] && [ -f .husky/_/husky.sh ]; then
    ok "Husky installed (core.hooksPath=.husky)"
else
    fail "Husky was not installed"
fi

echo "==> Build"
dotnet build Sample.slnx

echo "==> Container-free tests (unit, architecture and local sidecar)"
dotnet test --project tests/Sample.UnitTests/Sample.UnitTests.csproj --no-build
dotnet test --project tests/Sample.ArchitectureTests/Sample.ArchitectureTests.csproj --no-build
dotnet test --project local/openapi/Sample.OpenApiExamples.UnitTests/Sample.OpenApiExamples.UnitTests.csproj --no-build

echo "==> Injecting the Project worked example from the clean-webapi skill"
EXAMPLES="$SAMPLE/.opencode/skills/clean-webapi/assets/examples"
cp -r "$EXAMPLES/src/." "$SAMPLE/src/"
cp -r "$EXAMPLES/tests/." "$SAMPLE/tests/"
cp -r "$EXAMPLES/local/." "$SAMPLE/local/"

python3 - "$SAMPLE" <<'PY'
import pathlib
import sys

sample = pathlib.Path(sys.argv[1])


def edit(relative, replacements):
    path = sample / relative
    text = path.read_text()
    for old, new in replacements:
        if old not in text:
            raise SystemExit(f"anchor not found in {relative}: {old[:60]!r}")
        text = text.replace(old, new, 1)
    path.write_text(text)


edit('src/Sample.Api/ConfigureServices.cs', [
    ('using Sample.Api.Common;', 'using Sample.Api.Common;\nusing Sample.Api.Project;'),
    ('        services.AddTodoModule();\n', '        services.AddTodoModule();\n        services.AddProjectModule();\n'),
])

edit('tests/Sample.Tests.Shared/Fixtures/TestServerFixtureBase.cs', [
    ('    public TodoTestRepository Todos { get; private set; } = null!;\n',
     '    public TodoTestRepository Todos { get; private set; } = null!;\n\n'
     '    public ProjectTestRepository Projects { get; private set; } = null!;\n'),
    ('        Todos = new TodoTestRepository(connectionString);\n',
     '        Todos = new TodoTestRepository(connectionString);\n'
     '        ProjectTestRepositoryIsCreated = true;\n'
     '        Projects = new ProjectTestRepository(connectionString);\n'),
    ('        if (TodoTestRepositoryIsCreated)\n        {\n            await Todos.DisposeAsync();\n        }\n',
     '        if (TodoTestRepositoryIsCreated)\n        {\n            await Todos.DisposeAsync();\n        }\n\n'
     '        if (ProjectTestRepositoryIsCreated)\n        {\n            await Projects.DisposeAsync();\n        }\n'),
    ('    private bool TodoTestRepositoryIsCreated { get; set; }',
     '    private bool TodoTestRepositoryIsCreated { get; set; }\n\n'
     '    private bool ProjectTestRepositoryIsCreated { get; set; }'),
])
PY

echo "==> Build with the injected example"
dotnet build Sample.slnx

echo "==> Container-free tests with the injected example"
dotnet test --project tests/Sample.UnitTests/Sample.UnitTests.csproj --no-build
dotnet test --project tests/Sample.ArchitectureTests/Sample.ArchitectureTests.csproj --no-build

if [ "$AOT" -eq 1 ]; then
    echo "==> Publish Native AOT ($RID)"
    dotnet publish src/Sample.Api/Sample.Api.csproj -c Release -r "$RID" --self-contained
else
    echo "==> Native AOT publish skipped (--no-aot)"
fi

if [ "$failures" -ne 0 ]; then
    echo "Template smoke test: $failures failed check(s)." >&2
    exit 1
fi

echo "==> Template smoke test OK"
