# Telegram manual library

Status: ED-24G.0 implements the first Telegram manual-library foundation. It supports role-gated manual requests after
diagnostics, Telegram `file_id` delivery when a binding exists, and Admin/Owner registration of document bindings. It
does not add a Mini App, public manual search, OCR, database migration, external storage provider, or committed manual
binaries.

## Intended model

Telegram `file_id` is the delivery handle. Telegram must not be the only source of truth: repository metadata remains in
`data/equipment-diagnostics/manual-library/manuals.json`, while runtime file bindings live in a server-local JSON file.

Runtime file binding:

- Default path: `artifacts/operations/equipment-diagnostics-manual-bindings.json`.
- Sample/template: `data/equipment-diagnostics/manual-library/manual-file-bindings.sample.json`.
- Real `telegramFileId` values must not be committed.
- If the binding file is missing or a manual has no binding, `/manuals` still works and says in Russian that the
  manual is known but the file is not connected yet.

User-facing messages and normal logs must never expose raw local paths, package IDs, JSON paths, Telegram chat IDs, user
IDs, file IDs, tokens, or secrets.

## Access policy

| Role | Manual delivery |
|---|---|
| Consumer / Client | Denied |
| Installer / Монтажник | Allowed |
| Engineer / Сервис-инженер | Allowed |
| Admin | Allowed |
| Owner | Allowed |

An allowed role is not enough by itself. The manual record must also be reviewed and marked `eligibleForTelegramLibrary`.

## Manual request UX

Supported request paths:

- Technical reply keyboard button: `📘 Руководства`.
- Command: `/manuals`.

The request uses the last successful diagnostic from Telegram history. It resolves reviewed `manualId` values from the
selected diagnostic answer and enforces the current user role. If one answer has multiple `sourceReferences[]`, manual
delivery uses all relevant references rather than asking the user to choose a source. It must not generalize one manual
across Gree series.

For entries without `sourceReferences[].manualId`, the fallback match is intentionally narrow: exact `sourceName` to
registry `documentTitle` or exact source name to registry file name without extension. No loose/fuzzy manual matching is
allowed.

## Registration flow

Only Admin and Owner can register a manual file binding.

Supported flows:

- Send a Telegram document to the bot with caption `/manual_register <manualId>`.
- Reply to a Telegram document with `/manual_register <manualId>`.

Constraints:

- `<manualId>` must already exist in `manuals.json`.
- The Telegram file identifier must come from the document payload, not from user text.
- Consumer, Installer, and Engineer cannot register bindings.
- The file extension must match the registry format and allowed extension list (`.pdf`, `.doc`, `.docx`, `.xls`,
  `.xlsx` by default).
- Confirmation messages show a safe manual display name, not file IDs, chat IDs, user IDs, local paths, package IDs, or
  JSON paths.

Private bot registration is the supported first path. If a storage group is used operationally, configure it as a
trusted process outside the repo and keep real identifiers out of committed files.

## Source-of-truth and safety rules

- Keep manual identity and coverage metadata in the registry or a reviewed database.
- Keep diagnostic JSON manual-bound.
- Keep same-code/same-equipment/same-meaning cases as one diagnostic answer with multiple source references.
- Do not derive diagnostic meaning from Telegram captions.
- Do not expose storage chat identifiers in user-facing text or logs.
- Do not send manuals to Consumer users.
- Do not commit PDF, DOC, DOCX, XLS, or XLSX manual binaries.
- Do not add diagnostic entries while registering manual delivery bindings.
