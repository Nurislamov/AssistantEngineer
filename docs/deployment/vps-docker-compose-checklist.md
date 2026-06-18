# VPS Docker Compose Checklist

## Before Deployment

- Select a VPS provider and supported Linux image.
- Point reviewed DNS records to the VPS.
- Open only required ports, including `80` and `443`.
- Replace placeholder `example.com` in the Caddy example.
- Verify HTTPS issuance and renewal.
- Create `deploy/.env` locally from `.env.example`; never commit it.
- Run `validate-production-env.ps1` and `validate-deployment-scaffold.ps1`.
- Run `validate-deployment-scaffold.ps1 -RunDockerComposeConfig` when Docker Compose is available.
- Confirm the provider-neutral CI deployment dry-run workflow passed; it validates and builds but does not deploy.
- Configure Telegram secrets only through the deployment secret/environment mechanism.
- Keep Telegram `IsEnabled=false` until secrets and access policy are ready.
- For polling production mode, set `TELEGRAM_INBOUND_MODE=Polling`, `TELEGRAM_POLLING_ENABLED=true`, and
  `TELEGRAM_DELETE_WEBHOOK_ON_STARTUP=true`.
- Use `EnableChatIdDiscovery=true` temporarily only for initial `/id` setup.
- Configure `TELEGRAM_BOOTSTRAP_OWNER_CHAT_ID=<telegram-chat-id>` or direct `BootstrapOwnerChatId` binding; legacy
  `TELEGRAM_ALLOWED_CHAT_ID` remains only a bootstrap compatibility fallback.
- Apply the Telegram user, conversation, phone, diagnostic history, `AddTelegramServiceRequests`, and
  `AddTelegramServiceRequestAssignments` migrations before enabling Telegram.
- Optionally configure `TELEGRAM_SERVICE_REQUESTS_CHAT_ID` for the service group; leave it empty to create requests
  without group notifications.
- Rebuild the backend image after ED-21B so the runtime includes `libgssapi-krb5-2`.
- Review `DeniedChatIds` and deny-wins-over-allow behavior.
- Confirm each Engineer has opened the bot privately with `/start` and received the reviewed `Engineer` role before
  using queue commands in the service group.
- Keep the `api_operations` named volume unless a reviewed host path replaces it; it stores Telegram polling offset
  and processed-message idempotency files without secrets.

## Verification

- Build and start the stack with the deployment scripts.
- Run `smoke-production-stack.ps1`.
- Confirm frontend, `/health`, and the deterministic bot endpoint are reachable.
- Confirm Telegram delivery is disabled until explicitly approved.
- For polling mode, run `delete-telegram-webhook.ps1 -DropPendingUpdates`, then `get-telegram-webhook-info.ps1`;
  webhook URL should be empty.
- Confirm duplicate Telegram updates for one message produce one bot response and a sanitized duplicate-skip log.
- Confirm Consumer `/start`, `/help`, `/me`, code-first diagnostic conversation, contact sharing, and diagnostic
  replies are Russian and public-safe.
- Confirm `🔎 Новый код`, `/new`, `/reset`, and `/cancel` reset the active conversation without disabling polling,
  dedupe, TelegramUsers, roles, or contact flow.
- Confirm production logs do not show EF/Npgsql SQL `SELECT`, `INSERT`, or `UPDATE` command text at Information level.
- For webhook fallback, run `set-telegram-webhook.ps1` and `get-telegram-webhook-info.ps1`.
- Use `delete-telegram-webhook.ps1` during disablement or incident response.
- Record the previous image tag/digest and reviewed rollback command before activation.

## Still Required Later

- backup and restore plan;
- log retention and audit strategy;
- monitoring and alerting;
- CI/CD and rollback process;
- provider-specific firewall, SSH, patching, and secret-store hardening.
