# Native auth

A new user registers with an email and a password, confirms the email through a link, and signs in. Sign-in returns a Rehi access token and a rotating refresh token. Every existing endpoint accepts the Rehi access token, next to the Auth0 tokens that the web app already uses. This journey is also the token source for every other authenticated journey in this map.

## Sub-features

- `auth-register` creates a user with a password hash and sends a verification email (`POST /auth/register`). The email must be a plain address. A register for an unverified row that has a password replaces that password and name, voids the row's earlier links, and sends a new one. A verified row, or a row with no password (an Auth0 user), answers `409`.
- `auth-verify-email` consumes the emailed token once and sets `Users."EmailVerifiedAt"` (`POST /auth/verify-email`).
- `auth-resend-verification` sends a new link and voids the older one. It answers `200` for any email (`POST /auth/resend-verification`).
- `auth-login` returns a token pair for a verified user (`POST /auth/login`, `client` is `"web"` or `"extension"`). It checks the password before it reports an unverified email. The access token lives 15 minutes.
- `auth-refresh` rotates the refresh token. A token reused within 30 s of its rotation gets a fresh pair in the same family. A reuse after 30 s revokes the whole family (`POST /auth/refresh`).
- `auth-logout` revokes the family of the given refresh token (`POST /auth/logout`).
- `auth-rate-limit` allows 10 logins, 5 registers, and 5 resends per minute per client IP, then answers `429`. The client IP is the TCP peer. `X-Forwarded-For` counts only when the deployment sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.

## How to get to it (user POV)

- Web app: the register, verify-email, and login pages, and the Next.js route handlers under `/api/auth/*` that call these endpoints.
- Extension: login with `client: "extension"` and its own refresh loop.
- The verification email links to `{Rehi:WebBaseUrl}/verify-email?token=<token>`.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- `REHI_VERIFY_TOKEN` is unset.
- `S=.claude/skills/verify-rehi-backend/scripts/verify.sh`, `J='Content-Type: application/json'`, and `E="$($S evidence)"`.
- `poll() { for i in $(seq 1 20); do T="$($S mail-token "$1" verify 2>/dev/null)" && [[ "$T" != "${2:-}" ]] && echo "$T" && return; sleep 1; done; return 1; }` prints the newest verify token sent to `$1` that differs from `$2`.

