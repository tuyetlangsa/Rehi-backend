# Auth gate

Every client route requires a bearer token, and a call without one gets `401` before any handler runs. The payment webhooks are the deliberate exception: payOS and PayPal call them without a token.

## Sub-features

- `gate-anonymous-401` rejects protected routes without a token.
- `gate-bad-token-401` rejects a malformed or foreign token.
- `gate-webhooks-anonymous` keeps `/payos/webhook` and `/paypal/webhook` reachable without a token.

## How to get to it (user POV)

- A signed-out client, or a client with an expired token, calls any API route.
- A payment provider posts to a webhook.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- `REHI_VERIFY_TOKEN` is unset.

- **Anonymous read.** Run `scripts/verify.sh call states-anon /states`. Status `401`.
- **Anonymous write.** Run `scripts/verify.sh call tags-anon -X POST -H 'Content-Type: application/json' -d '{"name":"x","createAt":1795000000000}' /tags`. Status `401`.
- **Garbage token.** Run `scripts/verify.sh call states-garbage -H 'Authorization: Bearer not-a-jwt' /states`. Status `401`.
- **No side effect.** Run `scripts/verify.sh sql 'select count(*) from "Tags"'`. It prints `0`.
- **Webhook reachability.** Run `scripts/verify.sh call payos-anon -X POST -H 'Content-Type: application/json' -d '{}' /payos/webhook`. Status `200` with body `{"isSuccess":true,"data":{"status":"failed"}}`. The handler rejected the empty payload, which proves the request got past the auth gate. Any status except `401` passes this step.
- **Proof.** The evidence holds the four `.status` files and the SQL count.

## Gotchas

- Swagger UI's Authorize button does not mint tokens. It only sends one you paste.
- When a new auth scheme lands, such as native login, rerun this whole journey. A second scheme added as the default can quietly make a protected route anonymous.
- The webhook call sends an empty payload on purpose. Never post a realistic payload, because a valid-looking one can change subscription rows.
