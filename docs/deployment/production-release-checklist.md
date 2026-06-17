# Production Release Checklist

EquipmentDiagnostics ED-20A does not satisfy this production checklist. Its separate `docs/equipment-diagnostics/closed-beta-release-checklist.md` is limited to a controlled closed beta.
The ED-22C Telegram deployment activation dry-run is additional closed-beta pre-activation evidence; it does not satisfy or alter this production checklist.

This checklist prepares a reviewed production deployment. It does not perform a deployment or select a provider.

## Infrastructure

- Choose a VPS provider and supported Linux image.
- Buy or assign the production domain.
- Point reviewed DNS `A` and, when used, `AAAA` records to the server.
- Open ports `80` and `443`; keep other public ports closed unless reviewed.
- Install Docker Engine and the Docker Compose plugin.
- Replace only the documented `example.com` reverse-proxy placeholder.
- Confirm the reverse proxy can obtain and renew public HTTPS certificates.

## Configuration

- Copy `deploy/.env.example` to ignored `deploy/.env`; never commit secrets to Git.
- Keep Telegram transport and chat ID discovery disabled during initial stack verification.
- Run `.\scripts\deployment\validate-production-env.ps1`.
- Run `.\scripts\deployment\validate-deployment-scaffold.ps1`.
- When Docker is available, run the scaffold validator with `-RunDockerComposeConfig`.
- Run `run-ci-deployment-dry-run.ps1 -RequireDocker -BuildImages` or confirm its matching CI workflow passed.

## Release

- Build images with `build-production-images.ps1`.
- Start the stack with `start-production-stack.ps1`.
- Run `smoke-production-stack.ps1` against the reviewed frontend and API URLs.
- Verify HTTPS, frontend routing, `/health`, `/ready`, and the deterministic EquipmentDiagnostics bot response.
- Record the non-secret `X-Correlation-ID` printed by deployment smoke when troubleshooting a failed check.
- Confirm operators can follow the incident runbooks and write only sanitized logs under ignored `artifacts/operations/`.

## Final Telegram Activation

- Create the BotFather token only at the final approved activation step and store it outside Git.
- Use polling mode for providers where Telegram inbound HTTPS traffic times out.
- Keep the polling offset and processed-message idempotency files on durable operational storage; the deployment
  scaffold uses the `api_operations` named volume.
- Generate a new webhook secret using the documented character and length rules only when webhook fallback is used.
- Configure `BootstrapOwnerChatId`; legacy `AllowedChatIds__0` may remain only as bootstrap compatibility fallback.
- Apply the `TelegramUsers` EF migration and confirm the user store is available.
- Confirm unknown Telegram users are auto-created as `Consumer`, not Engineer/Admin.
- Temporarily enable chat ID discovery only when required to identify an approved chat.
- Disable chat ID discovery immediately after the allowlist is configured.
- Verify an unknown Telegram account is ignored while the allowed chat can run `/start` and one deterministic smoke
  diagnostic.
- Explicitly enable the reviewed Telegram transport only after all preceding checks pass.
- For polling mode, run `delete-telegram-webhook.ps1 -DropPendingUpdates`, then `get-telegram-webhook-info.ps1`;
  verify no webhook URL is configured and API logs show `Telegram polling started`.
- Verify a replayed duplicate update for the same Telegram `chat.id + message_id` does not send a second response.
- For webhook fallback, run `set-telegram-webhook.ps1`, then `get-telegram-webhook-info.ps1`.
- Send one deterministic Telegram smoke message and verify the bounded response.
- From the bootstrap owner, verify `/admin users`, role promotion, block/unblock, disable/enable, and Consumer help
  hiding admin commands.

## Closeout

- Record the deployed image tag or digest and the rollback command.
- Confirm `deploy/.env`, generated artifacts, secrets, and PDF/manual files remain uncommitted.
- Review the current limitations in `logging-monitoring-backup-notes.md`.
