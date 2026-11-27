# Password reset

A user who forgot their password asks for a reset link by email, then sets a new password through that link. An Auth0-only user, whose row has no password, uses the same flow to get one, because the link proves they own the email. A reset signs the user out everywhere: every refresh token they held stops working.

## Sub-features

- `auth-forgot-password` sends a one-hour `/reset-password` link through the outbox and voids older reset links. It answers `200` for any well-formed email, known or not, and sends nothing for an unknown one (`POST /auth/forgot-password`, body `{ email }`). The email matches case-insensitively, so a mixed-case Auth0 row is found.
- `auth-reset-password` consumes the link, sets the password hash, sets `Users."EmailVerifiedAt"` if it was empty, and revokes every active refresh token of the user (`POST /auth/reset-password`, body `{ token, newPassword }`). An unknown, expired, or consumed token answers `400 Auth.InvalidEmailToken`. A password outside 8 to 128 characters answers `400 General.Validation`.
- `auth-reset-rate-limit` allows 5 forgot-password and 5 reset-password calls per minute per client IP, then answers `429`.

## How to get to it (user POV)

- Web app: a "Forgot password?" link on the login page posts the email, and the `reset-password` page reads `?token=` from the emailed link and posts the new password.
- The email links to `{Rehi:WebBaseUrl}/reset-password?token=<token>`.
- An Auth0 user who wants a password uses the same "Forgot password?" link.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`. Running it after the other journeys is fine, because it uses its own users. Start it at least 61 s after another journey's last `/auth/*` call. Every verify call comes from `127.0.0.1`, so all journeys share one rate-limit budget per endpoint.
- `S=.claude/skills/verify-rehi-backend/scripts/verify.sh`, `J='Content-Type: application/json'`, `E="$($S evidence)"`, and `M="http://127.0.0.1:${REHI_VERIFY_MAIL_PORT:-58025}"`.
- `poll() { for i in $(seq 1 20); do $S mail-token "$1" "$2" 2>/dev/null && return; sleep 1; done; return 1; }`.

- **Set up a signed-in user.** Register `reset@rehi.test` with `"password":"old-password-1"` as `pr-register`, run `VT="$(poll reset@rehi.test verify)"`, and verify it as `pr-verify`. Log in with `"client":"web"` as `pr-login-web` and with `"client":"extension"` as `pr-login-ext`. All four answer `200`. Set `RW`, `RX`, and `AT` from the two bodies' `refreshToken` and the web `accessToken`. With `uid="select \"Id\" from \"Users\" where \"Email\" = 'reset@rehi.test'"`, `$S sql "select count(*) filter (where \"RevokedAt\" is null), count(distinct \"FamilyId\") from \"RefreshTokens\" where \"UserId\" = ($uid)"` prints `2|2`.
- **Forgot.** Run `$S call pr-forgot -X POST -H "$J" -d '{"email":" Reset@Rehi.test "}' /auth/forgot-password`. Status `200`. Run `RT="$(poll reset@rehi.test reset)"`. `RT` holds 43 base64url characters and the mail subject is `Reset your Rehi password`. The `EmailTokens` row for the user with `"Purpose" = 1` shows `ConsumedAt` empty and `"ExpiresAt" - "CreatedAt"` `01:00:00`.
- **Short password.** Reset with `{"token":"$RT","newPassword":"short"}` as `pr-reset-short`. Status `400`, `title` `General.Validation`. The token is not consumed.
- **Reset.** Run `$S call pr-reset -X POST -H "$J" -d "{\"token\":\"$RT\",\"newPassword\":\"new-password-2\"}" /auth/reset-password`. Status `200`. `$S sql "select count(*) filter (where \"RevokedAt\" is null), count(*) filter (where \"RevokedAt\" is not null and \"ReplacedById\" is null) from \"RefreshTokens\" where \"UserId\" = ($uid)"` prints `0|2`.
- **Old password, new password.** Log in with `old-password-1` as `pr-login-old`. Status `401`, `title` `Auth.InvalidCredentials`. Log in with `new-password-2` as `pr-login-new`. Status `200`.
- **Sessions from before the reset.** Refresh with `RW` as `pr-refresh-old-web` and with `RX` as `pr-refresh-old-ext`. Both answer `401`, `title` `Auth.InvalidRefreshToken`.
- **Reused link.** Reset again with `RT` and `"newPassword":"third-password-3"` as `pr-reset-reused`. Status `400`, `title` `Auth.InvalidEmailToken`.
- **Unknown email.** Read `curl -s "$M/api/v1/messages?limit=1" | jq .total` and `$S sql 'select count(*) from outbox_messages'`. Run forgot-password for `nobody@rehi.test` as `pr-forgot-unknown`. Status `200`. After `sleep 7`, both counts are unchanged and a Mailpit search for `to:"nobody@rehi.test"` returns 0 messages.
- **Auth0-style user.** `POST /users` is the only API path that creates a row with no password. It needs any valid token and stores the body email as sent. Run `REHI_VERIFY_TOKEN="$AT" $S call pr-auth0-seed -X POST -H "$J" -d '{"email":"Auth0.Mixed@Rehi.Test","fullName":"Auth0 Style"}' /users`. Status `200`. `$S sql "select \"Email\", \"PasswordHash\" is null, \"EmailVerifiedAt\" is null from \"Users\" where lower(\"Email\") = 'auth0.mixed@rehi.test'"` prints `Auth0.Mixed@Rehi.Test|t|t`. Log in as `auth0.mixed@rehi.test` as `pr-auth0-login-before`. Status `401`, `title` `Auth.InvalidCredentials`. Run forgot-password for `AUTH0.mixed@rehi.test` as `pr-auth0-forgot`, then `AT0="$(poll Auth0.Mixed@Rehi.Test reset)"`. The mail goes to the stored address `Auth0.Mixed@Rehi.Test`. Reset with `AT0` and `"newPassword":"auth0-password-1"` as `pr-auth0-reset`. Status `200`. The same SQL now prints `Auth0.Mixed@Rehi.Test|f|f`. Log in as `auth0.mixed@rehi.test` with the new password as `pr-auth0-login-after`. Status `200`, and its access token answers `200` on `/states`.
- **Rate limit ignores X-Forwarded-For.** Run `sleep 61`, because the steps above spent 3 forgot-password and 4 reset-password calls. Read `curl -s "$M/api/v1/search?query=to:%22reset@rehi.test%22%20subject:%22Reset%22" | jq .messages_count`. Call forgot-password for `reset@rehi.test` 6 times, each with a new `-H "X-Forwarded-For: 198.20.$n.1"`, as `pr-forgot-rate-1` to `-6`. Calls 1 to 5 answer `200`, call 6 answers `429`. Ten seconds later the same search counts 5 more reset emails. Call reset-password 6 times with `{"token":"not-a-real-token","newPassword":"whatever-123"}`, each with a new `X-Forwarded-For`, as `pr-reset-rate-1` to `-6`. Calls 1 to 5 answer `400`, call 6 answers `429`.
- **Proof.** The evidence holds every `.status` and `.body` above, the SQL reads saved as `<name>.sql.txt`, `pr-forgot-unknown.mailpit.txt`, `mail-reset-reset@rehi.test.json`, and `mail-reset-Auth0.Mixed@Rehi.Test.json`.

## Gotchas

- The outbox job sends the reset email, and it runs every 5 s. Poll `mail-token` after forgot-password. Right after forgot-password, the newest email to `reset@rehi.test` is still the verification email, so `mail-token ... reset` fails until the reset email lands.
- A reset revokes refresh tokens only. An access token issued before the reset stays valid until its `exp`, as with logout.
- A second forgot-password voids the earlier link, so only the newest email works.
- A real Auth0 sign-in cannot run here. The Auth0-style row comes from `POST /users` with a Rehi token, which stores the same shape of row the web app's Auth0 onboarding stores.
