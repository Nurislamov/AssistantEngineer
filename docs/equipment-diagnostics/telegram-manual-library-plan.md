# Telegram manual library plan

Status: plan only. ED-24F.0 does not implement file upload, storage, delivery, buttons, callbacks, or Telegram runtime changes.

## Intended model

A closed Telegram group or channel may later be used as a controlled file-delivery library for reviewed manuals. Telegram must not be the only source of truth.

The repository or a future reviewed database model should retain metadata and references such as:

- `manualId`
- `telegramChatId` or a safe non-secret alias
- `messageId`
- `fileId`
- `fileUniqueId`
- `fileName`
- `caption`
- `documentCode`
- `series`
- `equipmentFamily`
- `importedPackageIds`
- `accessPolicy`

Documentation examples must never contain a real private chat ID, bot token, webhook secret, phone number, or raw callback payload.

## Access policy

| Role | Manual delivery |
|---|---|
| Consumer / Client | Denied |
| Installer / Монтажник | Allowed |
| Engineer / Сервис-инженер | Allowed |
| Admin | Allowed |
| Owner | Allowed |

An allowed role is not enough by itself. The manual record must also be reviewed and marked `eligibleForTelegramLibrary`.

## Possible future diagnostic action

A later stage may add a post-diagnostic button:

`📘 Открыть руководство`

The button should resolve a reviewed `manualId`, enforce the current user role, and return only the manual associated with the selected diagnostic source. It must not generalize one manual across Gree series.

## Source-of-truth and safety rules

- Keep manual identity and coverage metadata in the registry or a reviewed database.
- Keep diagnostic JSON manual-bound.
- Do not derive diagnostic meaning from Telegram captions.
- Do not expose storage chat identifiers in user-facing text or logs.
- Do not send manuals to Consumer users.
- Do not implement Telegram file storage or delivery in ED-24F.0.
