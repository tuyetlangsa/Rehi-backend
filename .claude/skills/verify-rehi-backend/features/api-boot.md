# API boot and schema

A fresh deployment of the API starts from an empty database, applies every EF migration, seeds the subscription plans, and publishes its HTTP contract at `/swagger/v1/swagger.json`. Clients depend on that contract, so route changes show up here first.

## Sub-features

- `boot-empty-db` starts and listens with no existing schema.
- `boot-migrations` applies every migration in `Rehi.Infrastructure/Database/Migrations`.
- `boot-seed` seeds the five `SubscriptionPlans` rows.
- `boot-contract` lists every mapped route in Swagger.

## How to get to it (user POV)

- A deploy or a local start of `Rehi.Apis` against an empty database.
- A client or developer reading `/swagger/v1/swagger.json` or `/swagger`.

## Driving it with verify.sh

Preconditions:

- No verify instance runs on the chosen ports.

- **Start from empty.** Run `scripts/verify.sh up`. It prints `verify: ready` and an evidence path.
- **Migrations applied.** Run `scripts/verify.sh doctor`. The line `OK   migrations applied: N/N` shows the applied count equal to the file count.
- **Seed present.** Run `scripts/verify.sh sql 'select count(*) from "SubscriptionPlans"'`. It prints `5`.
- **Contract published.** Run `scripts/verify.sh call swagger /swagger/v1/swagger.json`. Status `200`. The body's `paths` object holds every route you expect, including any route you just added.
- **Proof.** Save `doctor` output with `scripts/verify.sh doctor > "$(scripts/verify.sh evidence)/doctor.txt"`. The evidence directory then holds `doctor.txt`, `swagger.body`, and after `down`, `api.log` with `Now listening on: http://127.0.0.1:5299`.

## Gotchas

- `api.log` always starts with one `ERR Failed executing DbCommand` on `__EFMigrationsHistory`. EF probes the history table before it creates it. Any other `ERR` during startup is real.
- A new migration that the API does not pick up still lets the API boot. Only the `doctor` count catches it.
- Swagger lists routes, not auth requirements. Check auth on the [Auth gate](./auth-gate.md) journey.
