# Deployment Environment Variables

`deploy/.env.example` is a placeholder-only template. Copy it to ignored `deploy/.env` and configure values outside
Git. Do not put real bot tokens, webhook secrets, domains, or credentials in source-controlled files.

## Scaffold Variables

| Variable | Purpose | Default/example |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | API environment | `Production` |
| `API_LOCAL_PORT` | Loopback API smoke port | `8080` |
| `FRONTEND_LOCAL_PORT` | Loopback frontend smoke port | `8081` |
| `HTTP_PORT` / `HTTPS_PORT` | Reverse proxy ports | `80` / `443` |
| `VITE_API_BASE_URL` | Existing frontend build-time API base URL | `http://localhost/` |
| `VITE_API_VERSION` | API version used by frontend routes | `1` |
| `TELEGRAM_IS_ENABLED` | Explicit transport enable switch | `false` |
| `TELEGRAM_INBOUND_MODE` | Telegram inbound transport | `Polling` |
| `TELEGRAM_POLLING_ENABLED` | Polling worker enable switch | `false` |
| `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY` | Temporary `/id` setup switch | `false` |
| `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID` | Emergency owner bootstrap chat ID | empty placeholder |
| `TELEGRAM_ALLOWED_CHAT_ID` | Docker Compose shortcut for one closed-beta chat allowlist entry | empty placeholder |
| `TELEGRAM_ALLOWED_USERNAME` | Optional Docker Compose shortcut for one username allowlist entry | empty placeholder |

Telegram `BotToken`, `WebhookSecret`, `BootstrapOwnerChatId`, `AllowedChatIds`, `AllowedUsernames`, and `DeniedChatIds` use the nested
ASP.NET environment variable names shown in `.env.example`. They intentionally have empty committed values. For
Docker Compose deployments, prefer `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID=<telegram-chat-id>` for the emergency owner.
`TELEGRAM_ALLOWED_CHAT_ID=<telegram-chat-id>` remains a compatibility fallback for older closed-beta deployments.
For direct ASP.NET configuration, use
`AssistantEngineer__EquipmentDiagnostics__Telegram__BootstrapOwnerChatId=<telegram-chat-id>`.

Telegram users are stored in the existing application PostgreSQL database through the `TelegramUsers` EF migration.

Validate the ignored production file without printing secrets:

```powershell
.\scripts\deployment\validate-production-env.ps1 -EnvPath deploy/.env
```

For source-controlled placeholder validation only:

```powershell
.\scripts\deployment\validate-production-env.ps1 -EnvPath deploy/.env.example -AllowPlaceholders
```

The production validator requires `ASPNETCORE_ENVIRONMENT=Production`. When Telegram is explicitly enabled it
requires a bot token, a webhook secret only for webhook mode, and at least one allowlist entry unless chat ID
discovery is temporarily enabled for setup. The preferred access entry is `BootstrapOwnerChatId`; legacy
`AllowedChatIds__0` is accepted as bootstrap compatibility. Discovery emits a warning and keeps readiness unsafe
until disabled.

The CI deployment dry run uses only `.env.example`. It does not read a real production `.env`, enable Telegram, or
send secret values to image builds.
