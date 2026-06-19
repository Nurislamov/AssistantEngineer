# Equipment Diagnostics Telegram Delivery Deployment

## ED-17B Scope

ED-17B added one disabled-by-default inbound webhook transport:

```http
POST /api/v1/equipment-diagnostics/telegram/webhook
```

The controller validates the Telegram secret header, delegates to the deterministic Telegram adapter, and sends
reply text through an outbound `HttpClient` abstraction. The webhook endpoint remains available as an optional
fallback mode.

Production deployments that cannot receive Telegram inbound HTTPS reliably should use polling delivery through
Telegram Bot API `getUpdates`. Polling preserves the same deterministic Telegram adapter and only replaces inbound
transport.

## Required Secret Configuration

Keep secrets outside source control. The disabled defaults in `appsettings.json` contain null secret values.
For deployment, use the platform secret store or environment configuration:

```text
AssistantEngineer__EquipmentDiagnostics__Telegram__IsEnabled=true
AssistantEngineer__EquipmentDiagnostics__Telegram__InboundMode=Polling
AssistantEngineer__EquipmentDiagnostics__Telegram__DeleteWebhookOnStartup=true
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__Enabled=true
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__TimeoutSeconds=50
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__Limit=25
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__DelayAfterErrorSeconds=10
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__ProcessedMessageStoreFilePath=artifacts/operations/equipment-diagnostics-telegram-processed-messages.txt
AssistantEngineer__EquipmentDiagnostics__Telegram__Polling__ProcessedMessageStoreMaxEntries=5000
AssistantEngineer__EquipmentDiagnostics__Telegram__Commands__SyncOnStartup=true
AssistantEngineer__EquipmentDiagnostics__Telegram__ServiceRequests__NotificationChatId=<service-group-chat-id>
AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken=<secret>
AssistantEngineer__EquipmentDiagnostics__Telegram__BootstrapOwnerChatId=<chat-id>
AssistantEngineer__EquipmentDiagnostics__Telegram__DeniedChatIds__0=<blocked-chat-id>
AssistantEngineer__EquipmentDiagnostics__Telegram__EnableChatIdDiscovery=false
```

For webhook fallback mode, also configure:

```text
AssistantEngineer__EquipmentDiagnostics__Telegram__InboundMode=Webhook
AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=<secret>
```

The webhook secret must contain 1-256 letters, digits, underscores, or hyphens. When webhook mode is enabled, a
missing or invalid configured secret fails closed. The inbound request must provide the same value in
`X-Telegram-Bot-Api-Secret-Token`.

## Configure Telegram

The public webhook URL must use HTTPS. Telegram webhook delivery and `getUpdates` long polling are mutually
exclusive operational modes. For polling production mode, delete the Telegram webhook before or during startup:

```powershell
.\scripts\equipment-diagnostics\delete-telegram-webhook.ps1 -DropPendingUpdates
.\scripts\equipment-diagnostics\get-telegram-webhook-info.ps1
```

`getWebhookInfo` should show an empty or not configured webhook URL after deleteWebhook. The API container logs
should contain `Telegram polling started`. After sending `/start` in Telegram, logs should contain safe polling
update entries with update id and chat type, without token, webhook secret, message text, chat id, or username.
Duplicate Telegram updates for the same visible message are deduplicated by `chat.id + message.message_id` before
the handler sends a response. The dedupe store writes SHA-256 message identity hashes, not raw chat IDs.

Consumer-facing replies are Russian by default and use a Telegram reply keyboard. ED-22B keeps `🔎 Новый код`
available throughout the conversation. A Consumer without a saved phone sees `📞 Поделиться номером Telegram` with
`request_contact=true` and `✏️ Ввести другой номер`; a Consumer with a saved phone can use `✏️ Изменить номер`.
Telegram contact phones are saved with source `TelegramContact` and verified only when `contact.user_id` matches
`from.id`; manual phone input is normalized, saved with source `Manual`, and remains unverified. Phone numbers are
not printed in logs, `/me`, `/admin users`, or diagnostics. After either a contact message or manual phone is
accepted, any active diagnostic session is preserved and the previous prompt is restored when possible. Owner, Admin,
and Engineer technical replies may be split into multiple ordered `sendMessage` calls so Telegram's message limit is
not hit. If any chunk fails, the update is reported as outbound failed.

