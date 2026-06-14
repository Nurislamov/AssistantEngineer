# Incident Report Template

## Summary

- Title:
- Start time UTC:
- Environment:
- Affected component:
- Correlation IDs:
- Observed symptoms:

## Operational State

- `/health` status:
- `/ready` status:
- Telegram webhook counters summary without chat IDs:
- Commands run:
- Sanitized log excerpt:

## Response

- Suspected cause:
- Mitigation:
- Rollback decision:
- Follow-up actions:

## Sensitive Fields Explicitly Excluded

Do not include BotToken, WebhookSecret, Authorization or Telegram secret-header values, chat IDs, Telegram message
body, raw request/response bodies, environment-file contents, or internal artifact paths. Store local sanitized
incident artifacts only under ignored `artifacts/operations/`.
