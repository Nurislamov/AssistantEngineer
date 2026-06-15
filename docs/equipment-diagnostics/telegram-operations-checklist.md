# Telegram Operations Checklist

ED-20A closed-beta preparation also requires the consolidated `closed-beta-release-checklist.md`. Keep Telegram transport and chat identifier discovery disabled by default.
ED-22B additionally requires the reviewed release candidate, operator limitation card, and manual smoke matrix.
ED-22C adds a secret-free deployment activation dry-run that must pass before any separately approved activation.
ED-22D adds the reviewed manual activation runbook, sanitized smoke evidence template, and local activation checklist generator.

## Access Policy

- Keep `IsEnabled=false` until the public HTTPS endpoint, secrets, and access lists are reviewed.
- Configure `AllowedChatIds` through environment configuration.
- Use `DeniedChatIds` for emergency or explicit blocks. Deny wins over allow.
- Username allow/deny rules are optional; chat ID rules are preferred.
- Empty allow and deny lists preserve the current permissive adapter behavior, so production must configure an allowlist.

## Initial Chat ID Discovery

1. Create the BotFather token last and store it only in the deployment secret store.
2. Generate the webhook secret for this deployment and store it only in environment or secret-store configuration.
3. Temporarily set `EnableChatIdDiscovery=true`.
4. Deploy/restart and send `/id` or `/whoami`.
5. Add the returned `chatId` to `AllowedChatIds` in environment configuration.
6. Set `EnableChatIdDiscovery=false` and deploy/restart again.

Discovery is disabled by default. Its response never includes the bot token, webhook secret, server paths, or diagnostic data.

## Production Readiness

- Domain and public HTTPS endpoint are ready.
- Telegram-supported webhook port is used: `443`, `80`, `88`, or `8443`.
- `BotToken` and `WebhookSecret` exist only in environment/secret-store configuration.
- No real token or webhook secret is present in `appsettings`, source control, or generated artifacts.
- `IsEnabled=true` only in the reviewed production deployment.
- `AllowedChatIds` is non-empty and `DeniedChatIds` is reviewed.
- `EnableChatIdDiscovery=false` after setup.
- Telegram webhook and long polling are not used together.
- Run `set-telegram-webhook.ps1`, then `get-telegram-webhook-info.ps1`.
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
- No database persistence.
- No endpoint-specific rate limiting beyond the broader API setup.
