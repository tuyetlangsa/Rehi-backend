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
| Mail | container `rehi-verify-mailpit-51025`, SMTP `127.0.0.1:51025`, HTTP `http://127.0.0.1:58025` | Gmail SMTP from `appsettings` |

For a second instance at the same time (another worktree), set `REHI_VERIFY_API_PORT`, `REHI_VERIFY_PG_PORT`, `REHI_VERIFY_SMTP_PORT`, and `REHI_VERIFY_MAIL_PORT` on every command.

## Launch

```bash
$S up
```

It starts `postgres:17` on tmpfs and `axllent/mailpit:v1.21.8`, waits until postgres accepts TCP queries and Mailpit's HTTP API answers, runs `dotnet build` on `Rehi.Apis`, then starts the built `Rehi.Apis.dll` in `Development` with the connection string and Seq URL overridden, SMTP pointed at Mailpit (`SendEmail__Host`, `__Port`, `__Security=None`, dummy user and password), and the Rehi token settings set by env: a random 64-character `Jwt__Secret` for each run, `Jwt__Issuer=rehi-verify`, `Jwt__Audience=rehi-verify-api`, and `Rehi__WebBaseUrl=http://localhost:3000`. The API refuses to start when `Jwt:Secret` is shorter than 32 bytes or when `Jwt:Issuer`, `Jwt:Audience`, or `Rehi:WebBaseUrl` is missing. Mailpit accepts any SMTP login, so no email the verify instance sends ever reaches Gmail. The API applies all EF migrations before it listens. Ready is the line `verify: ready` with the evidence path. It also means `/swagger/v1/swagger.json` answered 200 with the schema in place. A cold run takes about 10 seconds after the first build.

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
- the Mailpit container is running and `/api/v1/info` answers 200
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

Use `sql` only to observe. Seeding state must go through the API, the same path a user takes. The one exception is the passwordless row an Auth0 sign-in creates: `POST /users` creates only the caller's own row, so without an Auth0 token the journeys insert that row with `sql`.

Timestamps in request bodies (`createAt`, `updateAt` and the route segments with those names) are Unix epoch milliseconds.

### Authentication

Every endpoint except `/payos/webhook`, `/paypal/webhook`, and `/auth/*` calls `RequireAuthorization()`. The API resolves the caller by the token's email claim and looks that email up in `Users`. It accepts two token kinds. A Rehi access token is an HS256 JWT that this instance issues at `POST /auth/login`. An Auth0 access token comes from the web app's Auth0 sign-in, and this skill cannot mint one.

Get a token from the [Native auth](features/native-auth.md) journey. It registers `verify@rehi.test`, reads the verification link from Mailpit, signs in, and exports `REHI_VERIFY_TOKEN`. Every authenticated journey in `features/` runs with that token. A path that needs an Auth0 token is unverifiable here. Report it as unverifiable, with the journey name. Do not work around it with a test-only endpoint, a relaxed auth scheme, or a hand-forged JWT.

### Email

Every email the instance sends lands in Mailpit. Read a token out of the newest email to an address:

```bash
$S mail-token user@example.com verify   # token from {Rehi:WebBaseUrl}/verify-email?token=...
$S mail-token user@example.com reset    # token from {Rehi:WebBaseUrl}/reset-password?token=...
```

It prints only the token, so `TOKEN="$($S mail-token ...)"` feeds the next `call`. It saves the message JSON as `mail-<kind>-<to>.json` in the evidence directory. It fails when the address has no email or the newest one has no link of that kind. Browse the inbox at `http://127.0.0.1:58025`.

`$S mail-selftest` sends one message over SMTP with both link shapes to a fresh address and checks that `mail-token` reads both tokens back. Register and resend-verification send verify emails through the outbox, so a verify link lands within about 5 s. Forgot-password sends reset emails the same way (see [Password reset](features/password-reset.md)).

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

It stops only the API pid it recorded, removes only the postgres and Mailpit containers that carry its `rehi-verify=1` label, and deletes the run state. It never kills by process name, and it never removes evidence. Run it after every attempt, failed ones included.

## Feature map

`features/README.md` indexes the user-facing journeys. Use the matching file as the recipe, and add or update a file when you change a feature.
