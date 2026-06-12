# Runtime Observability Foundation

## ED-19A Scope

ED-19A aligns the existing anonymous operational endpoints with deployment smoke checks:

- `GET /health` confirms the API process is alive.
- `GET /ready` confirms registered readiness checks pass, the EquipmentDiagnostics bot facade resolves, and
  Telegram webhook configuration is safe.

Telegram being disabled is a healthy default. When Telegram is enabled, readiness requires a valid webhook secret,
configured bot token, non-empty allowed chat IDs, and chat ID discovery disabled.

Health responses remain compact and do not expose configuration values, internal paths, exception stacks, manual
or staging artifacts, chat IDs, message bodies, bot tokens, or webhook secrets. Readiness proves only that current
in-process checks pass; it does not prove external Telegram delivery, DNS, HTTPS, provider health, or production
traffic readiness.

## Non-Claims

- No Prometheus, Seq, ELK, Grafana, or external monitoring service is implemented.
- No alerting, audit log, database persistence, or durable metrics storage is implemented.
- No provider-specific monitoring or cloud infrastructure is added.

Future ED-19B may add a reviewed structured logging provider, request correlation, metrics export, alerting, and
audit/persistence only after the database decision.
