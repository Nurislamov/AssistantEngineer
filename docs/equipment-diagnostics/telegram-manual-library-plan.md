# Telegram file library

Status: ED-24MAN.3a keeps the protected Telegram file library foundation and ED-24MAN.3 short callback/category work,
localizes visible OwnerManual labels, and hides InstallationManual from visible Telegram library/upload menus for now. It
extends existing `TelegramManualBindings` instead of creating a parallel file-id system, keeps persistent library access
grants and requests, and allows diagnostic document delivery only from `OwnerManual` bindings.

## Current rules

- Diagnostic code flow can deliver only `OwnerManual` files with `CanUseForDiagnostics = true`.
- `ServiceManual`, `ControllerGuide`, debugging/internal/source documents, and error-code tables are library-only.
- `InstallationManual` remains an internal library-only document type, but it is hidden from visible Telegram
  library/upload menus for now and is not delivered by diagnostic guide actions.
- Existing production service manual bindings default to `DocumentType = ServiceManual`, `MinRole = Engineer`,
  `IsLibraryVisible = true`, and `CanUseForDiagnostics = false`.
- If no Owner manual is bound for a diagnostic series, the diagnostic button returns
  `Руководство пока не добавлено` and never falls back to a service manual.
- If exactly one Owner manual is bound for the diagnostic series, the diagnostic button sends it immediately.
- If multiple Owner manuals are bound for the diagnostic series, the diagnostic button shows a safe file selection list
  by title/filename and sends only the selected Owner manual.
- Files are sent with Telegram `sendDocument(file_id)` and `protect_content=true`.
- `forwardMessage` and `copyMessage` are not used for library/manual delivery.
- Raw `TelegramFileId`, `FileUniqueId`, chat ids, local paths, package ids, source references, full filenames, and long
  manual ids are not placed into callback data.
- Generic library file buttons use short callback data such as `lib:f:<bindingId>` and re-check current binding state
  before sending a file.

## Library access

Library access is separate from Telegram role.

- `Owner` has implicit full library access and can grant/revoke access.
- `Admin` does not manage the library automatically and does not receive library access by role alone.
- `Admin`, `Engineer`, and `Installer` can use the library only when Owner has issued an active grant.
- `Consumer`, disabled, blocked, and unknown users cannot open or fetch library files.
- Every library command, callback, access request, grant/revoke action, and file-fetch callback re-checks role,
  enabled/blocked state, and active grant.
- Normal library navigation uses stable callback routes for home, Gree, access requests, access management, back, and
  cancel, so fresh inline buttons do not return the stale-action response.
- Old callbacks after revoke fail safely.

The main keyboard shows `📚 Библиотека` only when:

- role is `Owner`; or
- role is `Admin`, `Engineer`, or `Installer`, the user is enabled/unblocked, and an active grant exists.

The old global `📘 Руководства` button is not restored.

## Owner management

Owner can:

- open `/library`;
- review pending access requests;
- approve or reject requests;
- grant access with `/library_grant <chatId>`;
- revoke access with `/library_revoke <chatId>`;
- bind/rebind protected files through `/manual_bind` or the Owner-only `➕ Добавить файл` library action.

Access request lists show the requester's display name, username when available, role, and chat id. Approve/reject
actions notify the requester in private chat through bot `sendMessage`; approve includes a refreshed main keyboard with
the library entry when access is valid, while reject keeps the library entry hidden unless another active grant allows it.
Grant/revoke paths also refresh the target user's keyboard when the notification can be delivered.

Admin cannot approve/reject requests, grant/revoke access, or bind/rebind files by default.

## File catalog

ED-24MAN.2 exposes the first structured Gree catalog:

- Root shows `Gree`, Owner-only `➕ Добавить файл`, `Запросы доступа`, `Управление доступом`, and `Отмена`.
- `Gree` shows `Наружные`, `Внутренние`, `Пульты / Controllers`, `Аксессуары и прочее`, and `Назад`.
- `Пульты / Controllers` moved under `Gree`.
- `Gree -> Наружные` shows fixed product lines: `GMV6`, `GMV6 HR`, `GMV Mini / Slim`, `GMV X`, `GMV9 Flex`.
- Each outdoor product line shows document buckets: `📕 Сервисные мануалы` and
  `📘 Руководства пользователя`.
- Empty buckets show `Пока файлов нет.`
- `Gree -> Наружные -> GMV Mini / Slim -> 📘 Руководства пользователя` supports multiple active files. This is needed because
  GMV Mini / Slim owner manuals are split by model/capacity groups, for example 8-16kW, 12-18kW, and 22-35kW groups.
