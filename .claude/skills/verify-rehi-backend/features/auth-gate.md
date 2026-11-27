# Auth gate

Every client route requires a bearer token, and a call without one gets `401` before any handler runs. The API accepts two token kinds. It sends a token whose `iss` is `Jwt:Issuer` to the `Rehi` scheme and every other token to the `Auth0` scheme, and each scheme validates the token fully. The payment webhooks and the `/auth/*` endpoints are the deliberate exceptions: payOS and PayPal call the webhooks without a token, and a signed-out client calls `/auth/*`.

## Sub-features

- `gate-anonymous-401` rejects protected routes without a token.
- `gate-bad-token-401` rejects a malformed or foreign token.
- `gate-webhooks-anonymous` keeps `/payos/webhook` and `/paypal/webhook` reachable without a token.
- `gate-rehi-token-200` accepts a Rehi access token from [Native auth](./native-auth.md).
- `gate-forged-rehi-401` rejects a Rehi access token whose signature was changed.

## How to get to it (user POV)

- A signed-out client, or a client with an expired token, calls any API route.
- A payment provider posts to a webhook.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- `REHI_VERIFY_TOKEN` is unset.
- For the two Rehi token steps, [Native auth](./native-auth.md) ran through **Login** in this instance, and `TOKEN="$(jq -r .data.accessToken "$(scripts/verify.sh evidence)/login.body")"`.

- **Anonymous read.** Run `scripts/verify.sh call states-anon /states`. Status `401`.
- **Anonymous write.** Run `scripts/verify.sh call tags-anon -X POST -H 'Content-Type: application/json' -d '{"name":"x","createAt":1795000000000}' /tags`. Status `401`.
- **Garbage token.** Run `scripts/verify.sh call states-garbage -H 'Authorization: Bearer not-a-jwt' /states`. Status `401`.
- **No side effect.** Run `scripts/verify.sh sql 'select count(*) from "Tags"'`. It prints `0`.
- **Webhook reachability.** Run `scripts/verify.sh call payos-anon -X POST -H 'Content-Type: application/json' -d '{}' /payos/webhook`. Status `200` with body `{"isSuccess":true,"data":{"status":"failed"}}`. The handler rejected the empty payload, which proves the request got past the auth gate. Any status except `401` passes this step.
- **Rehi token.** Run `REHI_VERIFY_TOKEN="$TOKEN" scripts/verify.sh call gate-rehi-token /states`. Status `200`.
- **Tampered Rehi token.** Change the first character of the signature segment: `SIG="${TOKEN##*.}"; [[ "${SIG:0:1}" == A ]] && C=B || C=A; FORGED="${TOKEN%.*}.${C}${SIG:1}"`. Run `REHI_VERIFY_TOKEN="$FORGED" scripts/verify.sh call gate-forged-rehi /states`. Status `401`, and `gate-forged-rehi.headers` holds `WWW-Authenticate: Bearer error="invalid_token"`. `api.log` shows `Rehi was not authenticated` with `IDX10517: Signature validation failed`, which proves the issuer routed the token to the `Rehi` scheme and the signature check rejected it.
- **Proof.** The evidence holds the six `.status` files and the SQL count.

## Gotchas

- Swagger UI's Authorize button does not mint tokens. It only sends one you paste.
- When an auth scheme changes, rerun this whole journey. A second scheme added as the default can quietly make a protected route anonymous.
- Change the first signature character, not the last. The last base64url character of an HS256 signature carries 2 padding bits, so some edits there decode to the same signature.
- Never hand-write a JWT for this journey. The forged token is a real token from the running instance with one signature character changed.
- No step here sends an Auth0 token, because this skill cannot mint one. The `Auth0` scheme still handles the garbage token: `api.log` shows `Auth0 was not authenticated` for `states-garbage`.
- The webhook call sends an empty payload on purpose. Never post a realistic payload, because a valid-looking one can change subscription rows.
