#!/usr/bin/env bash
# Valida el asunto de un commit con la convención del repositorio:
#   feat|fix|refactor|docs|test|chore|build|perf|revert(scope): descripción
# Permite los mensajes que genera Git (Merge, Revert) y los fixup!/squash!.
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
El asunto del commit no sigue la convención:

    <tipo>(<ámbito opcional>): <descripción de 1 a 72 caracteres>

Tipos: feat, fix, refactor, docs, test, chore, build, perf, revert.
Ejemplos:
    feat(todo): add title uniqueness per owner
    fix(auth): reject tokens without email
    docs: document the batch workflow

Escribe el mensaje en una línea (el cuerpo va después, separado por una línea
en blanco). Para saltar el hook: HUSKY=0 git commit ... o git commit --no-verify.
EOF
    exit 1
fi
