#!/usr/bin/env bash
# Validates a commit subject against the repository convention:
#   feat|fix|refactor|docs|test|chore|build|perf|revert(scope): description
# It allows the messages Git generates (Merge, Revert) and fixup!/squash!.
set -euo pipefail

message_file=${1:?usage: commit-msg.sh <message-file>}
subject=$(head -n 1 "$message_file")

case "$subject" in
    Merge\ *|Revert\ *|fixup!\ *|squash!\ *)
        exit 0
        ;;
esac

pattern='^(feat|fix|refactor|docs|test|chore|build|perf|revert)(\([a-z0-9-]+\))?: .{1,72}$'

if ! printf '%s\n' "$subject" | grep -Eq "$pattern"; then
    cat >&2 <<'EOF'
The commit subject does not follow the convention:

    <type>(<optional scope>): <description of 1 to 72 characters>

Types: feat, fix, refactor, docs, test, chore, build, perf, revert.
Examples:
    feat(todo): add title uniqueness per owner
    fix(auth): reject tokens without email
    docs: document the batch workflow

Write the message on one line (the body goes after it, separated by a blank
line). To skip the hook: HUSKY=0 git commit ... or git commit --no-verify.
EOF
    exit 1
fi
