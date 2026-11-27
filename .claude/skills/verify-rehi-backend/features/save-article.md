# Save an article

A signed-in user saves a page by URL. The first save stores it, a second save of the same URL returns the stored article, and the client sees it through state sync. This is the core journey of the web app's library and the extension's save button.

## Sub-features

- `article-onboard` creates the user's row. A native user gets it from register (`POST /auth/register`). An Auth0 user gets it from `POST /users` after the first Auth0 sign-in.
- `article-save` stores a new article (`POST /articles/get-or-add`).
- `article-get-existing` returns the stored article for a known URL.
- `article-sync` lists it in `GET /states` and `GET /states/{lastUpdateTime}`.
- `article-delete-recover` soft-deletes it and recovers it.

## How to get to it (user POV)

- Web app: paste a URL into the library, or open a saved article.
- Extension: press save on the current page.
- A native user registers once, as in [Native auth](./native-auth.md). The web app still calls `POST /users` after an Auth0 sign-in. This map cannot drive that path, because it cannot mint an Auth0 token.

## Driving it with verify.sh

Preconditions:

- Baseline instance is up and `doctor` is all `OK`.
- [Native auth](./native-auth.md) ran through **Export the token**, so `REHI_VERIFY_TOKEN` holds an access token whose email claim is `verify@rehi.test`.

- **Onboard.** The native-auth **Register** step created the user. `scripts/verify.sh sql 'select "Email" from "Users" where "EmailVerifiedAt" is not null'` prints `verify@rehi.test`.
- **Save.** Run `scripts/verify.sh call article-save -X POST -H 'Content-Type: application/json' -d '{"id":"11111111-1111-1111-1111-111111111111","url":"https://example.com/verify","rawHtml":"<html><body><h1>Verify</h1><p>Body text</p></body></html>","title":"Verify","createAt":1795000000000}' /articles/get-or-add`. Status `201` for a new article. The body contains the article id and URL.
- **Same URL again.** Repeat the save with a different `id`. Status `200`, and the body returns the original id. `scripts/verify.sh sql 'select count(*) from "Articles"'` prints `1`.
- **Sync.** Run `scripts/verify.sh call states-all /states`. Status `200`. The body lists the article.
- **Proof.** The evidence holds the three call files plus the SQL reads.

## Gotchas

- A token whose email has no `Users` row fails with `User.NotFound`, and its message reads "Article not found". Register first.
- Saving an article raises `ArticleCreatedDomainEvent`, which the outbox job processes every 5 seconds. If its handler needs Azure AI Search or Azure OpenAI, both empty locally, `api.log` shows outbox errors after the save. Report that side effect as unverifiable locally. It is not a failure of the save. Observed: `ArticleCreatedDomainEvent` succeeds, and `ArticleParsedDomainEvent` fails with `System.UriFormatException: Invalid URI: The URI is empty.` because `AzureSearch:ServiceEndpoint` is empty.
- Delete and recover take `updateAt` in the route as epoch milliseconds. Use a value later than `createAt`.
