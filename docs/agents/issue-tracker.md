# Issue tracker: GitHub

Las incidencias y specs de este repositorio viven como GitHub Issues. Usa la CLI `gh` para
todas las operaciones.

## Convenciones

- **Crear una incidencia**: `gh issue create --title "..." --body "..."`. Usa un heredoc para
  cuerpos multilínea.
- **Leer una incidencia**: `gh issue view <number> --json number,title,body,labels,comments`.
- **Listar incidencias**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'`
  con los filtros `--label` y `--state` que correspondan.
- **Convertir una incidencia en sub-incidencia de otra**: `gh issue create --parent <parent> ...`,
  o después `gh issue edit <parent> --add-sub-issue <child>` (`gh` 2.94+). En versiones
  anteriores: `gh api --method POST repos/<owner>/<repo>/issues/<parent>/sub_issues -F sub_issue_id=<child-db-id>`
  (el id de base de datos, como en **Bloqueos** más abajo). Sin sub-incidencias, pon
  `Part of #<parent>` al principio del cuerpo de la hija.
- **Bloqueos**: las **dependencias nativas de incidencias** de GitHub, la representación canónica
  y visible en la UI. Añade una arista con `gh issue create --blocked-by <n>,<n> ...` o
  `gh issue edit <child> --add-blocked-by <blocker>` (`gh` 2.94+). En versiones anteriores:
  `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`,
  donde `<blocker-db-id>` es el **id de base de datos** numérico del bloqueador
  (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _no_ el `#number` ni el `node_id`). Donde
  las dependencias no estén disponibles, usa como fallback una línea
  `Blocked by: #<n>, #<n>` al principio del cuerpo de la hija. Una incidencia está desbloqueada
  cuando todos sus bloqueadores están cerrados.
- **Contar bloqueadores abiertos**: `gh api repos/<owner>/<repo>/issues/<n> --jq .issue_dependencies_summary.blocked_by`
  (solo bloqueadores abiertos; 0 significa que se puede empezar). `gh issue view <n> --json blockedBy`
  también lista bloqueadores cerrados, así que fíltralo:
  `--jq '[.blockedBy.nodes[] | select(.state == "OPEN")] | length'`.
- **Comentar una incidencia**: `gh issue comment <number> --body "..."`
- **Aplicar / quitar labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Cerrar**: publica la explicación con `gh issue comment <number> --body "..."` y después
  `gh issue close <number>`. En una incidencia ya cerrada, `gh issue close --comment` descarta el
  comentario y aun así sale con código 0.

Deduce el repositorio de `git remote -v`; `gh` lo hace automáticamente cuando se ejecuta dentro de
un clon.

Abre incidencias solo en el repositorio del usuario. Nunca en `mattpocock/skills` (el repositorio
del que vienen estas skills) salvo que el usuario sea su dueño. En un fork, `gh` puede apuntar por
defecto al repositorio padre, así que antes del primer `gh issue create` de una sesión comprueba el
destino con `gh repo view --json nameWithOwner` y confírmalo con el usuario si no es el suyo.

## Pull requests como superficie de triage

**PRs como superficie de peticiones: no.** _(Ponlo en `yes` si este repositorio trata los PRs
externos como peticiones de funcionalidad; `/triage` lee este flag.)_

Cuando esté en `yes`, los PRs pasan por las mismas labels y estados que las incidencias, usando los
equivalentes de `gh pr`:

- **Leer un PR**: `gh pr view <number>`, después `gh pr view <number> --comments` (fuera de una
  TTY, `--comments` imprime solo los comentarios, no el cuerpo), y `gh pr diff <number>` para el
  diff.
- **Listar PRs externos para triage**: `gh api --paginate 'repos/{owner}/{repo}/pulls?state=open' --jq '.[] | select(.author_association | IN("OWNER","MEMBER","COLLABORATOR") | not) | {number, title, author: .user.login, author_association, labels: [.labels[].name]}'`.
- **Comentar / etiquetar / cerrar**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`,
  `gh pr close`.

GitHub comparte un único espacio de numeración entre incidencias y PRs, así que un `#42` suelto
puede ser cualquiera de los dos: resuélvelo con `gh pr view 42` y recurre a `gh issue view 42`.

## Cuando una skill dice "publica en el issue tracker"

Crea una incidencia de GitHub.

## Cuando una skill dice "recupera la incidencia correspondiente"

Léela como se indica en **Leer una incidencia**.

## Operaciones de Wayfinder

Las usa `/wayfinder`. El **mapa** es una única incidencia con **incidencias hijas** como tickets.

- **Mapa**: una única incidencia etiquetada `wayfinder:map`, que contiene el cuerpo
  Notes / Decisions-so-far / Fog. `gh issue create --label wayfinder:map`.
- **Ticket hijo**: una incidencia enlazada al mapa como sub-incidencia de GitHub (ver
  **Convertir una incidencia en sub-incidencia de otra**). Donde las sub-incidencias no estén
  habilitadas, añade la hija a una task list del cuerpo del mapa y pon `Part of #<map>` al
  principio del cuerpo de la hija. Labels: `wayfinder:<type>`
  (`research`/`prototype`/`grilling`/`task`). Una vez reclamado, el ticket se asigna a quien lo
  conduce.
- **Bloqueos**: ver **Bloqueos** en Convenciones.
- **Consulta de frontera**: lista las hijas abiertas del mapa (`gh issue list --state open`,
  acotado a las sub-incidencias / task list del mapa), descarta las que tengan un bloqueador
  abierto (ver **Contar bloqueadores abiertos**, o una incidencia abierta en la línea
  `Blocked by`) o un asignado; gana la primera en el orden del mapa.
- **Reclamar**: `gh issue edit <n> --add-assignee @me`, la primera escritura de la sesión.
- **Resolver**: `gh issue comment <n> --body "<answer>"`, después `gh issue close <n>`, después
  añade un puntero de contexto (gist + enlace) a Decisions-so-far del mapa.