ED-22C also registers a safe global command menu with Telegram Bot API `setMyCommands` during startup when Telegram
is enabled and `BotToken` is configured. The menu contains `/start`, `/new`, `/phone`, `/me`, `/help`, `/history`,
`/last`, and `/requests`. It deliberately does not publish `/request`, `/admin_help`, `/admin users`, `/admin allow`, `/admin block`,
`/admin role`, or parameterized admin commands. Owner/Admin can still open `/admin_help` manually or through `/help`. Set
`AssistantEngineer__EquipmentDiagnostics__Telegram__Commands__SyncOnStartup=false` to skip menu synchronization.
Failure to sync the menu must log a warning and must not stop startup, polling, or webhook fallback.

ED-23A adds structured diagnostic history. Final completed diagnostics and not-found code requests are saved in
`TelegramDiagnosticCases`; intermediate prompts, `/start`, `/help`, `/me`, `/phone`, contact messages, reset buttons,
and admin commands are not saved as cases. `/history` returns the latest five cases for the current Telegram user,
and `/last` returns only that user's latest case. Owner/Admin history is still self-only in ED-23A. Stored data excludes
full incoming text, full bot response text, phone numbers, raw chat IDs, Telegram user IDs, tokens, and webhook
secrets. `CreatedAt` is stored in UTC, while `/history` and `/last` render timestamps in
`AssistantEngineer:EquipmentDiagnostics:Telegram:DisplayTimeZone` (`TELEGRAM_DISPLAY_TIME_ZONE` in Docker Compose),
defaulting to `Asia/Tashkent`. Empty or invalid values fall back to `Asia/Tashkent` with a sanitized warning and do
not crash the bot. Consumer `/last` uses the public-safe Russian summary instead of any saved English technical
summary; Engineer, Owner, and Admin may see the saved short technical summary.

ED-23B adds the PostgreSQL-backed `TelegramServiceRequests` foundation. After a final diagnosis, the main keyboard
offers `🛠 Нужен мастер`; hidden `/request` is an alias. A request requires the current user's latest diagnostic case
and a saved phone. `/requests` shows that user's latest five requests only. Active `New`/`InProgress` requests are
deduplicated per diagnostic case. The optional `TELEGRAM_SERVICE_REQUESTS_CHAT_ID` Docker variable maps to
`AssistantEngineer:EquipmentDiagnostics:Telegram:ServiceRequests:NotificationChatId`. If it is empty or notification
delivery fails, the database request remains created. Group messages include only phone saved/not-saved state and
phone source, never the full number. ED-23B does not add CRM, assignment, engineer status actions, a global admin
queue, web UI, Mini App, or photo/OCR.

ED-23C uses that same configured group for text-command queue actions: `/queue`, `/take`, `/assign`, `/done`,
`/cancel_request`, `/request_status`, and `/contact`. They are not added to `setMyCommands`. Engineers must register
in a private bot chat with `/start` before Owner/Admin grants the `Engineer` role. Group commands authenticate the
message sender through `from.id`; the group chat itself is never registered as a Consumer.

Taking or assigning a request sends customer status privately and attempts to send the full contact phone privately
to the assigned operator. `/contact <id>` is restricted to the assigned Engineer or Owner/Admin. Full phone numbers
are never sent to the group or logs. Failed private notifications do not roll back assignment or status changes.
Apply the `AddTelegramServiceRequestAssignments` migration before using these commands.

ED-23D adds inline action keyboards under new service request notifications and compact controls after `/queue`.
Polling requests both `message` and `callback_query` update types. Every callback is acknowledged through
`answerCallbackQuery`, then routed through the existing ED-23C role and service-group checks. The short callback
payload contains only an action code plus internal request/user ids.

The `Назначить` button lists enabled, non-blocked Telegram users with `Engineer`, `Admin`, or `Owner` roles. Contact
delivery remains private to the assigned Engineer or Owner/Admin; no full phone is rendered in the group or callback
data. ED-23C commands remain operational fallback. No database migration or new environment variable is required for
ED-23D.

ED-23D.1 turns each notification into a live card. Apply
`AddTelegramServiceRequestNotificationMessage`; it stores the Telegram chat/message identifiers and notification
timestamps. Take, assign, done, cancel, status refresh, and matching text commands edit the stored card. Assign uses
the same message for the engineer picker and provides `Назад`. Terminal cards expose only `Статус`. If editing fails,
the committed database state remains and a replacement safe card is sent and stored when possible. Contact still goes
only to the authorized private chat, and no new environment variable is required.

