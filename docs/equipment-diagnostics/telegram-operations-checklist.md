# Telegram Operations Checklist

ED-20A closed-beta preparation also requires the consolidated `closed-beta-release-checklist.md`. Keep Telegram transport and chat identifier discovery disabled by default.
ED-22B additionally requires the reviewed release candidate, operator limitation card, and manual smoke matrix.
ED-22C adds a secret-free deployment activation dry-run that must pass before any separately approved activation.
ED-22D adds the reviewed manual activation runbook, sanitized smoke evidence template, and local activation checklist generator.
ED-22E adds the final deterministic go/no-go evidence and placeholder-only handoff before any separately approved manual activation.
ED-22F adds the committed manual annotated-tag and release-handoff procedure; its local checklist never creates or pushes a tag.

## Access Policy

- Keep `IsEnabled=false` until the public HTTPS endpoint, secrets, bootstrap owner, and user access policy are reviewed.
- Use `InboundMode=Polling` and `Polling__Enabled=true` when production webhook delivery cannot reach the API.
- Keep webhook delivery as an optional fallback only; do not run webhook and polling at the same time.
- Configure `BootstrapOwnerChatId` before enabling production Telegram. Existing `AllowedChatIds__0` remains a
  compatibility bootstrap owner fallback, but the DB-backed `TelegramUsers` table is the primary access model.
- Use `DeniedChatIds` for emergency or explicit blocks. Deny wins over allow.
- Username allow/deny rules are optional; chat ID rules are preferred.
- Unknown Telegram users are auto-created as `Consumer`, `IsEnabled=true`, `IsBlocked=false`.
- Consumer receives Russian public-safe messages by default. Owner, Admin, and Engineer receive the more detailed
  Russian technical response.
- In `Production`, enabled Telegram without `BootstrapOwnerChatId` or legacy bootstrap fallback fails startup unless
  chat ID discovery is temporarily enabled for setup.

## Closed Beta Single-Operator Mode

- Use one explicit bootstrap owner entry: `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID=<telegram-chat-id>` for Docker Compose,
  or `AssistantEngineer__EquipmentDiagnostics__Telegram__BootstrapOwnerChatId=<telegram-chat-id>` for direct ASP.NET
  configuration.
- `TELEGRAM_ALLOWED_CHAT_ID=<telegram-chat-id>` remains accepted as compatibility fallback for the bootstrap owner.
- Keep `TELEGRAM_ALLOWED_USERNAME=` empty unless a reviewed username fallback is required.
- Keep `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false` after the chat ID is known.
- Verify an unknown Telegram account is created as `Consumer` and receives only the simplified public-safe response.
- Verify Consumer `/start` and `/help` are Russian, contain no `/admin` commands, and mention both phone options:
  sharing the Telegram contact number and manually entering another number for a callback.
- Verify the Telegram command menu contains only `/start`, `/new`, `/phone`, `/me`, `/help`, `/history`, and `/last`;
  it must not list `/admin_help` or parameterized admin commands such as `/admin users`, `/admin allow`,
  `/admin block`, or `/admin role`.
- Verify `/new` resets the current session and asks `Введите код ошибки, например: Gree H5.`; `/phone` opens the
  existing phone flow; `/admin_help` remains available by manual input or Owner/Admin `/help`, returns admin help
  only for Owner/Admin, and returns `Команда недоступна.` for Consumer and Engineer.
- Verify completed diagnostics create a private `TelegramDiagnosticCases` history record, not-found requests create a
  `NotFound` record, and intermediate brand/type/display prompts do not create records.
- Verify `/history` returns the latest five records for the current user only; `/last` returns only the current user's
  latest record; Owner/Admin do not get global history in ED-23A.
- Verify history output and stored records do not include phone numbers, chat IDs, Telegram user IDs, internal ids,
  token/secret values, full incoming text, or full bot response text.
- Verify Consumer diagnostic replies do not include confidence, source, internal traces, `Response shortened`,
  `deterministic bot API`, or unsafe board/compressor/inverter/refrigerant/high-voltage instructions.
- Verify the main Consumer keyboard without a saved phone shows `📞 Поделиться номером Telegram` and
  `✏️ Ввести другой номер`.
- Verify sharing a Telegram contact saves the phone number with source `TelegramContact` and verifies it only when
  Telegram `contact.user_id` matches `from.id`.
- Verify manual phone input accepts common formats such as `+998 90 123 45 67`, saves source `Manual`, keeps
  `PhoneNumberVerified=false`, and keeps invalid input in `WaitingForPhoneNumber`.
- Verify phone entry does not delete an active brand/type/display-context diagnostic session and restores the previous
  prompt when possible.
