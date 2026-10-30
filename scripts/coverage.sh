#!/usr/bin/env bash
# Collects coverage for unit, component and functional tests, merges it, writes an HTML/text
# report and fails when any configured threshold is not met.
#
# Requirements: .NET SDK, Docker (component and functional tests use Aspire containers).
# Usage: npm run coverage   (or: bash scripts/coverage.sh)
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT_DIR=$(dirname "$SCRIPT_DIR")
OUT_DIR="$ROOT_DIR/coverage-report/coverage"
XML="$OUT_DIR/report/Cobertura.xml"

cd "$ROOT_DIR"

dotnet tool restore
dotnet build CleanWebApiTemplate.slnx

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

collect() {
    local name=$1
    local project=$2
    echo "Collecting coverage: $name"
    dotnet tool run dotnet-coverage collect \
        -f cobertura \
        -o "$OUT_DIR/$name.xml" \
        dotnet test --project "$project" --no-build
}

collect unit tests/CleanWebApiTemplate.Api.UnitTests/CleanWebApiTemplate.Api.UnitTests.csproj
collect component tests/CleanWebApiTemplate.Api.ComponentTests/CleanWebApiTemplate.Api.ComponentTests.csproj
collect functional tests/CleanWebApiTemplate.Api.FunctionalTests/CleanWebApiTemplate.Api.FunctionalTests.csproj

dotnet tool run dotnet-coverage merge \
    -f cobertura \
    -o "$OUT_DIR/merged.xml" \
    "$OUT_DIR/unit.xml" "$OUT_DIR/component.xml" "$OUT_DIR/functional.xml"

dotnet tool run reportgenerator \
    -reports:"$OUT_DIR/merged.xml" \
    -targetdir:"$OUT_DIR/report" \
    -reporttypes:'TextSummary;MarkdownSummaryGithub;Cobertura' \
    -assemblyfilters:'+CleanWebApiTemplate.Api;+CleanWebApiTemplate.Application;+CleanWebApiTemplate.Domain;+CleanWebApiTemplate.Infrastructure;+CleanWebApiTemplate.ServiceDefaults' \
    -filefilters:'-**/obj/**;-**/bin/**'

percent() {
    awk -v rate="$1" 'BEGIN { printf "%.1f", rate * 100 }'
}

below() {
    awk -v actual="$1" -v minimum="$2" 'BEGIN { exit !(actual + 0 < minimum + 0) }'
}

read_rate() {
    local xml_line=$1
    local attribute=$2
    printf '%s\n' "$xml_line" | sed -n "s/.*$attribute=\"\\([^\"]*\\)\".*/\\1/p"
}

failed=0

check_scope() {
    local scope=$1
    local line_rate=$2
    local branch_rate=$3
    local min_line=$4
    local min_branch=$5

    local line branch
    line=$(percent "$line_rate")
    branch=$(percent "$branch_rate")

    printf '%-24s %8s%% %8s%% %8s%% %8s%%\n' "$scope" "$line" "$min_line" "$branch" "$min_branch"

    if below "$line" "$min_line" || below "$branch" "$min_branch"; then
        echo "Coverage threshold failed for $scope." >&2
        failed=1
    fi
}

root_line=$(grep -m1 '<coverage ' "$XML")
printf '%-24s %8s %8s %8s %8s\n' "Scope" "Line" "Min" "Branch" "Min"
check_scope "src (total)" "$(read_rate "$root_line" line-rate)" "$(read_rate "$root_line" branch-rate)" 90 80

check_package() {
    local name=$1
    local min_line=$2
    local min_branch=$3
    local package_line
    package_line=$(grep -m1 "<package name=\"$name\"" "$XML" || true)

    if [ -z "$package_line" ]; then
        echo "Coverage package not found: $name" >&2
        failed=1
        return
    fi

    check_scope "$name" \
        "$(read_rate "$package_line" line-rate)" \
        "$(read_rate "$package_line" branch-rate)" \
        "$min_line" "$min_branch"
}

check_package CleanWebApiTemplate.Api 85 75
check_package CleanWebApiTemplate.Application 95 90
check_package CleanWebApiTemplate.Domain 100 100
check_package CleanWebApiTemplate.Infrastructure 90 85
check_package CleanWebApiTemplate.ServiceDefaults 95 100

echo
echo "Report: $OUT_DIR/report/index.html"

if [ "$failed" -ne 0 ]; then
    exit 1
fi