ED-23E adds private-chat button management for Telegram users. Owner/Admin can open `/admin_users`,
`/admin_pending`, or `/engineers`; callbacks edit the same admin card and are always acknowledged. Admin cannot manage
Admin/Owner accounts or assign Admin, while Owner accounts and destructive self-actions are protected. The affected
user's full phone and all raw Telegram identifiers remain absent from the UI. Existing parameterized admin commands
remain fallback and no admin command is added to the global command menu. ED-23E requires no migration or new
environment variable.

ED-23F adds the `TelegramServiceRequestEvents` audit table and requires the
`AddTelegramServiceRequestEvents` migration. Request cards include `История`; `/request_events <id>` is the hidden
command fallback. Owner/Admin may view any request history, while Engineer access is limited to the assigned request.
History is rendered in the configured local display time zone. Contact events record only private delivery
success/failure and internal database references—never the phone value, raw chat id, Telegram user id, callback data,
token, or secret. Audit append failure is sanitized and does not roll back the already committed request action.
ED-23F.1 also makes history reads and callback acknowledgement best-effort: a missing audit table or transient query
failure returns `История временно недоступна. Попробуйте позже.`, audit write failures do not block request workflows,
and every callback attempts `answerCallbackQuery` once so polling can advance safely. No new migration or environment
variable is required for ED-23F.1.

ED-23G adds service-group queue views: `/queue` (active), `/queue active`, `/queue new`, `/queue in-progress`,
`/queue closed`, `/queue all`, and `/my_requests`. Inline queue filters edit the original queue message through short
`sq:*` callback payloads and always acknowledge the callback. Queue query failures return
`Очередь временно недоступна. Попробуйте позже.` without failing polling. Queue output remains phone-safe, the
commands stay out of the global command menu, and ED-23G requires no migration or environment change.

Webhook fallback still requires a public HTTPS URL. Telegram supports webhook ports `443`, `80`, `88`, and `8443`.

Webhook dry run:

```powershell
.\scripts\equipment-diagnostics\set-telegram-webhook.ps1 `
  -BotToken $env:ASSISTANTENGINEER_TELEGRAM_BOT_TOKEN `
  -WebhookUrl https://example.test/api/v1/equipment-diagnostics/telegram/webhook `
  -WebhookSecret $env:ASSISTANTENGINEER_TELEGRAM_WEBHOOK_SECRET `
  -WhatIf
```

Remove `-WhatIf` only during an approved webhook fallback deployment. The script prints the sanitized webhook URL
and never prints the bot token.

Inspect or delete the configured webhook without printing the bot token:

```powershell
.\scripts\equipment-diagnostics\get-telegram-webhook-info.ps1 -WhatIf
.\scripts\equipment-diagnostics\delete-telegram-webhook.ps1 -WhatIf
```

`delete-telegram-webhook.ps1` accepts `-DropPendingUpdates` only when pending messages must deliberately be discarded.
Use the temporary `/id` or `/whoami` discovery flow documented in
[telegram-operations-checklist.md](telegram-operations-checklist.md), then disable discovery immediately.

## Production Checklist

- Set `InboundMode=Polling` and `Polling__Enabled=true` for production polling mode.
- Store bot token in the deployment secret store.
- Store webhook secret only when webhook fallback is enabled.
- Configure the bootstrap owner chat ID. Legacy `AllowedChatIds__0` is accepted only as compatibility fallback.
- Apply the `TelegramUsers`, `TelegramConversationSessions`, `AddTelegramUserPhoneSource`, and
  `AddTelegramDiagnosticCases`, `AddTelegramServiceRequests`, `AddTelegramServiceRequestAssignments`, and
  `AddTelegramServiceRequestNotificationMessage` and `AddTelegramServiceRequestEvents` EF migrations before enabling
  the bot.
- Confirm unknown users become `Consumer`, not Engineer/Admin.
- Confirm Consumer `/start`, `/help`, `/me`, code-first diagnostic replies, and button prompts are Russian and do not
  list admin commands.
- Confirm the global Telegram command menu lists only `/start`, `/new`, `/phone`, `/me`, `/help`, `/history`,
  `/last`, and `/requests`; `/request` and `/admin_help` are hidden from the global menu, and `/admin_help` remains reachable
  for Owner/Admin through `/help` or manual input.
- Confirm `/queue`, `/take`, `/assign`, `/done`, `/cancel_request`, `/request_status`, and `/contact` work only in
  the configured service group and remain absent from the global command menu.
- Confirm new request cards contain inline actions and `/queue` includes compact request buttons. Press each action
  and verify Telegram clears the callback spinner.