- Verify the bootstrap owner can use `/admin users`, `/admin block <chatId>`, `/admin unblock <chatId>`,
  `/admin disable <chatId>`, `/admin enable <chatId>`, and `/admin role <chatId> <Owner|Admin|Engineer|Consumer>`.

## Initial Chat ID Discovery

1. Create the BotFather token last and store it only in the deployment secret store.
2. Generate the webhook secret for this deployment and store it only in environment or secret-store configuration.
3. Temporarily set `EnableChatIdDiscovery=true` or `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=true`.
4. Deploy/restart and send `/id` or `/whoami`.
5. Add the returned `chatId` to `BootstrapOwnerChatId` in environment configuration.
6. Set `EnableChatIdDiscovery=false` or `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false` and deploy/restart again.

Discovery is disabled by default. Its response never includes the bot token, webhook secret, server paths, or diagnostic data.

## Production Readiness

- `BotToken` exists only in environment/secret-store configuration.
- Command-menu sync is either left at the safe default or explicitly disabled with
  `Commands__SyncOnStartup=false`; failures are warnings only and do not stop startup or polling.
- `WebhookSecret` exists only when webhook fallback is enabled.
- No real token or webhook secret is present in `appsettings`, source control, or generated artifacts.
- `IsEnabled=true` only in the reviewed production deployment.
- Polling production mode has `InboundMode=Polling`, `Polling__Enabled=true`, and `DeleteWebhookOnStartup=true`.
- Polling production mode has `ProcessedMessageStoreFilePath` configured on durable operational storage and
  `ProcessedMessageStoreMaxEntries` sized for the expected duplicate window.
- `BootstrapOwnerChatId` is configured, `TelegramUsers`, `TelegramConversationSessions`, `AddTelegramUserPhoneSource`,
  and `AddTelegramDiagnosticCases` migrations have been applied, and `DeniedChatIds` is reviewed.
- Unknown-user policy is `AutoConsumer`; Consumer help does not list admin commands.
- Phone sharing uses a Telegram reply keyboard with `request_contact=true`; manual phone input is available through
  `✏️ Ввести другой номер`; phone numbers are not logged, not printed in `/admin users`, and are not required for
  diagnostics.
- Diagnostic history stores `PhoneWasSaved` and `PhoneNumberSource` only; it does not store phone number values.
- Production logging keeps application, polling, and request operational messages while suppressing EF Core/Npgsql SQL
  command noise below Warning.
- The backend Docker image includes the GSSAPI runtime dependency required by Npgsql so `libgssapi_krb5.so.2` missing
  library warnings should not appear after image rebuild.
- `EnableChatIdDiscovery=false` after setup.
- Telegram webhook and long polling are not used together.
- Run `delete-telegram-webhook.ps1 -DropPendingUpdates`, then `get-telegram-webhook-info.ps1`.
- Confirm `getWebhookInfo` shows no webhook URL.
- Confirm `docker logs` for the API show `Telegram polling started`.
- Send `/start` and confirm polling/update logs appear without token, secret, chat ID, username, or message text.
- Send or replay duplicate Telegram updates only in a controlled smoke; one response should be sent for the same
  `chat.id + message_id`, and a sanitized `duplicate message skipped` log should appear.
- Send a deterministic smoke message and verify the expected reply.
- Review incident response; use `delete-telegram-webhook.ps1` when disabling delivery.
- For the ED-18A scaffold, replace the placeholder Caddy domain and verify public HTTPS before enabling Telegram.
- Run the ED-18B environment and scaffold validators before image build or Telegram activation.
- Record and test the rollback command before enabling the webhook.
- Confirm `/ready` stays healthy after reviewed Telegram activation; readiness never returns token, secret, chat ID, or message values.
- Use a safe `X-Correlation-ID` when reproducing webhook delivery issues; logs never include Telegram message text or chat ID values.
- Follow the Telegram webhook incident runbook and attach only sanitized log excerpts.

## Remaining Risks

- No audit log.
- No web admin UI.
- No diagnostic case history, CRM lead assignment, photo/OCR, or ServiceLead workflow.
- Polling offset and processed-message idempotency persistence are file-based unless deployment mounts a durable
  volume or overrides the paths.
- No endpoint-specific rate limiting beyond the broader API setup.

## Optional Webhook Fallback

- Domain and public HTTPS endpoint are ready.
- Telegram-supported webhook port is used: `443`, `80`, `88`, or `8443`.
- `WebhookSecret` exists only in environment/secret-store configuration.
- Run `set-telegram-webhook.ps1`, then `get-telegram-webhook-info.ps1`.
