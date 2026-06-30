# Deployment Environment Variables

`deploy/.env.example` is a placeholder-only template. Copy it to ignored `deploy/.env` and configure values outside
Git. Do not put real bot tokens, webhook secrets, domains, or credentials in source-controlled files.

Do not paste full `docker compose config` output from production into chats, issues, logs, or reviews. It resolves
`deploy/.env` placeholders and can print API keys, webhook secrets, database passwords, and connection strings. Use
`validate-deployment-scaffold.ps1 -RunDockerComposeConfig` for placeholder-only scaffold validation and see
[production-secret-rotation-runbook.md](production-secret-rotation-runbook.md) for safe production checks and rotation
steps.

## Scaffold Variables

| Variable | Purpose | Default/example |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | API environment | `Production` |
| `API_LOCAL_PORT` | Loopback API smoke port | `8080` |
| `FRONTEND_LOCAL_PORT` | Loopback frontend smoke port | `8081` |
| `HTTP_PORT` / `HTTPS_PORT` | Reverse proxy ports | `80` / `443` |
| `VITE_API_BASE_URL` | Existing frontend build-time API base URL | `http://localhost/` |
| `VITE_API_VERSION` | API version used by frontend routes | `1` |
| `ASSISTANTENGINEER_DATAPROTECTION_KEYS_PATH` | Persistent ASP.NET DataProtection key-ring directory | `/home/app/.aspnet/DataProtection-Keys` |
| `ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PATH` | Optional PFX path used to encrypt DataProtection keys at rest | empty placeholder |
| `ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PASSWORD` | Optional PFX password; required only by the selected certificate | empty placeholder |
| `TELEGRAM_IS_ENABLED` | Explicit transport enable switch | `false` |
| `TELEGRAM_INBOUND_MODE` | Telegram inbound transport | `Polling` |
| `TELEGRAM_COMMANDS_SYNC_ON_STARTUP` | Sync the safe global Telegram command menu during startup when Telegram is enabled and token is configured | `true` |
| `TELEGRAM_DISPLAY_TIME_ZONE` | Display time zone for Telegram `/history` and `/last`; invalid or empty values fall back to `Asia/Tashkent` | `Asia/Tashkent` |
| `TELEGRAM_OPERATOR_INBOX_ENABLED` | Enable the closed Owner operator inbox bridge | `false` |
| `TELEGRAM_OPERATOR_CHAT_ID` | Telegram group/supergroup chat id used for operator cards and Owner replies | empty placeholder |
| `TELEGRAM_OPERATOR_LOG_DIAGNOSTICS` | Mirror short successful diagnostic events to the operator inbox; normal default avoids diagnostic noise | `false` |
| `TELEGRAM_SERVICE_REQUESTS_CHAT_ID` | Optional Telegram group chat for new service-request notifications | empty placeholder |
| `TELEGRAM_SERVICE_REQUESTS_NOTIFY_ON_CREATE` | Send group notifications when the service-request chat is configured | `true` |
| `TELEGRAM_POLLING_ENABLED` | Polling worker enable switch | `false` |
| `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY` | Temporary `/id` setup switch | `false` |
| `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID` | Emergency owner bootstrap chat ID | empty placeholder |
| `TELEGRAM_ALLOWED_CHAT_ID` | Docker Compose shortcut for one closed-beta chat allowlist entry | empty placeholder |
| `TELEGRAM_ALLOWED_USERNAME` | Optional Docker Compose shortcut for one username allowlist entry | empty placeholder |

## DataProtection Key Persistence

ED-24OPS.3 configures the API with the stable DataProtection application name `AssistantEngineer`. Docker Compose
mounts the named volume `assistantengineer_dataprotection_keys` at
`/home/app/.aspnet/DataProtection-Keys`, and the backend image prepares that directory for the non-root application
user. Container rebuilds and replacements therefore retain the key ring as long as the named volume is preserved.

`ASSISTANTENGINEER_DATAPROTECTION_KEYS_PATH` can override the key directory for direct or non-Compose deployments.
The API creates the configured directory during startup and fails startup if the path cannot be prepared. Keep that
path on persistent storage and writable by the API process.

