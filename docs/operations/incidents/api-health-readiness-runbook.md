# API Health And Readiness Incident Runbook

## Meaning

The implemented live-process endpoint is `GET /health`; there is no separate `/health/live` alias. A successful
response means the API process and liveness checks respond. `GET /ready` means registered readiness checks pass,
including safe EquipmentDiagnostics and Telegram configuration checks. Neither endpoint proves external delivery,
DNS, provider health, or complete production readiness.

## First Response

1. Generate a safe correlation ID such as `incident-health-<compact-id>`.
2. Call `/health` and `/ready` with `X-Correlation-ID`; record status codes and the echoed ID.
3. Run `docker compose -f deploy/docker-compose.yml ps`.
4. Collect only sanitized logs:
   `.\scripts\operations\collect-sanitized-logs.ps1 -IncludeDockerComposeLogs -ServiceName assistantengineer-api -CorrelationId <id>`.
5. Check process restarts, readiness failures, configuration-presence booleans, and Telegram counters without
   recording secret or chat ID values.

Do not expose Authorization headers, Telegram tokens or webhook secrets, chat IDs, Telegram message bodies, raw
request/response bodies, or internal artifact paths.

If `/health` fails, escalate to deployment rollback review. If `/health` passes but `/ready` fails, review safe
configuration and dependent readiness checks before rollback. Follow the provider-neutral
[rollback checklist](../../deployment/rollback-checklist.md).

There is no external monitoring, alerting, audit persistence, or durable operational counter storage yet.
