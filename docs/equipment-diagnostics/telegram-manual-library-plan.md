# Telegram file library

Status: ED-24LIB.1 adds the protected Telegram file library foundation. It extends existing
`TelegramManualBindings` instead of creating a parallel file-id system, adds persistent library access grants and
requests, and changes diagnostic document delivery to Owner/User manuals only.

## Current rules

- Diagnostic code flow can deliver only `OwnerManual` or `UserGuide` files with `CanUseForDiagnostics = true`.
- `ServiceManual`, `EngineeringManual`, debugging/internal/source documents, and error-code tables are library-only.
- Existing production service manual bindings default to `DocumentType = ServiceManual`, `MinRole = Engineer`,
  `IsLibraryVisible = true`, and `CanUseForDiagnostics = false`.
- If no Owner/User manual is bound for a diagnostic series, the diagnostic button returns
  `Руководство пока не добавлено` and never falls back to a service manual.
- Files are sent with Telegram `sendDocument(file_id)` and `protect_content=true`.
- `forwardMessage` and `copyMessage` are not used.
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
- bind/rebind protected files through the existing `/manual_bind` workflow.

Admin cannot approve/reject requests, grant/revoke access, or bind/rebind files by default.

## File catalog

ED-24LIB.1 exposes a minimal protected catalog:

- `Gree` section lists active `TelegramManualBindings` visible to the current role.
- File visibility requires `IsLibraryVisible = true`.
- File fetch requires active library access and `role >= MinRole`.
- Owner sees all active visible files.
- Engineer with grant can fetch `MinRole = Engineer` files.
- Installer with grant cannot fetch `MinRole = Engineer` service manuals.

Existing Gree service bindings for GMV9 Flex, GMV X, and GMV6 are service/library files. GMV Mini remains pending until a
binding is added.

## Bind workflow

`/manual_bind` remains the protected upload path but is Owner-only in ED-24LIB.1.

The current MVP keeps the existing series/PDF/confirmation workflow and stores the binding as:

- `DocumentType = ServiceManual`
- `MinRole = Engineer`
- `CanUseForDiagnostics = false`
- `IsLibraryVisible = true`

Future work can add document-type selection for Owner/User, Installation, and Service manuals.

## Future work

- Richer model matching and exact model-family variants.
- Mini / Mini Star / Slim handling.
- Remote controller categories.
- Owner/service taxonomy polish and document-type selection in bind flow.
- Optional file request workflow.
