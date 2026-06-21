# Telegram manual library plan

Status: plan only. ED-24F.0, ED-24F.1a, and ED-24F.1b do not implement file upload, storage, delivery, buttons, callbacks, or Telegram runtime changes.

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

The button should resolve reviewed `manualId` values from the selected diagnostic answer, enforce the current user role, and return all manuals where the selected code appears. If one answer has multiple `sourceReferences[]`, manual delivery should use those references rather than asking the user to choose a source. It must not generalize one manual across Gree series.

ED-24F.1b prepares this for the GMV IDU merge by adding `manualId` references for both the original GMV6 service manual and `GC202004-X` on 38 existing indoor entries. Runtime delivery is still deferred.

## Source-of-truth and safety rules

- Keep manual identity and coverage metadata in the registry or a reviewed database.
- Keep diagnostic JSON manual-bound.
- Keep same-code/same-equipment/same-meaning cases as one diagnostic answer with multiple source references.
- Do not derive diagnostic meaning from Telegram captions.
- Do not expose storage chat identifiers in user-facing text or logs.
- Do not send manuals to Consumer users.
- Do not implement Telegram file storage or delivery in ED-24F.0, ED-24F.1a, or ED-24F.1b.
