#!/bin/sh
# Formatea los .cs que están en staging y, si ha cambiado algo, aborta el commit
# para que revises el resultado y vuelvas a commitear.
# También bloquea el commit si queda algún error de estilo no corregible (IDE0060),
# sin modificar los ficheros.
# No hace `git add`: los arreglos se quedan en el working tree a propósito.
# Saltar el hook: HUSKY=0 git commit ... (o git commit --no-verify).
set -eu
set -f

solution="CleanWebApiTemplate.slnx"

# Solo ficheros añadidos/copiados/modificados/renombrados: no hay nada que
# formatear en los borrados.
staged=$(git diff --cached --name-only --diff-filter=ACMR -- '*.cs')
[ -n "$staged" ] || exit 0

# Huella del contenido actual para detectar si `dotnet format` toca algo.
# Se usa `git hash-object` (sin filtros de path) para no depender de sha256sum
# ni de shasum, que no existen en todas las plataformas.
fingerprint() {
    printf '%s\n' "$staged" | while IFS= read -r file; do
        if [ -f "$file" ]; then
            git hash-object -- "$file"
        fi
    done
}

before=$(fingerprint)

# `$staged` va sin comillas a propósito: `--include` espera una lista de rutas.
dotnet format whitespace "$solution" --include $staged --verbosity minimal
# IDE0060 se excluye a propósito: no debe modificarse ni eliminarse ningún
# parámetro. La regla se comprueba después con `--verify-no-changes`.
dotnet format style "$solution" --include $staged --verbosity minimal \
    --exclude-diagnostics IDE0060

after=$(fingerprint)

if [ "$before" != "$after" ]; then
    printf '\n' >&2
    printf 'Husky ha aplicado cambios de formato en los .cs que ibas a commitear.\n' >&2
    printf 'Revisalos con:            git diff\n' >&2
    printf 'Y vuelve a commitear con: git add -u && git commit\n' >&2
    printf '\n' >&2
    exit 1
fi

# Si el working tree no coincide con el index en esos ficheros, el commit
# llevaría la versión sin formatear: mejor bloquear.
if ! git diff --quiet -- $staged; then
    printf '\n' >&2
    printf 'Husky no puede formatear: estos .cs tienen cambios sin stagear:\n' >&2
    printf '%s\n' "$staged" | while IFS= read -r file; do
        if ! git diff --quiet -- "$file"; then
            printf '    %s\n' "$file" >&2
        fi
    done
    printf 'Stagealos (git add -u) o descarta esos cambios (git restore) y reintenta.\n' >&2
    printf '\n' >&2
    exit 1
fi

# IDE0060 (parámetro sin usar) no tiene code fix: `dotnet format` no puede
# corregirlo. Se verifica aparte para bloquear el commit sin tocar el fichero:
# `--verify-no-changes` solo analiza.
if ! dotnet format style "$solution" --include $staged --no-restore \
        --severity error --verify-no-changes --verbosity minimal; then
    printf '\n' >&2
    printf 'Husky ha detectado parámetros sin usar (IDE0060) en los .cs staged.\n' >&2
    printf 'No se ha modificado ningún fichero: elimina el parámetro o renómbralo a `_`/`_1`.\n' >&2
    printf '\n' >&2
    exit 1
fi
