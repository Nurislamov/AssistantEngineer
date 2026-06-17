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
is enabled and `BotToken` is configured. The menu contains `/start`, `/new`, `/phone`, `/me`, `/help`, and
`/admin_help` only. It deliberately does not publish `/admin users`, `/admin allow`, `/admin block`, `/admin role`,
or parameterized admin commands. Set
`AssistantEngineer__EquipmentDiagnostics__Telegram__Commands__SyncOnStartup=false` to skip menu synchronization.
Failure to sync the menu must log a warning and must not stop startup, polling, or webhook fallback.

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
- Apply the `TelegramUsers`, `TelegramConversationSessions`, and `AddTelegramUserPhoneSource` EF migrations before
  enabling the bot.
- Confirm unknown users become `Consumer`, not Engineer/Admin.
- Confirm Consumer `/start`, `/help`, `/me`, code-first diagnostic replies, and button prompts are Russian and do not
  list admin commands.
- Confirm the global Telegram command menu lists only `/start`, `/new`, `/phone`, `/me`, `/help`, and `/admin_help`;
  `/admin_help` is unavailable to Consumer and Engineer, and Owner/Admin `/help` points to `/admin_help` instead of
  listing parameterized admin commands.
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
- no diagnostic history, ServiceLead/CRM, web admin UI, or photo/OCR;
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
