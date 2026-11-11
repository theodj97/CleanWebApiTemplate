#!/bin/sh
# Formats the staged .cs files and aborts the commit when something changed, so
# you can review the result and commit again.
# It also blocks the commit when a style error that cannot be fixed (IDE0060)
# remains, without modifying the files.
# It never runs `git add`: the fixes stay in the working tree on purpose.
# Skip the hook: HUSKY=0 git commit ... (or git commit --no-verify).
set -eu

solution="CleanWebApiTemplate.slnx"

# Only added/copied/modified/renamed files: there is nothing to format in
# deletions. The NUL-separated list supports paths with spaces.
staged=$(git diff --cached --name-only --diff-filter=ACMR -- '*.cs')
[ -n "$staged" ] || exit 0

list=$(mktemp)
trap 'rm -f "$list"' EXIT
git diff --cached --name-only -z --diff-filter=ACMR -- '*.cs' > "$list"

# Fingerprint of the current content to detect whether `dotnet format` changes
# anything. `git hash-object` is used (without path filters) so the script does
# not depend on sha256sum or shasum, which are not available everywhere.
fingerprint() {
    printf '%s\n' "$staged" | while IFS= read -r file; do
        if [ -f "$file" ]; then
            git hash-object -- "$file"
        fi
    done
}

before=$(fingerprint)

# `xargs -0` passes each path as a separate argument: spaces do not break the
# list. `--include` goes last so it consumes exactly those paths.
xargs -0 dotnet format whitespace "$solution" --no-restore --verbosity minimal --include < "$list"
# IDE0060 is excluded on purpose: no parameter should be modified or removed.
# The rule is checked afterwards with `--verify-no-changes`.
xargs -0 dotnet format style "$solution" --no-restore --verbosity minimal \
    --exclude-diagnostics IDE0060 --include < "$list"

after=$(fingerprint)

if [ "$before" != "$after" ]; then
    printf '\n' >&2
    printf 'Husky applied formatting changes to the .cs files you were about to commit.\n' >&2
    printf 'Review them with:      git diff\n' >&2
    printf 'Then commit again:     git add -u && git commit\n' >&2
    printf '\n' >&2
    exit 1
fi

# If the working tree does not match the index for those files, the commit
# would carry the unformatted version: block it instead.
unstaged=$(printf '%s\n' "$staged" | while IFS= read -r file; do
    if ! git diff --quiet -- "$file"; then
        printf '%s\n' "$file"
    fi
done)

if [ -n "$unstaged" ]; then
    printf '\n' >&2
    printf 'Husky cannot format: these .cs files have unstaged changes:\n' >&2
    printf '%s\n' "$unstaged" | while IFS= read -r file; do
        printf '    %s\n' "$file" >&2
    done
    printf 'Stage them (git add -u) or discard those changes (git restore) and retry.\n' >&2
    printf '\n' >&2
    exit 1
fi

# IDE0060 (unused parameter) has no code fix: `dotnet format` cannot correct
# it. It is verified separately to block the commit without touching the file:
# `--verify-no-changes` only analyses. `--diagnostics` limits the check to
# IDE0060 so the message does not blame other error-severity rules.
if ! xargs -0 dotnet format style "$solution" --no-restore \
        --diagnostics IDE0060 --severity error --verify-no-changes --verbosity minimal \
        --include < "$list"; then
    printf '\n' >&2
    printf 'Husky found unused parameters (IDE0060) in the staged .cs files.\n' >&2
    printf 'No file was modified: remove the parameter or rename it to `_`/`_1`.\n' >&2
    printf '\n' >&2
    exit 1
fi
