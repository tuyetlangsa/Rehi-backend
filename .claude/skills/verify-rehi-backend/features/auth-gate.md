# Auth gate

Every client route requires a bearer token, and a call without one gets `401` before any handler runs. The API accepts two token kinds. It sends a token whose `iss` is `Jwt:Issuer` to the `Rehi` scheme and every other token to the `Auth0` scheme, and each scheme validates the token fully. The payment webhooks and the `/auth/*` endpoints are the deliberate exceptions: payOS and PayPal call the webhooks without a token, and a signed-out client calls `/auth/*`.

## Sub-features

- `gate-anonymous-401` rejects protected routes without a token.
- `gate-bad-token-401` rejects a malformed or foreign token.
- `gate-unreadable-bearer` answers every bearer string the API cannot read like a missing token: `401` on a protected route, and the normal answer on an anonymous one, never `500`.
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
- **Unreadable bearers.** For each of these bearer strings, call `/states`, `POST /auth/verify-email` with `{"token":"x"}`, and `POST /payos/webhook` with `{}`, with the string in `REHI_VERIFY_TOKEN`:

  ```bash
  i=0; for t in 'eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0.b.c.d.e' 'eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..AAAA.AAAA.AAAA' \
      'a.b.c' 'eyJhbGciOiJIUzI1NiJ9.!!!.x' 'eyJhbGciOiJIUzI1NiJ9.eyJpc3MiOjF9.x' 'eyJ9.e30.' '.' '....'; do
    i=$((i+1))
    REHI_VERIFY_TOKEN="$t" scripts/verify.sh call bearer-$i-states /states
    REHI_VERIFY_TOKEN="$t" scripts/verify.sh call bearer-$i-verify -X POST -H 'Content-Type: application/json' -d '{"token":"x"}' /auth/verify-email
    REHI_VERIFY_TOKEN="$t" scripts/verify.sh call bearer-$i-payos -X POST -H 'Content-Type: application/json' -d '{}' /payos/webhook
  done
  ```

  Every `bearer-*-states.status` is `401`, every `bearer-*-verify.status` is `400` with `title` `Auth.InvalidEmailToken`, and every `bearer-*-payos.status` is `200`. `api.log` has no `An unhandled exception has occurred` line for these calls. The first string is a five-part JWE whose segments are not base64url, which used to answer `500` on every route.
- **No side effect.** Run `scripts/verify.sh sql 'select count(*) from "Tags"'`. It prints `0`.
- **Webhook reachability.** Run `scripts/verify.sh call payos-anon -X POST -H 'Content-Type: application/json' -d '{}' /payos/webhook`. Status `200` with body `{"isSuccess":true,"data":{"status":"failed"}}`. The handler rejected the empty payload, which proves the request got past the auth gate. Any status except `401` passes this step.
- **Rehi token.** Run `REHI_VERIFY_TOKEN="$TOKEN" scripts/verify.sh call gate-rehi-token /states`. Status `200`.
- **Tampered Rehi token.** Change the first character of the signature segment: `SIG="${TOKEN##*.}"; [[ "${SIG:0:1}" == A ]] && C=B || C=A; FORGED="${TOKEN%.*}.${C}${SIG:1}"`. Run `REHI_VERIFY_TOKEN="$FORGED" scripts/verify.sh call gate-forged-rehi /states`. Status `401`, and `gate-forged-rehi.headers` holds `WWW-Authenticate: Bearer error="invalid_token"`. `api.log` shows `Rehi was not authenticated` with `IDX10517: Signature validation failed`, which proves the issuer routed the token to the `Rehi` scheme and the signature check rejected it.
- **Proof.** The evidence holds the six `.status` files, the 24 `bearer-*` files, and the SQL count.

## Gotchas

- Swagger UI's Authorize button does not mint tokens. It only sends one you paste.
- When an auth scheme changes, rerun this whole journey. A second scheme added as the default can quietly make a protected route anonymous.
- Change the first signature character, not the last. The last base64url character of an HS256 signature carries 2 padding bits, so some edits there decode to the same signature.
- Never hand-write a valid JWT for this journey. The forged token is a real token from the running instance with one signature character changed. The unreadable bearers are not tokens at all, and they test only that the scheme selector never throws.
- No step here sends an Auth0 token, because this skill cannot mint one. The `Auth0` scheme still handles the garbage token: `api.log` shows `Auth0 was not authenticated` for `states-garbage`.
- The webhook call sends an empty payload on purpose. Never post a realistic payload, because a valid-looking one can change subscription rows.