- **Register.** Run `$S call register -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","fullName":"Verify User"}' /auth/register`. Status `200`, body `{"isSuccess":true,"data":null}`. `$S sql 'select "Email", "PasswordHash" is not null, "EmailVerifiedAt" from "Users"'` prints `verify@rehi.test|t|`.
- **Unverified login.** Run `$S call login-unverified -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","client":"web"}' /auth/login`. Status `403`, `title` `Auth.EmailNotVerified`.
- **Read the link.** Run `TOKEN="$(poll verify@rehi.test)"`. `TOKEN` holds 43 base64url characters.
- **Verify once, in parallel.** Run `for n in 1 2 3 4 5; do $S call verify-par-$n -X POST -H "$J" -d "{\"token\":\"$TOKEN\"}" /auth/verify-email & done; wait`. `cat "$E"/verify-par-*.status | sort | uniq -c` prints `1 200` and `4 400`, and each `400` body has `title` `Auth.InvalidEmailToken`. `$S sql 'select "EmailVerifiedAt" is not null from "Users"'` prints `t`. Run the same call again as `verify-email-again`. Status `400`, `title` `Auth.InvalidEmailToken`.
- **Login.** Run `$S call login -X POST -H "$J" -d '{"email":"verify@rehi.test","password":"correct-horse-1","client":"web"}' /auth/login`. Status `200`. `data` holds `accessToken`, `accessTokenExpiresAt`, `refreshToken`, and `refreshTokenExpiresAt`, with both times in epoch milliseconds.
- **Export the token.** Run `export REHI_VERIFY_TOKEN="$(jq -r .data.accessToken "$E/login.body")"` and `R1="$(jq -r .data.refreshToken "$E/login.body")"`. `jq -rR 'split(".")[1] | gsub("-";"+") | gsub("_";"/") | @base64d' <<<"$REHI_VERIFY_TOKEN"` shows `iss`, `aud`, `sub`, plain `email` and `name`, and `exp`. Add `| fromjson | .exp - .iat` to the same filter, and it prints `900`.
- **Use the token.** Run `$S call states-rehi /states`. Status `200`.
- **Refresh rotates.** Run `$S call refresh -X POST -H "$J" -d "{\"refreshToken\":\"$R1\"}" /auth/refresh`. Status `200` with a new `refreshToken`. Set `R2` from `refresh.body`. `$S sql 'select "RevokedAt" is not null, "ReplacedById" is not null from "RefreshTokens" order by "CreatedAt"'` prints `t|t`, then `f|f`.
- **Parallel refresh.** Run `for n in a b; do $S call refresh-par-$n -X POST -H "$J" -d "{\"refreshToken\":\"$R2\"}" /auth/refresh & done; wait`. Both statuses are `200`. The family now has two active rows.
- **Reuse after the grace.** Run `sleep 31`, then refresh with `R1` as `refresh-reuse`. Status `401`, `title` `Auth.InvalidRefreshToken`. Refresh with the token from `refresh-par-a.body` as `refresh-after-reuse`. Status `401`. `$S sql 'select count(*) filter (where "RevokedAt" is null), count(*) from "RefreshTokens"'` prints `0|4`.
- **Logout.** Log in again with `"client":"extension"` as `login-extension`, and set `R3` from its body. Run `$S call logout -X POST -H "$J" -d "{\"refreshToken\":\"$R3\"}" /auth/logout`. Status `200`. Refresh with `R3` as `refresh-after-logout`. Status `401`. `$S sql 'select "Client", "RevokedAt" is not null from "RefreshTokens" order by "CreatedAt" desc limit 1'` prints `1|t`.
- **Wrong password.** Log in with `"password":"wrong-password"` as `login-wrong-password`. Status `401`, `title` `Auth.InvalidCredentials`.
- **Duplicate register.** Register `"email":"VERIFY@rehi.test"` as `register-duplicate`. Status `409`, `title` `User.EmailAlreadyExists`. The API lowercases the email first. The row is verified, so the register leaves it alone: `login` with `correct-horse-1` still works later.
- **Short password.** Register `short@rehi.test` with `"password":"short"` as `register-short-password`. Status `400`, `title` `General.Validation`. `$S sql "select count(*) from \"Users\" where \"Email\" = 'short@rehi.test'"` prints `0`.
- **Plain address only.** Register `"email":"bad space@rehi.test"` as `register-email-space` and `"email":"\"alias\" <verify@rehi.test>"` as `register-email-alias`, both with `correct-horse-1`. Each answers `400`, `title` `General.Validation`. `$S sql 'select count(*) from "Users"'` prints `1`, and Mailpit holds one email to `verify@rehi.test`.
- **Fresh register budget.** Run `sleep 61`. The steps above spent the minute's 5 registers, and the next group needs 5 more.
- **Squatter registers the victim's address.** Register `{"email":"victim@rehi.test","password":"squatter-pass-1","fullName":"Squatter"}` as `squat-register`. Status `200`. Log in as `victim@rehi.test` with `squatter-pass-1` as `squat-login-unverified`. Status `403`, `title` `Auth.EmailNotVerified`. Log in with `wrong-password` as `squat-login-wrong`. Status `401`, `title` `Auth.InvalidCredentials`, so the password is checked first. Run `SQUAT="$(poll victim@rehi.test)"`.
- **Victim registers their own address.** Register `{"email":"victim@rehi.test","password":"victim-pass-1","fullName":"Victim"}` as `victim-register`. Status `200`, body `{"isSuccess":true,"data":null}`, the same as a new registration. `$S sql "select \"FullName\", \"EmailVerifiedAt\" is null from \"Users\" where \"Email\" = 'victim@rehi.test'"` prints `Victim|t`. Verify with `SQUAT` as `squat-link`. Status `400`, `title` `Auth.InvalidEmailToken`.
- **Victim verifies.** Run `VICTIM="$(poll victim@rehi.test "$SQUAT")"`, then verify with it as `victim-verify`. Status `200`. Log in with `victim-pass-1` as `victim-login`. Status `200`. Log in with `squatter-pass-1` as `squat-login-after`. Status `401`, `title` `Auth.InvalidCredentials`.
- **Verified rows stay closed.** Register `victim@rehi.test` with `squatter-pass-1` again as `squat-register-again`. Status `409`, `title` `User.EmailAlreadyExists`.
- **Auth0 rows stay closed.** Seed a row with no password, like a row an Auth0 sign-in creates, with `$S sql "insert into \"Users\" (\"Id\", \"Email\", \"FullName\", \"CreateAt\", \"ScheduleTime\") values (gen_random_uuid(), 'auth0-only@rehi.test', 'Auth0 Only', now(), now())"`. No API path creates that row without an Auth0 token (see [Password reset](./password-reset.md), Gotchas). Register `auth0-only@rehi.test` with `correct-horse-1` as `register-auth0-only`. Status `409`, `title` `User.EmailAlreadyExists`. `$S sql "select \"PasswordHash\" is null from \"Users\" where \"Email\" = 'auth0-only@rehi.test'"` prints `t`.
- **Angle brackets.** Register `"email":"<angle@rehi.test>"` as `register-email-angle`. Status `400`, `title` `General.Validation`.
- **Concurrent registers.** Run `sleep 61`, then `for n in 1 2 3 4 5; do $S call race-$n -X POST -H "$J" -d '{"email":"race@rehi.test","password":"race-pass-1","fullName":"Race"}' /auth/register & done; wait`. `cat "$E"/race-*.status | sort | uniq -c` prints `1 200` and `4 409`, and each `409` body has `title` `User.EmailAlreadyExists`. `$S sql "select count(*) from \"Users\" where \"Email\" = 'race@rehi.test'"` prints `1`.
- **Concurrent re-registers.** Run `sleep 61`, then the same loop with `"password":"race-pass-2"` as `rerace-1` to `rerace-5`. All five statuses are `200`, and none is `500`. Wait until Mailpit holds 6 emails to `race@rehi.test` (`curl -s "http://127.0.0.1:${REHI_VERIFY_MAIL_PORT:-58025}/api/v1/search?query=to:%22race@rehi.test%22" | jq .messages_count`). Then `$S sql "select count(*) from \"EmailTokens\" t join \"Users\" u on u.\"Id\" = t.\"UserId\" where u.\"Email\" = 'race@rehi.test' and t.\"ConsumedAt\" is null"` prints `1`, so only the newest link works.
- **Rate limit ignores X-Forwarded-For.** In the same minute, log in 11 times as `nobody@rehi.test` with a wrong password and a new `-H "X-Forwarded-For: 198.18.$n.1"` on each, as `rate-1` to `rate-11`. Calls 1 to 10 answer `401`. Call 11 answers `429`.
- **Resend cap.** Still in the same minute, call `/auth/resend-verification` with `{"email":"race@rehi.test"}` 6 times, each with a new `X-Forwarded-For`, as `resend-1` to `resend-6`. Calls 1 to 5 answer `200`. Call 6 answers `429`. Ten seconds later Mailpit holds 11 emails to `race@rehi.test`, 5 more than before.
- **Proof.** The evidence holds every `.status` and `.body` above, the SQL reads saved as `<name>.sql.txt`, and `mail-verify-verify@rehi.test.json`.

