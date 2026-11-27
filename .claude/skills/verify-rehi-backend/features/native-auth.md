# Native auth

A new user registers with an email and a password, confirms the email through a link, and signs in. Sign-in returns a Rehi access token and a rotating refresh token. Every existing endpoint accepts the Rehi access token, next to the Auth0 tokens that the web app already uses. This journey is also the token source for every other authenticated journey in this map.

## Sub-features

- `auth-register` creates a user with a password hash and sends a verification email (`POST /auth/register`).
- `auth-verify-email` consumes the emailed token and sets `Users."EmailVerifiedAt"` (`POST /auth/verify-email`).
- `auth-resend-verification` sends a new link and voids the older one. It answers `200` for any email (`POST /auth/resend-verification`).
- `auth-login` returns a token pair for a verified user (`POST /auth/login`, `client` is `"web"` or `"extension"`).
- `auth-refresh` rotates the refresh token. A token reused within 30 s of its rotation gets a fresh pair in the same family. A reuse after 30 s revokes the whole family (`POST /auth/refresh`).
- `auth-logout` revokes the family of the given refresh token (`POST /auth/logout`).
- `auth-rate-limit` allows 10 logins, 5 registers, and 5 resends per minute per client IP, then answers `429`.

## How to get to it (user POV)

- Web app: the register, verify-email, and login pages, and the Next.js route handlers under `/api/auth/*` that call these endpoints.
- Extension: login with `client: "extension"` and its own refresh loop.
- The verification email links to `{Rehi:WebBaseUrl}/verify-email?token=<token>`.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- `REHI_VERIFY_TOKEN` is unset.
- `S=.claude/skills/verify-rehi-backend/scripts/verify.sh`, `J='Content-Type: application/json'`, and `E="$($S evidence)"`.

- **Register.** Run `$S call register -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","fullName":"Verify User"}' /auth/register`. Status `200`, body `{"isSuccess":true,"data":null}`. `$S sql 'select "Email", "PasswordHash" is not null, "EmailVerifiedAt" from "Users"'` prints `verify@rehi.test|t|`.
- **Unverified login.** Run `$S call login-unverified -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","client":"web"}' /auth/login`. Status `403`, `title` `Auth.EmailNotVerified`.
- **Read the link.** Run `for i in $(seq 1 20); do TOKEN="$($S mail-token verify@rehi.test verify 2>/dev/null)" && break; sleep 1; done`. `TOKEN` holds 43 base64url characters.
- **Verify.** Run `$S call verify-email -X POST -H "$J" -d "{\"token\":\"$TOKEN\"}" /auth/verify-email`. Status `200`. `$S sql 'select "EmailVerifiedAt" is not null from "Users"'` prints `t`. Run the same call again as `verify-email-again`. Status `400`, `title` `Auth.InvalidEmailToken`.
- **Login.** Run `$S call login -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","client":"web"}' /auth/login`. Status `200`. `data` holds `accessToken`, `accessTokenExpiresAt`, `refreshToken`, and `refreshTokenExpiresAt`, with both times in epoch milliseconds.
- **Export the token.** Run `export REHI_VERIFY_TOKEN="$(jq -r .data.accessToken "$E/login.body")"` and `R1="$(jq -r .data.refreshToken "$E/login.body")"`. `jq -rR 'split(".")[1] | gsub("-";"+") | gsub("_";"/") | @base64d' <<<"$REHI_VERIFY_TOKEN"` shows `iss`, `aud`, `sub`, plain `email` and `name`, and `exp`.
- **Use the token.** Run `$S call states-rehi /states`. Status `200`.
- **Refresh rotates.** Run `$S call refresh -X POST -H "$J" -d "{\"refreshToken\":\"$R1\"}" /auth/refresh`. Status `200` with a new `refreshToken`. Set `R2` from `refresh.body`. `$S sql 'select "RevokedAt" is not null, "ReplacedById" is not null from "RefreshTokens" order by "CreatedAt"'` prints `t|t`, then `f|f`.
- **Parallel refresh.** Run `for n in a b; do $S call refresh-par-$n -X POST -H "$J" -d "{\"refreshToken\":\"$R2\"}" /auth/refresh & done; wait`. Both statuses are `200`. The family now has two active rows.
- **Reuse after the grace.** Run `sleep 31`, then refresh with `R1` as `refresh-reuse`. Status `401`, `title` `Auth.InvalidRefreshToken`. Refresh with the token from `refresh-par-a.body` as `refresh-after-reuse`. Status `401`. `$S sql 'select count(*) filter (where "RevokedAt" is null), count(*) from "RefreshTokens"'` prints `0|4`.
- **Logout.** Log in again with `"client":"extension"` as `login-extension`, and set `R3` from its body. Run `$S call logout -X POST -H "$J" -d "{\"refreshToken\":\"$R3\"}" /auth/logout`. Status `200`. Refresh with `R3` as `refresh-after-logout`. Status `401`. `$S sql 'select "Client", "RevokedAt" is not null from "RefreshTokens" order by "CreatedAt" desc limit 1'` prints `1|t`.
- **Wrong password.** Log in with `"password":"wrong-password"` as `login-wrong-password`. Status `401`, `title` `Auth.InvalidCredentials`.
- **Duplicate register.** Register `"email":"VERIFY@rehi.test"` as `register-duplicate`. Status `409`, `title` `User.EmailAlreadyExists`. The API lowercases the email first.
- **Short password.** Register `short@rehi.test` with `"password":"short"` as `register-short-password`. Status `400`, `title` `General.Validation`. `$S sql "select count(*) from \"Users\" where \"Email\" = 'short@rehi.test'"` prints `0`.
- **Rate limit.** Log in 11 times with `-H 'X-Forwarded-For: 203.0.113.7'` and a wrong password, as `rate-1` to `rate-11`. Calls 1 to 10 answer `401`. Call 11 answers `429`. The same call with `X-Forwarded-For: 203.0.113.8` answers `401`, and with `X-Forwarded-For: 198.51.100.1, 203.0.113.7` it answers `429`.
- **Proof.** The evidence holds every `.status` and `.body` above, the SQL reads saved as `<name>.sql.txt`, and `mail-verify-verify@rehi.test.json`.

## Gotchas

- The outbox job sends the verification email, and it runs every 5 s. `mail-token` fails right after register, so poll it.
- Overlapping refreshes run in serializable transactions. When they collide, `api.log` shows `ERR Failed executing DbCommand` with `40001`, then `A transient exception occurred during execution. The operation will be retried after 0ms.`. That retry is the design, not a failure.
- Access tokens are stateless. Logout and family revocation stop refresh, but an issued access token stays valid until its `exp`.
- The rate limiter keys on the client IP after `UseForwardedHeaders`, which takes the rightmost `X-Forwarded-For` entry from any peer. Send a fresh `X-Forwarded-For` value to get a fresh bucket. Failed logins count toward the limit too.
- `up` passes a random `Jwt__Secret` on every run, so a token from an earlier run answers `401`.
- `/auth/*` endpoints ignore `REHI_VERIFY_TOKEN`, so it is safe to leave it exported for the rest of the journey.
