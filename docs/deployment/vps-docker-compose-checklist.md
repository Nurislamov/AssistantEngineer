# VPS Docker Compose Checklist

## Before Deployment

- Select a VPS provider and supported Linux image.
- Point reviewed DNS records to the VPS.
- Open only required ports, including `80` and `443`.
- Replace placeholder `example.com` in the Caddy example.
- Verify HTTPS issuance and renewal.
- Create `deploy/.env` locally from `.env.example`; never commit it.
- Configure Telegram secrets only through the deployment secret/environment mechanism.
- Keep Telegram `IsEnabled=false` until HTTPS, secrets, and access policy are ready.
- Use `EnableChatIdDiscovery=true` temporarily only for initial `/id` setup.
- Configure `AllowedChatIds`; review `DeniedChatIds` and deny-wins-over-allow behavior.

## Verification

- Build and start the stack with the deployment scripts.
- Run `smoke-production-stack.ps1`.
- Confirm frontend, `/health`, and the deterministic bot endpoint are reachable.
- Confirm the Telegram webhook is disabled until explicitly approved.
- When Telegram is enabled later, run `set-telegram-webhook.ps1` and `get-telegram-webhook-info.ps1`.
- Use `delete-telegram-webhook.ps1` during disablement or incident response.

## Still Required Later

- backup and restore plan;
- log retention and audit strategy;
- monitoring and alerting;
- CI/CD and rollback process;
- provider-specific firewall, SSH, patching, and secret-store hardening.
