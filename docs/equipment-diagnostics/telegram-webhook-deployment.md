# Equipment Diagnostics Telegram Webhook Deployment

## ED-17B Scope

ED-17B adds one disabled-by-default inbound webhook transport:

```http
POST /api/v1/equipment-diagnostics/telegram/webhook
```

The controller validates the Telegram secret header, delegates to the deterministic Telegram adapter, and sends
reply text through an outbound `HttpClient` abstraction. It does not diagnose, read knowledge files, or add a
long-polling service.

## Required Secret Configuration

Keep secrets outside source control. The disabled defaults in `appsettings.json` contain null secret values.
For deployment, use the platform secret store or environment configuration:

```text
AssistantEngineer__EquipmentDiagnostics__Telegram__IsEnabled=true
AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken=<secret>
AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=<secret>
AssistantEngineer__EquipmentDiagnostics__Telegram__AllowedChatIds__0=<chat-id>
AssistantEngineer__EquipmentDiagnostics__Telegram__DeniedChatIds__0=<blocked-chat-id>
AssistantEngineer__EquipmentDiagnostics__Telegram__EnableChatIdDiscovery=false
```

The webhook secret must contain 1-256 letters, digits, underscores, or hyphens. When enabled, a missing or invalid
configured secret fails closed. The inbound request must provide the same value in
`X-Telegram-Bot-Api-Secret-Token`.

## Configure Telegram

The public webhook URL must use HTTPS. Telegram webhook delivery and `getUpdates` long polling are mutually
exclusive operational modes; this project implements webhook transport only. Telegram supports webhook ports
`443`, `80`, `88`, and `8443`.

Dry run:

```powershell
.\scripts\equipment-diagnostics\set-telegram-webhook.ps1 `
  -BotToken $env:ASSISTANTENGINEER_TELEGRAM_BOT_TOKEN `
  -WebhookUrl https://example.test/api/v1/equipment-diagnostics/telegram/webhook `
  -WebhookSecret $env:ASSISTANTENGINEER_TELEGRAM_WEBHOOK_SECRET `
  -WhatIf
```

Remove `-WhatIf` only during an approved deployment. The script prints the sanitized webhook URL and never prints
the bot token.

Inspect or delete the configured webhook without printing the bot token:

```powershell
.\scripts\equipment-diagnostics\get-telegram-webhook-info.ps1 -WhatIf
.\scripts\equipment-diagnostics\delete-telegram-webhook.ps1 -WhatIf
```

`delete-telegram-webhook.ps1` accepts `-DropPendingUpdates` only when pending messages must deliberately be discarded.
Use the temporary `/id` or `/whoami` discovery flow documented in
[telegram-operations-checklist.md](telegram-operations-checklist.md), then disable discovery immediately.

## Production Checklist

- Configure HTTPS and the exact public webhook URL.
- Store bot token and webhook secret in the deployment secret store.
- Configure allowed chat IDs and/or usernames.
- Review denied chat IDs; deny wins over allow.
- Keep chat ID discovery disabled except during initial access setup.
- Keep transport disabled until configuration review is complete.
- Confirm global API rate-limit behavior and monitoring.
- Run webhook integration tests with fake outbound transport.
- Review Telegram token rotation and incident response.

## Known Limitations

- no database, audit log, message queue, or retry persistence;
- no admin UI for allowed chats;
- no endpoint-specific rate limiter beyond the broader API setup;
- no long polling;
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
