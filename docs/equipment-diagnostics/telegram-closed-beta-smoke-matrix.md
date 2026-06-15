# Telegram Closed Beta Smoke Matrix

Use this manual matrix only during a separately approved closed-beta activation rehearsal. Use placeholders and sanitized evidence; never place real credentials, domains, or chat IDs in this document.
Review a passing ED-22C deployment activation dry-run before starting this manual matrix.
Follow the ED-22D activation runbook and record only sanitized results in the ED-22D smoke evidence template.

| ID | Purpose | Setup | Input / action | Expected result | Evidence to collect | Pass / fail |
|---|---|---|---|---|---|---|
| SMK-API-HEALTH | API health/readiness | Local reviewed stack | Check `/health` and `/ready` | Healthy/readiness response without sensitive values | Sanitized status summary | [ ] |
| SMK-TG-DISABLED | Telegram disabled | Default configuration | Submit no webhook request | Transport remains disabled | Configuration validation result | [ ] |
| SMK-SECRET-INVALID | Invalid webhook secret | Reviewed test configuration | Send request with invalid placeholder secret | Request rejected without secret exposure | Sanitized status/correlation reference | [ ] |
| SMK-CHAT-ALLOW | Allowed chat | Placeholder allowed-chat fixture | Send deterministic known request | Allowed request follows deterministic path | Sanitized result summary | [ ] |
| SMK-CHAT-DENY | Denied chat | Placeholder denied-chat fixture | Send deterministic request | Request denied; deny wins | Sanitized result summary | [ ] |
| SMK-DISCOVERY-ON | Chat ID discovery enabled | Temporarily enable only for approved setup | Request identity discovery | Identity response is controlled and contains no credentials | Sanitized setup note | [ ] |
| SMK-DISCOVERY-OFF | Chat ID discovery disabled | Disable after setup | Repeat identity discovery | Discovery response is unavailable | Configuration validation result | [ ] |
| SMK-START | Start command | Approved test chat fixture | Send `/start` | Deterministic welcome/help boundary | Sanitized response status | [ ] |
| SMK-HELP | Help command | Approved test chat fixture | Send `/help` | Deterministic help boundary | Sanitized response status | [ ] |
| SMK-CODE-KNOWN | Known code | Runtime-covered fixture | Send known diagnostic request | Runtime answer with safety/provenance/verification | Sanitized scenario result | [ ] |
| SMK-CODE-AMBIGUOUS | Ambiguous code | Ambiguous fixture | Send request without enough context | Clarification required; no forced answer | Sanitized scenario result | [ ] |
| SMK-CODE-UNKNOWN | Unknown code | Unsupported fixture | Send unknown code | Safe not-found/unsupported response | Sanitized scenario result | [ ] |
| SMK-MESSAGE-LONG | Too long message | Approved test chat fixture | Send over-limit placeholder text | Controlled rejection; no echo of message body | Sanitized validation result | [ ] |
| SMK-TEXT-UNSUPPORTED | Unsupported text | Approved test chat fixture | Send unsupported placeholder text | Controlled unsupported response | Sanitized response status | [ ] |
| SMK-OUTBOUND-FAIL | Outbound failure | Deterministic failing outbound fixture | Trigger response send failure | Failure recorded without secret/message/chat exposure | Sanitized counter/correlation evidence | [ ] |
| SMK-LOGS-SANITIZED | Sanitized logs | Local fixture only | Run sanitized log collection | Output contains no credentials, chat IDs, or message body | Sanitized artifact review note | [ ] |
| SMK-ROLLBACK | Rollback | Reviewed activation rehearsal | Delete webhook, disable transport, restart, check readiness | Delivery stops and readiness remains healthy | Sanitized rollback checklist | [ ] |

## Shared Boundaries

- Closed beta only; not production or public release.
- No real secrets in Git; generated smoke evidence is not committed.
- Telegram is disabled by default and chat ID discovery is disabled by default.
- No long polling, database/audit persistence, or external monitoring.
- Runtime catalog is the only final-answer source.
- Manual-codebook, staging, and preview are not final diagnosis.
- Vendor manual coverage remains partial; no completeness claim is made.
