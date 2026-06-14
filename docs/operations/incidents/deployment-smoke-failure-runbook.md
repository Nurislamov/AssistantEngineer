# Deployment Smoke Failure Runbook

## Reproduce Safely

1. Run `.\scripts\deployment\validate-deployment-scaffold.ps1`.
2. Run `.\scripts\deployment\validate-production-env.ps1 -EnvPath deploy/.env.example -AllowPlaceholders`.
3. Run `.\scripts\deployment\run-ci-deployment-dry-run.ps1`.
4. Run `.\scripts\deployment\smoke-production-stack.ps1`; record its generated `X-Correlation-ID`.
5. Inspect `docker compose -f deploy/docker-compose.yml ps`.
6. Collect sanitized logs with `collect-sanitized-logs.ps1`; never attach raw Docker logs.

Classify whether the failure is frontend reachability, `/health`, `/ready`, deterministic bot response, correlation
echo, or the expected Telegram-disabled boundary. Do not change Telegram enablement or chat ID discovery merely to
make smoke pass.

Do not expose secrets, chat IDs, Telegram message bodies, raw request/response bodies, environment files, or
internal artifact paths. Use the [rollback checklist](../../deployment/rollback-checklist.md) when the current
reviewed image cannot pass smoke.

There is no external monitoring, provider automation, or audit persistence yet.
