# Rehi API verification map

This directory is the maintained source for verifying what Rehi API clients (the web app and the browser extension) can do. Read this index before driving, then use the matching feature file as the recipe.

## Baseline preconditions

- `scripts/verify.sh up` started the instance, and `scripts/verify.sh doctor` prints only `OK` lines.
- The database is the fresh tmpfs one that `up` created. It holds the five seeded `SubscriptionPlans` and no users.
- Authenticated journeys need `REHI_VERIFY_TOKEN` holding a token whose email claim matches a row in `Users`. The [Native auth](./native-auth.md) journey registers `verify@rehi.test`, signs in, and exports that token (see `SKILL.md`, Authentication).
- Never drive an instance this run did not start. The dev's stack on `:5204` and `:5432` is off limits.

## Driving conventions

- Start every journey from the baseline unless its preconditions say otherwise.
- Drive with `scripts/verify.sh call <name> ...`, one evidence name per user action.
- Seed through the API only. `scripts/verify.sh sql` is for reading.
- Timestamps in bodies and routes are Unix epoch milliseconds.

## Proof and skip reporting

- Capture each request and its response, not only the last one.
- Every mutation gets a second, read-only view: a GET or a SQL read.
- Auth and validation changes include the negative case.
- Report a blocked journey with the precondition it lacks. Do not report a blocked entry point as verified through a different path.

## Feature entry contract

Each feature file starts with an H1 title and one paragraph on the client-visible behavior, then exactly four H2 sections in this order: `Sub-features`, `How to get to it (user POV)`, `Driving it with verify.sh`, `Gotchas`.

## Features

- [API boot and schema](./api-boot.md): startup from an empty database, migrations, and the Swagger contract. Runnable now.
- [Auth gate](./auth-gate.md): protected routes reject anonymous calls and forged tokens, accept Rehi tokens, and webhooks stay anonymous. Runnable now.
- [Native auth](./native-auth.md): register, verify email, login, refresh rotation and reuse, logout, and rate limits. Runnable now, and the token source for the journeys below.
- [Save an article](./save-article.md): saving or fetching an article by URL, and reading it back through state sync. Needs the native-auth token.
- [Tags](./tags.md): creating a tag, assigning it to an article, and deleting it. Needs the native-auth token.
- [Highlights](./highlights.md): creating a highlight on a saved article, adding a note, and deleting it. Needs the native-auth token.
