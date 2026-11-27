---
name: verify-rehi-backend
description: Launch an isolated Rehi API (ASP.NET Core 8 + PostgreSQL 17) and drive it over HTTP to prove backend behavior with real responses and DB state. Use after changing endpoints, handlers, EF migrations, auth, or startup in Rehi-backend, before claiming a backend change works.
---

# Verify Rehi backend

Rehi-backend is a REST API. Its user is an HTTP client (the web app or the browser extension), so you prove behavior with curl against a running instance and read-only SQL against its database. Everything goes through `scripts/verify.sh`. Run it from the repo root:

```bash
S=.claude/skills/verify-rehi-backend/scripts/verify.sh
```

The instance is isolated from the dev's own stack. The dev usually has a Docker stack running on `5204` (API), `5432` (postgres with real data), and `5341` (Seq). Never drive, reset, or query those. The script uses its own ports and a throwaway database:

| | Verify instance | Dev stack (do not touch) |
|---|---|---|
| API | `http://127.0.0.1:5299` | `:5204` |
| Postgres | container `rehi-verify-pg-55432` on `127.0.0.1:55432`, tmpfs | container `postgres` on `:5432` |
| Logs | `~/.cache/rehi-verify/run-5299/api.log` (Seq sink pointed at a dead port) | Seq `:5341` |

For a second instance at the same time (another worktree), set `REHI_VERIFY_API_PORT` and `REHI_VERIFY_PG_PORT` on every command.

## Launch

```bash
$S up
```

It starts `postgres:17` on tmpfs, waits until it accepts TCP queries, runs `dotnet build` on `Rehi.Apis`, then starts the built `Rehi.Apis.dll` in `Development` with the connection string, Seq URL, and SMTP credentials overridden. The API applies all EF migrations before it listens. Ready is the line `verify: ready` with the evidence path. It also means `/swagger/v1/swagger.json` answered 200 with the schema in place. A cold run takes about 10 seconds after the first build.

`up` refuses to start if this port pair already has a running instance, or if either port is taken by something it did not start. Reuse the running instance or pick other ports. Never kill the other process.

## Doctor

```bash
$S doctor
```

Read-only. Every line must say `OK`:

- the API pid is ours and holds the port
- `swagger.json` answers 200
- `/states` without a token answers 401, which proves the auth pipeline is on
- the verify postgres container is running
- `__EFMigrationsHistory` row count equals the migration files in `Rehi.Infrastructure/Database/Migrations`

Run it first whenever anything looks off. A `FAIL` on migrations after you added one means the new migration did not apply. Read `api.log`.

## Drive

```bash
$S call <evidence-name> [curl args...] /path
```

Arguments that start with `/` are expanded to the verify base URL. Each call saves `<name>.cmd`, `.status`, `.headers`, and `.body` in the evidence directory, then prints the status and the start of the body. If `REHI_VERIFY_TOKEN` is set, the script adds `Authorization: Bearer $REHI_VERIFY_TOKEN` and keeps the token out of `.cmd`.

```bash
$S call swagger /swagger/v1/swagger.json
$S call states-no-token /states
$S call create-tag -X POST -H 'Content-Type: application/json' -d '{"name":"verify","createAt":1795000000000}' /tags
```

Read the resulting state back with SQL. Table and column names are PascalCase, so quote them:

```bash
$S sql 'select "Id","Email" from "Users"'
$S sql 'select count(*) from "Articles"'
```

Use `sql` only to observe. Seeding state must go through the API, the same path a user takes.

Timestamps in request bodies (`createAt`, `updateAt` and the route segments with those names) are Unix epoch milliseconds.

### Authentication

Every endpoint except `/payos/webhook` and `/paypal/webhook` calls `RequireAuthorization()`. The API resolves the caller by the token's email claim and looks that email up in `Users`. Today the only accepted tokens are Auth0 access tokens. This skill has no way to mint one, so every authenticated journey in `features/` is **BLOCKED** until a token source exists. Report it as blocked, with the journey name. Never report it as passed. Do not work around it with a test-only endpoint, a relaxed auth scheme, or a hand-forged JWT.

When native email/password login lands, add its register and login journey to `features/`. That journey sets `REHI_VERIFY_TOKEN`, and the blocked journeys become runnable.

## Evidence

Evidence lives in `~/.cache/rehi-verify/evidence/<run-id>/` (`$S evidence` prints it). It survives `down`, and `down` also copies `api.log` into it. A proof includes:

- the request (`.cmd`) and the response status and body for each user action
- a second, read-only view of every mutation: a follow-up GET, or `$S sql` output saved with `> "$($S evidence)/<name>.sql.txt"`
- for auth or validation changes, the negative case too (401 without a token, 400 for an invalid body)
- the `doctor` output from the same run

Exercise the real endpoint a client calls. Mock nothing. Chat (`/chat`, `/chat-with-article`) and article indexing need Azure OpenAI and Azure AI Search, which are empty in `appsettings.Development.json`. Report those paths as unverifiable locally. Do not stub them.

## Cleanup

```bash
$S down
```

It stops only the API pid it recorded, removes only the postgres container that carries its `rehi-verify=1` label, and deletes the run state. It never kills by process name, and it never removes evidence. Run it after every attempt, failed ones included.

## Feature map

`features/README.md` indexes the user-facing journeys. Use the matching file as the recipe, and add or update a file when you change a feature.
