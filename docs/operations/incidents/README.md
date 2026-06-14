# Incident Runbooks

Use these runbooks for deterministic first response:

- [API health and readiness](api-health-readiness-runbook.md);
- [Telegram webhook](telegram-webhook-runbook.md);
- [deployment smoke failure](deployment-smoke-failure-runbook.md);
- [correlation ID troubleshooting](correlation-id-troubleshooting.md);
- [incident report template](incident-report-template.md).

Never paste secrets, chat IDs, Telegram message bodies, raw request/response bodies, or internal artifact paths
into incident reports. Store local sanitized excerpts only under ignored `artifacts/operations/`.
