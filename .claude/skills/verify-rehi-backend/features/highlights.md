# Highlights

A signed-in user highlights text in a saved article from the web reader or the extension, adds a note to it, and deletes it.

## Sub-features

- `highlight-create` creates a highlight (`POST /highlights`).
- `highlight-note` adds a note (`POST /highlights/note`).
- `highlight-delete` deletes it (`DELETE /highlights/{highlightId}/{updateAt}`).

## How to get to it (user POV)

- Web app: select text in the reader, then pick a color.
- Extension: select text on the live page, then highlight. That call sends `createBy: "rehi-browser-extension"`.

## Driving it with verify.sh

Preconditions:

- [Save an article](./save-article.md) ran through **Sync** in this instance, with the same token.
- `REHI_VERIFY_TOKEN` holds the [Native auth](./native-auth.md) access token, as for that journey.

- **Create from web.** Run `scripts/verify.sh call hl-create -X POST -H 'Content-Type: application/json' -d '{"id":"22222222-2222-2222-2222-222222222222","location":"p:0","html":"<mark>Body</mark>","markdown":"Body","plainText":"Body","articleId":"11111111-1111-1111-1111-111111111111","color":"yellow","createAt":1795000003000,"createBy":"rehi-web"}' /highlights`. Status `201`, and `data` is the highlight id. `scripts/verify.sh sql 'select "PlainText" from "Highlights"'` prints `Body`.
- **Create from extension.** Repeat with a new id and `"createBy":"rehi-browser-extension"`. Status `201`.
- **Note.** Read the request shape from `Rehi.Apis/Highlights/CreateHighlightNote.cs`, then add a note to `22222222-…`. Status `200`, and the SQL read shows the note.
- **Delete.** Run `scripts/verify.sh call hl-delete -X DELETE /highlights/22222222-2222-2222-2222-222222222222/1795000004000`. Status `200`. `GET /states` returns it with `"isDeleted":true`.
- **Proof.** Save each call plus each SQL read.

## Gotchas

- `location` is an opaque client string. The web app derives it from the DOM path. Any stable string works for API proof, but a reader-rendering change needs the web verification, not this map.
