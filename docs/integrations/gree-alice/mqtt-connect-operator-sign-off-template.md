# GREE-ALICE-25: CONNECT-only operator sign-off template

## Purpose

This document is a template for a future human/operator sign-off packet. It is not an approval and it does not allow live MQTT CONNECT.

The template exists to make a later safety review auditable without committing raw credentials, packet captures, device identifiers, account identifiers, or local artifacts.

## Current status

```text
Operator sign-off status: not signed
Live CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
Runtime integration gate: blocked
```

## Required sign-off metadata

Fill this section only with non-sensitive values.

```text
Reviewer name or role: <required>
Review date: <YYYY-MM-DD>
Repository commit under review: <commit hash>
Dry-run report reference: <masked local report reference only>
Readiness gate report reference: <masked local report reference only>
Decision record reference: docs/integrations/gree-alice/mqtt-connect-safety-review-decision-record.md
Sign-off result: blocked-review-incomplete | blocked-safety-risk | ready-for-separate-connect-only-safety-stage
```

## Required confirmations

Every item must be checked by a human before the result can be considered complete.

```text
[ ] I verified the reviewed dry-run report is masked.
[ ] I verified the reviewed readiness gate report is masked.
[ ] I verified no raw token/password/auth secret is included.
[ ] I verified no raw device key is included.
[ ] I verified no raw MAC/account identifier is included.
[ ] I verified no PCAP payload is committed.
[ ] I verified no CSV export is committed.
[ ] I verified no local artifact under artifacts/ is committed.
[ ] I verified the current decision record is blocked-review-incomplete unless explicitly changed in a later stage.
[ ] I verified live CONNECT remains blocked by this template.
[ ] I verified SUBSCRIBE remains blocked by this template.
[ ] I verified PUBLISH remains blocked by this template.
[ ] I verified device control remains blocked by this template.
[ ] I verified AssistantEngineer.Api integration remains blocked.
[ ] I verified Telegram integration remains blocked.
[ ] I verified runtime config changes remain blocked.
[ ] I verified deployment changes remain blocked.
[ ] I verified migrations remain blocked.
[ ] I verified no third-party repository/source names were added to public docs.
```

## Result rules

The sign-off result must use exactly one of these values:

```text
blocked-review-incomplete
blocked-safety-risk
ready-for-separate-connect-only-safety-stage
```

The value `ready-for-separate-connect-only-safety-stage` still does not permit live CONNECT. It only allows a later explicit stage proposal to be created and reviewed.

## Forbidden content

Do not write any of the following into this file or any committed repository file:

```text
raw credentials
token values
password values
auth secret values
device key values
MAC addresses
account identifiers
PCAP files or packet payloads
CSV exports
artifacts/ files
device control payloads
live MQTT topic values
```

## Explicit non-approval

```text
This template does not approve live MQTT CONNECT.
This template does not approve MQTT SUBSCRIBE.
This template does not approve MQTT PUBLISH.
This template does not approve device control.
This template does not approve AssistantEngineer.Api integration.
This template does not approve Telegram integration.
This template does not approve runtime configuration.
This template does not approve deployment changes.
This template does not approve migrations.
```

## Next allowed step

The next safe step may package the offline evidence references into a review packet summary, still without raw values and still without live MQTT.

```text
Next possible stage: GREE-ALICE-26 — CONNECT-only offline review packet summary
Still blocked: live CONNECT, SUBSCRIBE, PUBLISH, device control
```