Key encryption at rest is optional. To enable it, mount a PFX outside source control and set
`ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PATH` plus
`ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PASSWORD` when the PFX requires a password. Never commit the PFX or
password, and do not paste resolved Compose configuration into logs or tickets. Leaving both certificate variables
empty keeps startup valid but stores the persistent XML key ring without certificate encryption.

After deployment, verify persistence without printing key contents or secrets:

```bash
docker compose exec assistantengineer-api sh -lc 'ls -la /home/app/.aspnet/DataProtection-Keys || true'
docker compose exec assistantengineer-api sh -lc \
  'test -d /home/app/.aspnet/DataProtection-Keys && find /home/app/.aspnet/DataProtection-Keys -maxdepth 1 -name "key-*.xml" -print'
docker compose logs --since=10m assistantengineer-api \
  | grep -Ei "DataProtection|warning|error|exception|failed"
```

Recreate the API container and repeat the filename-only check. Existing key filenames should remain present. Do not
remove `assistantengineer_dataprotection_keys` during routine deploy or rollback operations.

Telegram `BotToken`, `WebhookSecret`, `BootstrapOwnerChatId`, `AllowedChatIds`, `AllowedUsernames`, and `DeniedChatIds` use the nested
ASP.NET environment variable names shown in `.env.example`. They intentionally have empty committed values. For
Docker Compose deployments, prefer `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID=<telegram-chat-id>` for the emergency owner.
`TELEGRAM_ALLOWED_CHAT_ID=<telegram-chat-id>` remains a compatibility fallback for older closed-beta deployments.
For direct ASP.NET configuration, use
`AssistantEngineer__EquipmentDiagnostics__Telegram__BootstrapOwnerChatId=<telegram-chat-id>`.

Telegram users, diagnostic history, and service requests are stored in the existing application PostgreSQL database
through the `TelegramUsers`, `TelegramDiagnosticCases`, and `TelegramServiceRequests` EF migrations.
No additional `.env` variable is required for ED-21B Russian UX, contact keyboard, technical response splitting, or
production SQL log suppression. Telegram command-menu sync can be disabled with
`TELEGRAM_COMMANDS_SYNC_ON_STARTUP=false`; by default it runs only when Telegram is enabled and `BotToken` is
configured. Rebuild the backend Docker image to pick up the GSSAPI runtime package.
Telegram diagnostic `CreatedAt` values stay UTC in the database; only `/history` and `/last` rendering uses
`TELEGRAM_DISPLAY_TIME_ZONE`.

ED-24MAN.1 / ED-24LIB.1 production Telegram file bindings and library access state are stored in the existing
application database through the `TelegramManualBindings`, `TelegramLibraryAccessGrants`, and
`TelegramLibraryAccessRequests` EF Core migrations. No new required `.env` value is needed. Owner can bind protected
series files through `/manual_bind`; Admin does not manage the library automatically. Store real Telegram manual
`file_id` values only in the database and never commit file IDs or manual binaries. The older JSON `FileBindingsPath`
remains a module-level fallback for non-production/manual test wiring, not the production source of truth.

ED-24OPS.2 operator inbox is off by default. To enable it, set `TELEGRAM_OPERATOR_INBOX_ENABLED=true` and
`TELEGRAM_OPERATOR_CHAT_ID=<group-chat-id>`. Owner can get the group id by sending `/operator_chat_id` or `/chatid`
inside the intended operator group; the command does not mutate environment settings. Owner replies in that group
must be Telegram replies to an operator inbox card or copied mirrored message. The bot sends only text replies back
to the original private user as `Ответ специалиста:` and records the thread/message history in
`TelegramOperatorInboxThreads` and `TelegramOperatorInboxMessages` through migration `AddTelegramOperatorInbox`.
Successful diagnostic answers, inline button clicks, manual/library file delivery, polling internals, `/start`,
`/history`, `/last`, and active `/manual_bind` uploads are not mirrored by default.

When `TELEGRAM_SERVICE_REQUESTS_CHAT_ID` is empty, users can still create service requests and the application logs
a sanitized warning. When configured, new requests are sent to that Telegram group without a full phone number,
raw chat id, or Telegram user id.

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
