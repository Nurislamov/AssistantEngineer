# Telegram Operations Checklist

ED-20A closed-beta preparation also requires the consolidated `closed-beta-release-checklist.md`. Keep Telegram transport and chat identifier discovery disabled by default.
ED-22B additionally requires the reviewed release candidate, operator limitation card, and manual smoke matrix.
ED-22C adds a secret-free deployment activation dry-run that must pass before any separately approved activation.
ED-22D adds the reviewed manual activation runbook, sanitized smoke evidence template, and local activation checklist generator.
ED-22E adds the final deterministic go/no-go evidence and placeholder-only handoff before any separately approved manual activation.
ED-22F adds the committed manual annotated-tag and release-handoff procedure; its local checklist never creates or pushes a tag.

## Access Policy

- Keep `IsEnabled=false` until the public HTTPS endpoint, secrets, and access lists are reviewed.
- Use `InboundMode=Polling` and `Polling__Enabled=true` when production webhook delivery cannot reach the API.
- Keep webhook delivery as an optional fallback only; do not run webhook and polling at the same time.
- Configure `AllowedChatIds` or `AllowedUsernames` through environment configuration before enabling production
  Telegram. Chat ID allowlisting is preferred for closed beta.
- Use `DeniedChatIds` for emergency or explicit blocks. Deny wins over allow.
- Username allow/deny rules are optional; chat ID rules are preferred.
- Empty allowlists are deny-by-default. In `Production`, enabled Telegram without an allowlist fails startup unless
  chat ID discovery is temporarily enabled for initial setup.

## Closed Beta Single-Operator Mode

- Use one explicit allowlist entry: `TELEGRAM_ALLOWED_CHAT_ID=<telegram-chat-id>` for Docker Compose, or
  `AssistantEngineer__EquipmentDiagnostics__Telegram__AllowedChatIds__0=<telegram-chat-id>` for direct ASP.NET
  configuration.
- Keep `TELEGRAM_ALLOWED_USERNAME=` empty unless a reviewed username fallback is required.
- Keep `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false` after the chat ID is known.
- Verify an unknown Telegram account gets no diagnostic response while the allowed chat can use `/start` and a
  deterministic diagnostic smoke message.

## Initial Chat ID Discovery

1. Create the BotFather token last and store it only in the deployment secret store.
2. Generate the webhook secret for this deployment and store it only in environment or secret-store configuration.
3. Temporarily set `EnableChatIdDiscovery=true` or `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=true`.
4. Deploy/restart and send `/id` or `/whoami`.
5. Add the returned `chatId` to `AllowedChatIds` in environment configuration.
6. Set `EnableChatIdDiscovery=false` or `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false` and deploy/restart again.

Discovery is disabled by default. Its response never includes the bot token, webhook secret, server paths, or diagnostic data.

## Production Readiness

- `BotToken` exists only in environment/secret-store configuration.
- `WebhookSecret` exists only when webhook fallback is enabled.
- No real token or webhook secret is present in `appsettings`, source control, or generated artifacts.
- `IsEnabled=true` only in the reviewed production deployment.
- Polling production mode has `InboundMode=Polling`, `Polling__Enabled=true`, and `DeleteWebhookOnStartup=true`.
- Polling production mode has `ProcessedMessageStoreFilePath` configured on durable operational storage and
  `ProcessedMessageStoreMaxEntries` sized for the expected duplicate window.
- `AllowedChatIds` or `AllowedUsernames` is non-empty and `DeniedChatIds` is reviewed.
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
- No admin UI for allow/deny lists.
- Polling offset and processed-message idempotency persistence are file-based unless deployment mounts a durable
  volume or overrides the paths.
- No endpoint-specific rate limiting beyond the broader API setup.

## Optional Webhook Fallback

- Domain and public HTTPS endpoint are ready.
- Telegram-supported webhook port is used: `443`, `80`, `88`, or `8443`.
- `WebhookSecret` exists only in environment/secret-store configuration.
- Run `set-telegram-webhook.ps1`, then `get-telegram-webhook-info.ps1`.
