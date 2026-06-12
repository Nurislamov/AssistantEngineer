# Provider-Neutral Deployment Scaffold

## Purpose

ED-18A provides an example-only deployment scaffold for a future VPS using Docker Compose and a reverse proxy.
It is not a production deployment, hosting-provider selection, domain purchase, or security certification.

The scaffold contains:

- a multi-stage .NET 10 API image;
- a multi-stage Node/Vite frontend image served by nginx;
- a Caddy reverse-proxy example for same-domain `/api/*` routing and future HTTPS;
- a Docker Compose file with no database service;
- placeholder-only environment configuration and local operator scripts.

## Build And Run

Create a local ignored environment file:

```powershell
Copy-Item deploy/.env.example deploy/.env
```

Review the placeholders, then:

```powershell
.\scripts\deployment\validate-production-env.ps1 -EnvPath deploy/.env
.\scripts\deployment\validate-deployment-scaffold.ps1
.\scripts\deployment\build-production-images.ps1
.\scripts\deployment\start-production-stack.ps1
.\scripts\deployment\smoke-production-stack.ps1
.\scripts\deployment\stop-production-stack.ps1
```

The scaffold validator is static by default. Use `-RunDockerComposeConfig` only when the Docker Compose plugin is
available; it does not require a running Docker daemon.

The Caddy example uses `example.com`; replace it only during a reviewed deployment after the real domain and DNS
are ready. The direct local API and frontend ports exist for scaffold smoke checks.

## Frontend API Strategy

The frontend image uses the existing build-time `VITE_API_BASE_URL` setting. The compose default is
`http://localhost/`, matching same-domain `/api/*` routing through Caddy. It is not a secret.

## Future Decisions

- VPS provider and operating-system hardening;
- domain, DNS, firewall, and HTTPS issuance;
- BotFather token, webhook secret, and allowed/denied chat policy;
- backup and restore strategy;
- centralized logs, monitoring, and alerting;
- CI/CD deployment pipeline and rollback policy.

## Known Limitations

- no production deployment or provider-specific hardening;
- no database service or durable deployment data design;
- no audit log, backup strategy, or monitoring/alerting;
- no CI/CD deployment pipeline;
- no real secrets, domains, certificates, or Telegram enablement in Git.

Before a reviewed release, follow [production-release-checklist.md](production-release-checklist.md) and record a
rollback path using [rollback-checklist.md](rollback-checklist.md). Current logging, monitoring, and backup
non-claims are documented in [logging-monitoring-backup-notes.md](logging-monitoring-backup-notes.md).
