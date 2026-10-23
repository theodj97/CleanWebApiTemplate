# Bruno collection aligned with the local data (brunoGen)

*New (user decision).* When the repository keeps a Bruno collection with **brunoGen** (a versioned custom layer that
brunoGen applies on top of the OpenAPI), the agent makes every request of that layer work against the Local mode of
the AppHost with the seeded data: params, queries and bodies point to seeded rows, authentication uses devtoken, and
each request asserts the result the seed coverage table expects. Read this file only when discovery finds
`tools/bruno/bruno.config.json` (or another `bruno.config.json` + `custom/manifest.json`).

Do not introduce Bruno or brunoGen in a repository that does not use them: propose it in the report instead.

## Contents
- [How brunoGen works](#how-brunogen-works)
- [Workflow](#workflow)
- [The local environment](#the-local-environment)
- [Authentication without the login endpoint](#authentication-without-the-login-endpoint)
- [Aligning params and bodies with the seed](#aligning-params-and-bodies-with-the-seed)
- [Populating every request](#populating-every-request)
- [Assertions](#assertions)
- [Verification](#verification)

## How brunoGen works

- `tools/bruno/bruno.config.json`: where the OpenAPI comes from (`"specSource": "auto"` downloads it from the running
  local .NET API, using its `Development` launch profiles), exclusions, folder names, `secrets` rules, `paths` of the
  custom layer.
- `tools/bruno/custom/` is the **only place to edit**: `manifest.json`, `opencollection.yml` (collection auth and
  scripts), `environments/*.yml`, `overrides/` (bodies), `scripts/`, `.env` (git-ignored secrets) and `.env.sample`
  (same keys, no values, versioned).
- `manifest.json` keys requests by `METHOD /route` with `{}` for path parameters (`GET /orders/{}`) or by
  `operationId`, and folders by name. Request fields: `params` (path or query, by name; `{ "value", "enabled": false }`
  to disable, `null` to remove), `headers`, `body` (file in `overrides/`, mode from the extension), `auth`, `vars`
  (`pre-request`, `post-response`), `assertions`, `pre-request`/`after-response`/`tests` scripts, `settings`, `tags`,
  `docs`. File paths are relative to the manifest.
- The generated collection (`<root>/<Solution>Collection/` by default) is git-ignored and rebuilt by every `import`:
  never edit it, its changes are lost (bring them back with `brunoGen export`).
- Commands: `brunoGen import` (generate; copies `custom/.env` into the collection), `brunoGen check` (config,
  manifest keys against the OpenAPI, files, secrets written in the layer, `.env`/`.env.sample` keys; exit `1` on
  problems), `brunoGen export` (collection → custom layer). Exit code `2` = error (for example the API is not running).
- brunoGen is installed per user (`brunoGen --version`). Installing it is a machine-wide change: ask (SKILL.md rule 11).
  The Bruno CLI runs without a global install: `npx -y @usebruno/cli run …`.

## Workflow

After the seed and its coverage table exist (`database.md` § Designing the seed from the endpoints) and the AppHost
runs in Local mode:

1. `brunoGen check` and read the custom layer: environments, collection auth script, every manifest entry and body.
2. Add or update the local environment (§ The local environment) and the authentication branch (§ Authentication
   without the login endpoint).
3. Populate every request of the OpenAPI with seeded data and add assertions (§ Aligning params and bodies with the
   seed, § Populating every request, § Assertions). Add a "Bruno request" column to the seed coverage table so both stay in sync.
4. `dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env`, then `brunoGen import` and `brunoGen check`.
5. Run the collection (§ Verification) and fix the seed or the layer until every request passes. Never fix a failing
   request by changing the application.

## The local environment

One environment for the AppHost's Local mode in `custom/environments/` (reuse an existing `localhost`-style
environment when it already targets the same URL; otherwise create `aspire-local.yml`):

```yaml
name: aspire-local
variables:
  - name: baseUrl
    value: http://localhost:5180            # the URL Aspire gives the API in Local mode (aspire describe <api>)
  - name: authMode
    value: devtoken
  - name: devtokenProfile
    value: orders-admin                     # default profile; requests can override it
  - name: seedPendingOrderId
    value: 5e0d6a1c-0000-4000-8000-000000000001
  - name: seedPaidOrderId
    value: 5e0d6a1c-0000-4000-8000-000000000002
  - name: seedDeletableOrderId
    value: 5e0d6a1c-0000-4000-8000-000000000003
```

- `baseUrl`: the launch-profile URL the AppHost uses (`https` works once the development certificate is trusted;
  otherwise `allowInsecureLocalhost` in the config or the `http` URL).
- Seeded values the layer needs become `seed…` variables of this environment, so other environments (production,
  shared dev) keep their own values. Literal values already in the manifest that belong to another environment move to
  that environment as the same variables; ask when it is unclear which environment they belong to.
- When the existing literals form a coherent data graph, the seed may adopt them as its fixed ids instead (fewer
  changes in the layer).
- Variable names must not look sensitive (`token`, `password`, `secret`, `key`, `email`…): `brunoGen check` treats
  such environment values as secrets (it flagged `tokenSource`; `authMode` passes). Synthetic seeded values are not
  secrets; if a name cannot change, add it to `secrets.ignore` in the config rather than moving data to `.env`.

## Authentication without the login endpoint

SKILL.md rule 9 forbids obtaining local tokens from login endpoints, so in the local environment the collection takes
devtoken tokens; other environments keep their own mechanism (for example an auto-login script). Collection auth is
`bearer` with `{{token}}`, and the collection `before-request` script starts with:

```js
// aspire-local: tokens come from devtoken (DEVTOKEN_<PROFILE> in .env), never from the login endpoint.
// A request picks another profile with the request variable devtokenProfile.
if (bru.getEnvVar('authMode') === 'devtoken') {
  const profile = bru.getRequestVar('devtokenProfile') || bru.getEnvVar('devtokenProfile');
  const key = 'DEVTOKEN_' + String(profile).toUpperCase().replace(/[^A-Z0-9]/g, '_');
  const token = bru.getProcessEnv(key);
  if (!token) {
    throw new Error('[devtoken] Missing ' + key + ' in .env. Run: dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env, then brunoGen import.');
  }
  bru.setVar('token', token);
} else {
  // ADAPT: the existing auto-login (or other) logic for the remaining environments, unchanged.
}
```

- `dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env [--lifetime <minutes, max 1440>]` upserts one
  `DEVTOKEN_<PROFILE>` per devtoken profile and keeps the other lines; `brunoGen import` copies the file into the
  collection. Repeat both when tokens expire. List the `DEVTOKEN_*` keys (without values) in `.env.sample`.
- A request that needs another identity sets it in the manifest:
  `"vars": { "pre-request": { "devtokenProfile": "orders-customer" } }`. Permission-less profiles (403 checks) work
  the same way.
- The application's own login request stays in the collection as an ordinary endpoint: `"auth": "none"`, body with a
  seeded user and its synthetic password from `.env` (`{{process.env.LOCAL_CUSTOMER_PASSWORD}}`: `check` rejects
  literal passwords in bodies).
- Tokens live only in the git-ignored `.env` files (devtoken writes them with mode 600); never in environments,
  bodies, the manifest or docs.

## Aligning params and bodies with the seed

For every request (the manifest may not have an entry yet: add one), using the endpoint inventory and the seed
coverage table:

| Request part | Must reference |
|---|---|
| Path params of detail/update/delete/action routes | a seeded id in the state the operation accepts (`{{seedPaidOrderId}}` for "ship") |
| Destructive or state-changing requests | a row seeded for that purpose only (`{{seedDeletableOrderId}}`), so the other requests stay valid in the same session |
| Query params (filters, paging, date ranges) | values that match seeded rows; date ranges that contain the seed's relative dates; page sizes smaller than the seeded count when paging is tested |
| Body foreign keys and references | seeded ids (`{{seed…}}` variables) |
| Body unique fields (codes, names, emails) | new values that do not collide with the seed |
| Body enums, formats, required fields | valid values per the application's validators, so the request reaches the business logic |
| Login/user bodies | a seeded synthetic user; secrets through `{{process.env.…}}` |
| Optional params irrelevant to the main case | disabled (`{ "value": "…", "enabled": false }`), not deleted |

State-changing requests are one-shot per AppHost start: a second run in the same session fails by design (verified:
delete 404, ship 409), and `aspire stop` + `aspire start` reinstalls the seed. Say so in `local/local-development.md`.

## Populating every request

The goal is a collection where every endpoint can be tried in Bruno right after `aspire run`, without typing data:

- **Every request with path params, query params or a body gets a manifest entry**, including requests the manifest
  does not mention yet. brunoGen generates them with empty values (`"amount": ""`, `id: ""`): none may stay empty in
  the local environment.
- **Bodies are complete files** in `custom/overrides/`, named like the existing ones (`METHOD_route.body.json`, for
  example `POST_api_documento.body.json`), with every required field and the optional fields that make the main case
  meaningful, filled with seeded references (`{{seed…}}`) and valid new values. Reuse an existing override and only
  replace the values that must come from the seed.
- **Query params**: the ones the main case needs are filled and enabled; the others are filled with valid seeded
  values but disabled, so trying a filter is one click.
- **Requests without params or body** (lists, dashboards) still get their assertions.
- **The seed may need more rows**: when a request cannot be populated because the seed lacks the data (a state, a
  relation, a second page), add the rows to the seed and the coverage table first (`database.md`), then populate it.
- **Keep it in sync**: every later change of the seed (new rows, a new endpoint, a migration, new devtoken profiles)
  updates the Bruno layer in the same change, and a request for "add test data for X" includes the Bruno requests of X.
- **Check that nothing is left empty** after `brunoGen import`:
  `python3 <skill>/assets/bruno/find-empty-values.py <Solution>Collection` (exit 0). It reads only the `http` section of
  each request (response examples also contain empty strings and are ignored). Without Python/PyYAML, inspect the
  `http.params` and `http.body` of every request file instead.

## Assertions

Each request asserts what its seed coverage row expects, so a run proves the alignment instead of "it answered":

```json
"GET /orders/{}": {
  "params": { "id": "{{seedPendingOrderId}}" },
  "assertions": { "res.status": "eq 200", "res.body.status": "eq Pending" }
}
```

At least `res.status` for every request; for lists a minimum size (`"res.body.length": "gte 4"`) or a seeded value.

## Verification

```bash
dotnet local/tools/devtoken.cs dotenv tools/bruno/custom/.env
brunoGen import && brunoGen check                 # check exit 0
cd <Solution>Collection && npx -y @usebruno/cli run --env aspire-local
```

- No request keeps an empty param or body field (`find-empty-values.py` exit 0).
- Every request passes with its assertions on a fresh AppHost start (record "N requests, N passed, M/M assertions").
- `brunoGen check` exit 0: manifest keys exist in the OpenAPI, no secret in the layer, `.env` and `.env.sample` in sync.
- No request obtains its token from a login endpoint; the collection never targets an identity provider in the local
  environment.
- `git status` shows changes only under `tools/bruno/` (plus the seed and docs); no `.env` or generated collection is
  tracked.