- GMV Mini / Slim `📘 Руководства пользователя` lists all active files by safe title/filename.
- `🛠 Installation Manual` is not shown in visible library buckets; stale callbacks fail safely or return the nearest
  current menu and do not send files.
- GMV9 Flex OwnerManual is currently unavailable/pending; diagnostics keep returning `Руководство пока не добавлено`
  until an OwnerManual is explicitly bound.
- `Gree -> Внутренние` shows typed categories: `Настенные`, `Кассетные`, `Канальные`, and
  `📕 Сервисные мануалы`.
- `Gree -> Пульты / Controllers` shows typed categories: `Настенные` and `Беспроводные ИК`.
- Indoor and Controllers intentionally do not have a generic `Прочее` bucket. Future unclassified equipment files must
  add an explicit category rule before they become visible in a typed user category.
- Category file lists show full numbered filenames in the message body and short numbered button labels with safe
  `lib:f:<bindingId>` callbacks.
- `Gree -> Аксессуары и прочее` remains a free section and paginates when the list is long.
- File visibility requires `IsLibraryVisible = true`.
- File fetch requires active library access and `role >= MinRole`.
- Owner sees all active visible files.
- Engineer with grant can fetch `MinRole = Engineer` files.
- Installer with grant cannot fetch `MinRole = Engineer` service manuals.

Existing Gree service bindings for GMV9 Flex, GMV X, GMV6, and GMV Mini remain visible under
`Gree -> Наружные -> <product> -> 📕 Сервисные мануалы`.

## Bind workflow

`/manual_bind` remains protected and Owner-only. ED-24MAN.2 also exposes the same flow through `➕ Добавить файл`.

The flow is:

- Brand: `Gree`.
- Section: `Outdoor`, `Indoor`, `Controllers`, or `Accessories`.
- Outdoor: choose product line, then visible `ServiceManual` or `OwnerManual`.
- Free sections: choose the section-appropriate document type, send a PDF document, confirm, and save into the flat
  section list.
- Re-uploading the same outdoor service `Brand + ProductLine + DocumentType` or the same free-section key asks for
  replacement confirmation.
- Re-uploading an outdoor `OwnerManual` with the same generated title/filename key asks for replacement confirmation
  and replaces only that matching file. Uploading a different GMV Mini / Slim OwnerManual adds another active file and
  does not deactivate other GMV Mini / Slim OwnerManual files or ServiceManual files.
- Cancel preserves the old binding.

PDF validation requires a Telegram document, `.pdf`, `file_id`, non-empty safe filename, and a PDF-compatible content
type. Outdoor prompts recommend a `Gree + product + document type` filename without relying on raw production ids or
Telegram file ids.

Stored document policy:

- `ServiceManual`: `MinRole = Engineer`, library-only, `CanUseForDiagnostics = false`.
- `OwnerManual`: `MinRole = Consumer`, can be used by diagnostics only for outdoor product lines.
- `InstallationManual`: `MinRole = Installer`, internal/library-only, hidden from visible Telegram library/upload menus.
- `ControllerGuide`: `MinRole = Installer`, library-only.

Visible document-type labels:

- `ServiceManual`: `📕 Сервисные мануалы`; internal enum/database value remains `ServiceManual`.
- `OwnerManual`: `📘 Руководства пользователя`; internal enum/database value remains `OwnerManual`.
- The diagnostic contextual button remains the shorter `📘 Руководство`.

No PDF binaries are committed to the repository for ED-24MAN.3a.

## ED-24MAN.3 data correction

The known uploaded Gree Indoor service manual row was originally stored as `DocumentType = OwnerManual` and
`MinRole = Consumer`. ED-24MAN.3 includes an idempotent VPS SQL repair script:

`scripts/deployment/manual-library/fix-gree-indoor-service-manual-binding.sql`

The script selects the exact row before and after update, then updates only:

- `Brand = 'Gree'`
- `Series = 'Indoor'`
- `FileName = 'Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf'`

Expected corrected metadata:

- `DocumentType = 'ServiceManual'`
- `MinRole = 'Engineer'`
- `CanUseForDiagnostics = false`
- `IsLibraryVisible = true`
- `IsActive` remains unchanged

Production correction is not complete until the script is executed on the VPS and its before/after output is checked.

## ED-24MAN.4 U-Match and ERV library sections

ED-24MAN.4 adds two structured Gree library sections without changing the diagnostic manual-delivery policy:

