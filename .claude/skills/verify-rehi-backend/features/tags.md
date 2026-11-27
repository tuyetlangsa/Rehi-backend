# Tags

A signed-in user creates tags, assigns one to a saved article, and deletes a tag. State sync shows the tag and the assignment.

## Sub-features

- `tag-create` creates a tag (`POST /tags`).
- `tag-assign` assigns a tag to an article (`POST /articles/assign-tag`).
- `tag-delete` deletes a tag (`DELETE /tags/{name}/{updateAt}`).

## How to get to it (user POV)

- Web app: the tag picker on an article in the library.

## Driving it with verify.sh

Preconditions:

- [Save an article](./save-article.md) ran through **Sync** in this instance, with the same token.
- **BLOCKED until a token source exists**, as for that journey.

- **Create.** Run `scripts/verify.sh call tag-create -X POST -H 'Content-Type: application/json' -d '{"name":"verify-tag","createAt":1795000001000}' /tags`. Status `200`. `scripts/verify.sh sql 'select "Name" from "Tags"'` prints `verify-tag`.
- **Assign.** Read the request shape from `Rehi.Apis/Articles/AssignArticleTag.cs`, then call `/articles/assign-tag` with the saved article id and `verify-tag`. Status `200`. `scripts/verify.sh sql 'select count(*) from "ArticleTags"'` prints `1`.
- **Delete.** Run `scripts/verify.sh call tag-delete -X DELETE /tags/verify-tag/1795000002000`. Status `200`. `scripts/verify.sh sql 'select "Name","IsDeleted" from "Tags"'` prints `verify-tag|t`.
- **Proof.** Save each call plus each SQL read.

## Gotchas

- Tag names go in the delete route. URL-encode any name with spaces or slashes.
- Deletes are soft: the row stays, with `"IsDeleted" = true`. Check that column, not the row count.