- Confirm take, assign, status, done, and cancel edit the existing request card without an extra group status message.
- Confirm assign edits the card into an engineer picker, `Назад` restores it, and terminal cards retain only `Статус`.
- Simulate an edit failure and confirm state remains committed while a replacement card is sent and its message id is
  stored.
- Confirm `Назначить` shows only enabled, non-blocked Engineer/Admin/Owner users and that every callback rechecks
  actor permissions.
- Confirm each Engineer opened the bot privately with `/start` before role assignment. Verify contact delivery goes
  only to the assigned Engineer or Owner/Admin private chat and never displays the full phone in the service group.
- Confirm `/admin_pending` shows a newly registered Consumer, `Сделать инженером` updates the detail card, and the
  engineer can then work in the service group.
- Confirm `/admin_users` and `/engineers` expose no phone value or raw Telegram identifier; Admin cannot manage
  Admin/Owner, Owner/self destructive actions are rejected, and callbacks clear the Telegram spinner.
- Confirm `/admin_users`, `/admin_pending`, and `/engineers` are absent from the global command menu but present in
  `/admin_help`.
- Confirm service request cards expose `История`, `/request_events <id>` shows local-time events, assigned Engineer
  and Owner/Admin access succeeds, and non-assigned Engineer/Consumer access is denied.
- Confirm contact and notification events contain no full phone, raw Telegram identifiers, callback data, token, or
  secret.
- Confirm `/history` and `/last` show only the current user's cases, include not-found requests, and do not print
  phone numbers, chat IDs, internal ids, or full bot responses.
- Confirm `/history` and `/last` display Asia/Tashkent local time: `сегодня`/`вчера` are local-day relative, and older
  rows include local date and time. Confirm Consumer `/last` does not show saved English technical summaries.
- Confirm Consumer diagnostic replies do not include confidence, source, internal traces, or `Response shortened`.
- Confirm the contact sharing and manual phone buttons appear before the phone number is saved, manual phone
  validation keeps bad input in phone-entry state, and the phone number is not logged or printed in admin lists.
- Confirm sending `🔎 Новый код`, `/new`, `/reset`, or `/cancel` clears the active conversation and asks for a new
  code.
- Promote users with `/admin role <chatId> <Owner|Admin|Engineer|Consumer>` from the bootstrap owner or an Admin.
- Review denied chat IDs; deny wins over allow.
- Keep chat ID discovery disabled except during initial access setup.
- Keep transport disabled until configuration review is complete.
- Delete the Telegram webhook before polling mode and verify `getWebhookInfo` has no URL.
- Verify API logs contain `Telegram polling started`.
- Keep `artifacts/operations` on durable storage. The Docker Compose scaffold mounts a named `api_operations`
  volume for the polling offset and processed-message dedupe files.
- Confirm global API rate-limit behavior and monitoring.
- Run webhook integration tests with fake outbound transport.
- Review Telegram token rotation and incident response.
- Production logs should not show EF Core/Npgsql SQL `SELECT`, `INSERT`, or `UPDATE` command text at Information level.
- Rebuild the backend image after ED-21B so the runtime image includes `libgssapi-krb5-2` and Npgsql no longer emits
  `libgssapi_krb5.so.2` missing-library warnings.

## Known Limitations

- polling offset and processed-message idempotency persistence are local operational files by default;
- no audit log or message queue;
- no web admin UI for users/roles;
- no ServiceLead/CRM, web admin UI, admin global diagnostic-history browser, or photo/OCR;
- no endpoint-specific rate limiter beyond the broader API setup;
- no AI, RAG, vector search, or manual-PDF access.

## Provider-Neutral Deployment Scaffold

ED-18A adds an example Docker Compose and Caddy scaffold under `deploy/`. It does not deploy Telegram, configure a
real domain, or enable the webhook. A future deployment must replace the placeholder domain, establish public
HTTPS, configure secrets outside Git, and pass the operations checklist before calling `set-telegram-webhook.ps1`.

ED-18B adds static deployment and environment validation plus release and rollback checklists. Run both deployment
validators before building images. Create/configure the BotFather token only at the final activation step, keep
chat ID discovery disabled after setup, and never commit secrets.

ED-19A adds in-memory webhook outcome counters and readiness validation. Counters expose no chat IDs, usernames,
message bodies, bot token, or webhook secret and reset whenever the process restarts.

Polling mode uses the same counters because it calls the same Telegram update handler as the webhook endpoint.