- `Gree -> Полупром / U-Match`
- `Gree -> Вентиляция ERV`

Both sections expose only visible document buckets for:

- `ServiceManual`
- `OwnerManual`

`InstallationManual` remains hidden from visible Telegram library/upload menus and remains library-only. Diagnostic guide
delivery remains OwnerManual-only; ServiceManual files are library-only and require the configured library role/access
checks.

ED-24MAN.4 also adds idempotent production metadata correction scripts for the expected uploaded service-manual rows:

- `scripts/deployment/manual-library/fix-gree-umatch-r32-service-manual-binding.sql`
- `scripts/deployment/manual-library/fix-gree-erv-b-series-service-manual-binding.sql`

The scripts normalize only manual-library metadata. They do not add Telegram file ids, PDF binaries, secrets, or runtime
diagnostic cards.

## ED-24MAN.4a diagnostic guide bindings

ED-24MAN.4a fixes OwnerManual upload and production metadata handling for U-Match R32 and ERV B Series:

- OwnerManual uploads in the `Полупром / U-Match` and `Вентиляция ERV` sections now set
  `CanUseForDiagnostics = true`.
- U-Match R32 supports the cassette and duct OwnerManual files as a safe selection list.
- ERV B Series sends its single wired-controller OwnerManual directly.
- ServiceManual remains library-only and InstallationManual remains hidden and ineligible for diagnostic delivery.

## ED-24MAN.4b ERV/U-Match diagnostic guide correction

ED-24MAN.4b keeps diagnostic delivery OwnerManual-only while correcting production metadata and selection UX:

- ERV diagnostic lookup requires exact `Brand = Gree`, `Series = ERV B Series`, `DocumentType = OwnerManual`,
  `CanUseForDiagnostics = true`, and `IsActive = true`.
- The ERV Installation/Startup/Maintenance document is intentionally stored as an ERV-specific `OwnerManual` for this
  diagnostic-guide context. Its filename is
  `Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf`.
- Generic Controllers rows, ControllerGuide rows, XK46, and XE7A documents are never fallback ERV diagnostic guides.
- If one eligible ERV guide remains it is sent directly with protected content; multiple eligible ERV-specific guides
  produce an ERV-only selection.
- U-Match keeps full cassette/duct filenames in the message body while buttons distinguish
  `Кассетные 3.5-16kW` and `Канальные 3.5-16kW`.
- Diagnostic file callbacks contain only a short opaque token and remain within Telegram's 64-byte limit.
- ServiceManual remains library-only. InstallationManual remains hidden from generic visible library/upload menus.
- Callback data contains only a short derived token and remains within Telegram's 64-byte limit.

Production metadata scripts:

- `scripts/deployment/manual-library/fix-gree-umatch-r32-service-manual-binding.sql`
- `scripts/deployment/manual-library/fix-gree-umatch-r32-owner-manual-bindings.sql`
- `scripts/deployment/manual-library/fix-gree-erv-b-series-service-manual-binding.sql`
- `scripts/deployment/manual-library/fix-gree-erv-b-series-owner-manual-bindings.sql`

The scripts use the real `TelegramManualBindings` columns `FileName` and `UpdatedAt`, are idempotent, do not insert rows,
and print the final matching metadata.

## ED-24MAN.4d controller selected-file captions

ED-24MAN.4d keeps the production metadata unchanged and fixes display logic when a binding has a correct `FileName` but a
stale `Title`:

- Generic manual-library button labels, send status text, and document captions prefer the selected binding filename.
- Gree Controllers short labels are derived from the selected binding filename, so `YAP1F / YV1L1`,
  `XE7A-23H / XE7A-23HC`, `XE7A-24H / XE7A-24HC`, and `XK46` cannot be replaced by a stale ERV title.
- Controller classification uses filename text instead of stale title text.
- U-Match diagnostic selection and selected document captions use the selected binding filename; ERV exact-guide matching
  also uses filename text.
- Library and diagnostic callback data remains short id/token-based and does not include filenames, Telegram file ids,
  file_unique_id, source references, or long manual ids.

## Future work

- Richer model matching and exact model-family variants.
- Mini / Mini Star / Slim exact model-family handling beyond the current multi-file OwnerManual bucket.
- GMV9 Flex OwnerManual acquisition/binding when an approved source becomes available.
- Optional file request workflow.
- EF enum default/sentinel cleanup can continue under ED-24EF.1 for remaining warnings outside the fixed
  `TelegramLibraryDocumentType.OwnerManual` storage path.
