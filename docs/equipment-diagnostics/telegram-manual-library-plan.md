# Telegram file library

Status: ED-24MAN.2 has the protected Telegram file library foundation, callback/UX fixes, and the first Gree manual
taxonomy. It extends existing `TelegramManualBindings` instead of creating a parallel file-id system, keeps persistent
library access grants and requests, and allows diagnostic document delivery only from `OwnerManual` bindings.

## Current rules

- Diagnostic code flow can deliver only `OwnerManual` files with `CanUseForDiagnostics = true`.
- `ServiceManual`, `InstallationManual`, `ControllerGuide`, debugging/internal/source documents, and error-code tables
  are library-only.
- Existing production service manual bindings default to `DocumentType = ServiceManual`, `MinRole = Engineer`,
  `IsLibraryVisible = true`, and `CanUseForDiagnostics = false`.
- If no Owner manual is bound for a diagnostic series, the diagnostic button returns
  `Руководство пока не добавлено` and never falls back to a service manual.
- Files are sent with Telegram `sendDocument(file_id)` and `protect_content=true`.
- `forwardMessage` and `copyMessage` are not used for library/manual delivery.
- Raw `TelegramFileId`, `FileUniqueId`, DB ids, chat ids, local paths, package ids, and source references are not shown
  to users.

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
- Each outdoor product line shows document buckets: `📕 Service Manual`, `📘 Owner Manual`,
  `🛠 Installation Manual`.
- Empty buckets show `Пока файлов нет.`
- Free sections (`Внутренние`, `Пульты / Controllers`, `Аксессуары и прочее`) list files directly as safe
  title/filename buttons without a nested model tree and paginate when the list is long.
- File visibility requires `IsLibraryVisible = true`.
- File fetch requires active library access and `role >= MinRole`.
- Owner sees all active visible files.
- Engineer with grant can fetch `MinRole = Engineer` files.
- Installer with grant cannot fetch `MinRole = Engineer` service manuals.

Existing Gree service bindings for GMV9 Flex, GMV X, GMV6, and GMV Mini remain visible under
`Gree -> Наружные -> <product> -> 📕 Service Manual`.

## Bind workflow

`/manual_bind` remains protected and Owner-only. ED-24MAN.2 also exposes the same flow through `➕ Добавить файл`.

The flow is:

- Brand: `Gree`.
- Section: `Outdoor`, `Indoor`, `Controllers`, or `Accessories`.
- Outdoor: choose product line, then `ServiceManual`, `OwnerManual`, or `InstallationManual`.
- Free sections: choose the section-appropriate document type, send a PDF document, confirm, and save into the flat
  section list.
- Re-uploading the same outdoor `Brand + ProductLine + DocumentType` or the same free-section key asks for replacement
  confirmation.
- Cancel preserves the old binding.

PDF validation requires a Telegram document, `.pdf`, `file_id`, non-empty safe filename, and a PDF-compatible content
type. Outdoor prompts recommend a `Gree + product + document type` filename without relying on raw production ids or
Telegram file ids.

Stored document policy:

- `ServiceManual`: `MinRole = Engineer`, library-only, `CanUseForDiagnostics = false`.
- `OwnerManual`: `MinRole = Consumer`, can be used by diagnostics only for outdoor product lines.
- `InstallationManual`: `MinRole = Installer`, library-only.
- `ControllerGuide`: `MinRole = Installer`, library-only.

## Future work

- Richer model matching and exact model-family variants.
- Mini / Mini Star / Slim handling.
- Optional file request workflow.
- EF enum default/sentinel cleanup can continue under ED-24EF.1 for remaining warnings outside the fixed
  `TelegramLibraryDocumentType.OwnerManual` storage path.
