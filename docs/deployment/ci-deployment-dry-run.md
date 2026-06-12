# CI Deployment Dry Run

## Purpose

`deployment-dry-run.yml` validates the provider-neutral deployment scaffold on relevant pull requests and by
manual dispatch. It is readiness verification only and does not perform a production deployment.

The workflow:

- runs the static deployment scaffold validator;
- validates `deploy/.env.example` with placeholders allowed;
- validates Docker Compose configuration using the placeholder environment;
- builds backend and frontend images locally with CI-only tags;
- does not push images or log in to a registry;
- uses no real secrets, domains, or provider-specific infrastructure;
- keeps Telegram webhook transport and chat ID discovery disabled.

There is no deploy job, SSH/SCP step, cloud-specific infrastructure, registry push, or production environment.

## Local Equivalent

Run static validation and Compose config when available:

```powershell
.\scripts\deployment\run-ci-deployment-dry-run.ps1
```

Require a working Docker daemon and build both images:

```powershell
.\scripts\deployment\run-ci-deployment-dry-run.ps1 -RequireDocker -BuildImages
```

Without a Docker daemon, static validation still passes and image builds are reported as skipped. No secret value
is printed.

## Still Required Before Real Deployment

- choose a VPS provider;
- buy or assign a domain;
- configure DNS and public HTTPS;
- create the real ignored `.env` on the server;
- create the BotFather bot token at the final activation step;
- generate the webhook secret;
- configure `AllowedChatIds`;
- explicitly enable Telegram and run `setWebhook`;
- run a reviewed production smoke test and record rollback details.
