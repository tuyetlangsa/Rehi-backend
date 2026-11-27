# Extension session and Auth0 exchange

A signed-in web client turns its Rehi access token into a separate session for the browser extension, and the extension refreshes that session on its own. A web client that signed in through Auth0 trades the Auth0 access token for a Rehi session. Each endpoint accepts exactly one token kind: `extension-session` takes only a Rehi token, and `auth0-exchange` takes only an Auth0 token.

## Sub-features

- `ext-session` issues a token pair in a new refresh-token family with `Client = Extension` (`1`) and a 90-day refresh token (`POST /auth/extension-session`, no body, `RehiToken` policy).
- `ext-refresh` rotates the extension refresh token through the normal `POST /auth/refresh`, and the successor keeps `Client = Extension`.
- `auth0-exchange` finds the user by the Auth0 token's `email` claim, case-insensitively, or creates one, then issues a web pair (`POST /auth/auth0-exchange`, no body, `Auth0Token` policy). It never reads an email from the body. A token whose `email_verified` claim is present and not `true` answers `403` `Auth.EmailNotVerified`.
- `pin-rehi-only` and `pin-auth0-only`: a token of the other kind, or no token, answers `401`.

## How to get to it (user POV)

- Web: `POST /api/auth/extension-session` in `rehi-deploy` forwards the Rehi access token here when the extension posts `REHI_EXTENSION_READY`. The web then hands the refresh token to the extension.
- Web: the Auth0 `onCallback` in `rehi-deploy/src/lib/auth0.ts` calls `auth0-exchange` with the Auth0 access token and stores the returned refresh token in the `rehi_session` cookie.
- Extension: refreshes its own token through `POST /auth/refresh`.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- [Native auth](./native-auth.md) ran to the end in this instance, so `verify@rehi.test` is verified and its password is `correct-horse-1`. This journey adds `RefreshTokens` rows, so run it after native-auth, whose SQL counts assume none. Start it at least 61 s after native-auth ends. Native-auth spends the minute's login budget for `127.0.0.1`, and this journey logs in once.
- `S=.claude/skills/verify-rehi-backend/scripts/verify.sh`, `J='Content-Type: application/json'`, `E="$($S evidence)"`, and `REHI_VERIFY_TOKEN` is unset.
- `hash_of() { echo "translate(rtrim(encode(sha256(convert_to('$1','UTF8')),'base64'),'='),'+/','-_')"; }` gives the SQL expression for a refresh token's stored hash.

- **Web sign-in.** Run `$S call ext-web-login -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","client":"web"}' /auth/login`. Status `200`. Set `WEB="$(jq -r .data.accessToken "$E/ext-web-login.body")"` and `WEB_R="$(jq -r .data.refreshToken "$E/ext-web-login.body")"`.
- **Mint.** Run `REHI_VERIFY_TOKEN="$WEB" $S call ext-session -X POST /auth/extension-session`. Status `200`, and `data` is a `TokenPair`. `refreshTokenExpiresAt - accessTokenExpiresAt` is about 90 days less 15 minutes. Set `X_R` from its `refreshToken` and `X_A` from its `accessToken`.
- **Row is Extension.** Run `$S sql "select \"Client\", (\"ExpiresAt\" - \"CreatedAt\")::text, \"FamilyId\" <> (select \"FamilyId\" from \"RefreshTokens\" where \"TokenHash\" = $(hash_of "$WEB_R")) from \"RefreshTokens\" where \"TokenHash\" = $(hash_of "$X_R")"`. It prints `1|90 days|t`: Client is Extension, and the family differs from the web sign-in.
- **Extension token works.** Run `REHI_VERIFY_TOKEN="$X_A" $S call ext-states /states`. Status `200`.
- **Refresh.** Run `$S call ext-refresh -X POST -H "$J" -d "{\"refreshToken\":\"$X_R\"}" /auth/refresh`. Status `200` with a new `refreshToken`, set as `X_R2`. The SQL read of `"Client", "RevokedAt" is not null, "ReplacedById" is not null` for `X_R` prints `1|t|t`, and for `X_R2` prints `1|f|f`.
- **No token.** Run `$S call ext-session-anon -X POST /auth/extension-session`. Status `401`.
- **Rehi token on the exchange.** Count the rows with `$S sql 'select (select count(*) from "Users"), (select count(*) from "RefreshTokens")'`, then run `REHI_VERIFY_TOKEN="$WEB" $S call exchange-rehi-token -X POST /auth/auth0-exchange`. Status `401`, and `exchange-rehi-token.headers` holds `WWW-Authenticate: Bearer`. `api.log` shows `Auth0 was not authenticated` with `IDX10500` for this request, which proves the policy sent the Rehi token to the `Auth0` scheme. The same count prints the same numbers.
- **No token on the exchange.** Run `$S call exchange-anon -X POST /auth/auth0-exchange`. Status `401`.
- **Auth0 token on the exchange.** BLOCKED. The verify instance cannot mint an Auth0 token. The dev checks it in W1's live box: sign in through Auth0 and see one `RefreshTokens` row with `Client = 0` for that user, and no second `Users` row for an email that differs only in case.
- **Proof.** The evidence holds every `.status` and `.body` above, and the SQL reads saved as `<name>.sql.txt`.

## Gotchas

- Both endpoints take no body. The web posts with no `Content-Type`, so do not add `-d` to these calls.
- The policy re-authenticates with its one scheme and ignores the issuer selector. A Rehi token on `auth0-exchange` reaches the Auth0 scheme, which rejects it. Under the default `RequireAuthorization()` the same token would pass, so this `401` is the proof that the pin works.
- `auth0-exchange` trusts the `email` claim when the token has no `email_verified` claim. Whether the Auth0 Action adds `email_verified` is unproven, so an unverified Auth0 database user could claim a native account with the same email.
- `extension-session` accepts any Rehi access token, an extension one included, and each call starts a new family. Logout of the web family leaves the extension family active.
