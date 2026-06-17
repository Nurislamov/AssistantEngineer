# Request Correlation And Logging

ED-19B adds provider-neutral request correlation through `X-Correlation-ID`. A valid caller-supplied value uses
letters, digits, `_`, `-`, or `.`, and is at most 128 characters. Missing or invalid values are replaced with a
compact random ID. The API returns the accepted ID in the response header for `/health`, `/ready`, API routes, and
the Telegram webhook.

The request middleware creates a structured logging scope containing only `correlationId`, HTTP method, request
path without query-string values, completion status code, and elapsed milliseconds.

The Telegram outbound client propagates the safe correlation header for internal troubleshooting and logs only
that a send was attempted plus a `chatIdPresent` boolean. The correlation ID is not included in the bot's
user-facing response.

Application logs intentionally never include Authorization or Telegram secret headers, bot tokens, webhook
secrets, allow/deny chat ID values, Telegram message text, raw request/response bodies, query-string values, or
internal artifact paths. Allow/deny username values and phone numbers are treated as sensitive too.

Operators can pass one safe correlation ID during smoke or incident reproduction and search the local application
logs for that ID. `smoke-production-stack.ps1` generates and prints a non-secret smoke ID and verifies the API
echoes it.

This foundation does not add Seq, ELK, Grafana, Prometheus, OpenTelemetry exporters, an external log sink, log
retention, alerting, or audit persistence. ED-19C may add reviewed structured JSON console logs, deploy-side
retention, metrics export, and alerting after the hosting provider is chosen.

For incident review, follow [correlation ID troubleshooting](incidents/correlation-id-troubleshooting.md) and use
`scripts/operations/collect-sanitized-logs.ps1`. Generated sanitized excerpts remain local under ignored
`artifacts/operations/`.
