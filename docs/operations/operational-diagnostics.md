# Operational Diagnostics

The internal operational diagnostics snapshot contains safe process metadata, configuration booleans, and
in-memory Telegram webhook counters. It is consumed by readiness checks and tests; ED-19A does not expose a broad
public diagnostics endpoint.

Safe snapshot fields include application/environment/version, start time, uptime, bot-facade availability,
Telegram inbound mode, enabled/configured/discovery booleans, allow/deny-list configured booleans, and counters
for received, processed, ignored, secret-rejected, unauthorized, invalid, and outbound-failed updates.

The snapshot intentionally does not contain:

- bot token or webhook secret;
- allowed or denied chat ID values;
- usernames, chat IDs, or message bodies;
- Authorization or Telegram secret headers;
- file-system, staging, codebook, preview, or generated-artifact paths.

Counters are thread-safe and in-process only. They reset when the process restarts and are not an audit log,
durable metric store, or delivery guarantee. New diagnostic/error paths should use `OperationalSecretRedactor`
before emitting text that may contain token-like values, sensitive headers, query secrets, or connection-string
passwords.

ED-19B adds capability-only fields `correlationEnabled` and `correlationHeaderName`. The global snapshot never
contains a current request correlation ID. Correlation remains request-scoped and is not an audit record.

During incidents, summarize counters and configuration-presence booleans only. Use the incident runbooks and
sanitized log collector; never record actual chat IDs, message bodies, or secret values.
