# Runtime Observability Foundation

## ED-19A Scope

ED-19A aligns the existing anonymous operational endpoints with deployment smoke checks:

- `GET /health` confirms the API process is alive.
- `GET /ready` confirms registered readiness checks pass, the EquipmentDiagnostics bot facade resolves, and
  the selected Telegram inbound transport configuration is safe.

Telegram being disabled is a healthy default. When Telegram webhook mode is enabled, readiness requires a valid
webhook secret, configured bot token, configured bootstrap owner access, an available Telegram user store, and chat
ID discovery disabled. When Telegram polling mode is enabled, readiness requires a configured bot token, configured
bootstrap owner access, an available Telegram user store, and chat ID discovery disabled. Unknown user policy is
reported only as `AutoConsumer`.

Health responses remain compact and do not expose configuration values, internal paths, exception stacks, manual
or staging artifacts, chat IDs, usernames, phone numbers, message bodies, bot tokens, or webhook secrets. Readiness proves only that current
in-process checks pass; it does not prove external Telegram delivery, DNS, HTTPS, provider health, or production
traffic readiness.

ED-21B keeps the same sanitized observability surface while polishing Telegram runtime behavior: Consumer replies are
Russian and public-safe, contact sharing is handled through Telegram reply markup, technical replies may be split
across ordered outbound messages, and production SQL command text from EF Core/Npgsql is suppressed below Warning.
The backend Docker image installs `libgssapi-krb5-2` to avoid Npgsql GSSAPI missing-library noise.

## Non-Claims

- No Prometheus, Seq, ELK, Grafana, or external monitoring service is implemented.
- No alerting, audit log, database persistence, or durable metrics storage is implemented.
- No provider-specific monitoring or cloud infrastructure is added.

ED-19B adds safe request correlation and structured application log scopes without an external log sink. Future
work may add reviewed JSON console formatting, metrics export, alerting, and audit/persistence only after the
hosting and database decisions.

ED-19C adds operator incident runbooks and sanitized local log-review scripts. These workflows do not add external
monitoring, durable incident storage, or an audit trail.