## Gotchas

- The outbox job sends the verification email, and it runs every 5 s. `mail-token` fails right after register, so poll it.
- Overlapping refreshes, verify-email calls, and re-registers of an unverified row run in serializable transactions. When they collide, `api.log` shows `ERR Failed executing DbCommand` with `40001`, then `A transient exception occurred during execution. The operation will be retried after 0ms.`. That retry is the design, not a failure.
- Concurrent registers of a new email race on `IX_Users_Email`. `api.log` shows `ERR Failed executing DbCommand` with `23505` for each loser, and the loser answers `409`.
- Access tokens are stateless. Logout and family revocation stop refresh, but an issued access token stays valid until its `exp`.
- The rate limiter keys on the TCP peer address. Every verify call comes from `127.0.0.1`, so the whole journey shares one budget per endpoint, and `X-Forwarded-For` does not reset it. Wait 61 s before a step that needs a fresh minute. Failed logins and rejected registers count toward the limit too. To check the proxy setting, start the instance with `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true $S up`, and each new `X-Forwarded-For` then gets its own budget.
- A squatter who registers again after the victim, and before the victim opens the link, replaces the password again and voids the victim's link. The newest email in the victim's inbox then carries the squatter's password. Login with the victim's password then answers `401`, and the victim recovers through forgot-password.
- `up` passes a random `Jwt__Secret` on every run, so a token from an earlier run answers `401`.
- `/auth/*` endpoints ignore `REHI_VERIFY_TOKEN`, so it is safe to leave it exported for the rest of the journey.
