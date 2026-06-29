# Telegram manual library

Status: ED-24MAN.1 adds the protected production binding path for contextual diagnostic manuals. It supports
role-gated manual requests after diagnostics, Admin/Owner `/manual_bind` upload by Gree series, persistent EF Core
Telegram `file_id` bindings, and protected `sendDocument(file_id)` delivery. It does not add a Mini App, public manual
search, OCR, external storage provider, local PDF archive, committed manual binaries, diagnostic codes, routing changes,
or source-reference changes.

## Intended model

Telegram `file_id` is the delivery handle. Telegram must not be the only source of truth: repository metadata remains in
`data/equipment-diagnostics/manual-library/manuals.json`, while production runtime file bindings live in the existing
application database through the `TelegramManualBindings` EF Core table.

Production runtime file binding:

- Table: `TelegramManualBindings`.
- Migration: `AddTelegramManualBindings`.
- Scope: brand + series, currently Gree GMV6, Gree GMV Mini, Gree GMV X, and Gree GMV9 Flex.
- Stored metadata: Telegram `file_id`, optional `file_unique_id`, safe filename, content type, size, uploader Telegram
  user/chat ids, role, source, timestamps, and active state.
- Real `telegramFileId` values must not be committed.
- If the binding file is missing or a manual has no binding, `/manuals` still works and says in Russian that the
  manual is known but the file is not connected yet.
- If some manuals are connected and others are missing, `/manuals` sends connected documents and lists the missing
  manuals instead of failing the whole request.

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

- Contextual diagnostic action: `📄 Мануал`, shown only after a concrete found Gree series/code to Installer,
  Engineer, Admin, and Owner roles.
- Technical reply keyboard button: `📘 Руководства`.
- Command: `/manuals`.

Consumer users never see `📄 Мануал`. A manually submitted action or callback is denied before manual metadata is
resolved and does not disclose titles, source references, document codes, file IDs, or storage identifiers. Ambiguous
and not-found diagnostic responses do not show the contextual action.

The request uses the last successful diagnostic from Telegram history. It resolves reviewed `manualId` values from the
selected diagnostic answer and enforces the current user role. If one answer has multiple `sourceReferences[]`, manual
delivery uses all relevant references rather than asking the user to choose a source. It must not generalize one manual
across Gree series.

The contextual action additionally requires the concrete series stored with the latest successful diagnostic. When a
reviewed Telegram `file_id` binding exists, delivery uses `sendDocument` with `protect_content=true`. When no binding
exists, the technical user receives `Мануал пока не привязан` without source details or fabricated identifiers.
`copyMessage` and `forwardMessage` are intentionally not used.

Displayed and stored diagnostic codes use the canonical casing from the selected JSON/manual entry. Lookup may be
case-insensitive, but exact code casing is preferred first. If multiple entries differ only by case and the user did not
enter an exact match, the bot asks for the exact code shown on the equipment.

For entries without `sourceReferences[].manualId`, the fallback match is intentionally narrow: exact `sourceName` to
registry `documentTitle` or exact source name to registry file name without extension. No loose/fuzzy manual matching is
allowed.

## Registration and binding flow

Only Admin and Owner can register or bind a manual file.

Supported flows:

- Production contextual binding: `/manual_bind`, then choose series, send the PDF document to the bot, and confirm bind
  or explicit replace.
- Send a Telegram document to the bot with caption `/manual_register <manualId>`.
- Reply to a Telegram document with `/manual_register <manualId>`.
- Remove a binding with `/manual_unregister <manualId>`.
- List safe binding state with `/manual_bindings`.

Constraints:

- `<manualId>` must already exist in `manuals.json`.
- The Telegram file identifier must come from the document payload, not from user text.
- `/manual_bind` accepts only PDF documents whose filename contains both `Gree` and the selected series token. It shows a
  recommended filename and keeps the pending in-memory flow active after non-document or invalid-file messages.
- Rebinding an existing series requires explicit confirmation. Cancel leaves the active binding unchanged.
- Consumer, Installer, and Engineer cannot register, unregister, or list bindings.
- The file extension must match the registry format and allowed extension list (`.pdf`, `.doc`, `.docx`, `.xls`,
  `.xlsx` by default).
- Confirmation messages show a safe manual display name, not file IDs, chat IDs, user IDs, local paths, package IDs, or
  JSON paths.
- `/manual_bindings` shows only safe display name, document code when available, connection state, and safe original
  filename. It must not show `file_id`, `chat_id`, `user_id`, token values, package IDs, or local paths.

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
