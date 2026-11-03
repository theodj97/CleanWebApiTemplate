#!/bin/sh
# Formatea los .cs que están en staging y, si ha cambiado algo, aborta el commit
# para que revises el resultado y vuelvas a commitear.
# También bloquea el commit si queda algún error de estilo no corregible (IDE0060),
# sin modificar los ficheros.
# No hace `git add`: los arreglos se quedan en el working tree a propósito.
# Saltar el hook: HUSKY=0 git commit ... (o git commit --no-verify).
set -eu

solution="CleanWebApiTemplate.slnx"

# Solo ficheros añadidos/copiados/modificados/renombrados: no hay nada que
# formatear en los borrados. La lista con NUL soporta rutas con espacios.
staged=$(git diff --cached --name-only --diff-filter=ACMR -- '*.cs')
[ -n "$staged" ] || exit 0

list=$(mktemp)
trap 'rm -f "$list"' EXIT
git diff --cached --name-only -z --diff-filter=ACMR -- '*.cs' > "$list"

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

# `xargs -0` pasa cada ruta como un argumento distinto: los espacios no rompen
# la lista. `--include` va al final para que consuma exactamente esas rutas.
xargs -0 dotnet format whitespace "$solution" --no-restore --verbosity minimal --include < "$list"
# IDE0060 se excluye a propósito: no debe modificarse ni eliminarse ningún
# parámetro. La regla se comprueba después con `--verify-no-changes`.
xargs -0 dotnet format style "$solution" --no-restore --verbosity minimal \
    --exclude-diagnostics IDE0060 --include < "$list"

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
unstaged=$(printf '%s\n' "$staged" | while IFS= read -r file; do
    if ! git diff --quiet -- "$file"; then
        printf '%s\n' "$file"
    fi
done)

if [ -n "$unstaged" ]; then
    printf '\n' >&2
    printf 'Husky no puede formatear: estos .cs tienen cambios sin stagear:\n' >&2
    printf '%s\n' "$unstaged" | while IFS= read -r file; do
        printf '    %s\n' "$file" >&2
    done
    printf 'Stagealos (git add -u) o descarta esos cambios (git restore) y reintenta.\n' >&2
    printf '\n' >&2
    exit 1
fi

# IDE0060 (parámetro sin usar) no tiene code fix: `dotnet format` no puede
# corregirlo. Se verifica aparte para bloquear el commit sin tocar el fichero:
# `--verify-no-changes` solo analiza. `--diagnostics` limita la comprobación a
# IDE0060 para que el mensaje no acuse a otras reglas de severidad error.
if ! xargs -0 dotnet format style "$solution" --no-restore \
        --diagnostics IDE0060 --severity error --verify-no-changes --verbosity minimal \
        --include < "$list"; then
    printf '\n' >&2
    printf 'Husky ha detectado parámetros sin usar (IDE0060) en los .cs staged.\n' >&2
    printf 'No se ha modificado ningún fichero: elimina el parámetro o renómbralo a `_`/`_1`.\n' >&2
    printf '\n' >&2
    exit 1
fi
