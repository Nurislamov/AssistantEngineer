# Telegram Webhook Incident Runbook

## Triage

1. Confirm webhook transport enablement and chat ID discovery state as booleans only.
2. Confirm `X-Telegram-Bot-Api-Secret-Token` is configured and handled without printing or recording its value.
3. Confirm allowed/denied chat policy is configured without copying actual chat IDs into reports.
4. Review in-memory webhook counters for received, processed, ignored, rejected, invalid, and outbound-failed
   updates. Counters reset on restart and are not an audit log.
5. Use `get-telegram-webhook-info.ps1` to inspect provider state without printing the bot token.
6. Use a safe `X-Correlation-ID` and collect sanitized API logs for the affected request.

Use `set-telegram-webhook.ps1` or `delete-telegram-webhook.ps1` only during an approved corrective action. Temporary
`/id` or `/whoami` discovery may be used for initial setup only; set `EnableChatIdDiscovery=false` immediately
after the approved chat is configured.

Never paste BotToken, WebhookSecret, secret-header values, actual allowed/denied chat IDs, usernames, or Telegram
message bodies into tickets, log excerpts, or incident reports. Do not attach raw request/response bodies.

If outbound failures persist, keep deterministic bot behavior and access policy unchanged, remove webhook delivery
when necessary, and follow the rollback checklist. There is no external monitoring or audit persistence yet.
