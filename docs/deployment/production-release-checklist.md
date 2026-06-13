# Production Release Checklist

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
- Keep Telegram webhook transport and chat ID discovery disabled during initial stack verification.
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

## Final Telegram Activation

- Create the BotFather token only at the final approved activation step and store it outside Git.
- Generate a new webhook secret using the documented character and length rules.
- Configure a non-empty `AllowedChatIds` list.
- Temporarily enable chat ID discovery only when required to identify an approved chat.
- Disable chat ID discovery immediately after the allowlist is configured.
- Explicitly enable the webhook transport only after all preceding checks pass.
- Run `set-telegram-webhook.ps1`, then `get-telegram-webhook-info.ps1`.
- Send one deterministic Telegram smoke message and verify the bounded response.

## Closeout

- Record the deployed image tag or digest and the rollback command.
- Confirm `deploy/.env`, generated artifacts, secrets, and PDF/manual files remain uncommitted.
- Review the current limitations in `logging-monitoring-backup-notes.md`.
