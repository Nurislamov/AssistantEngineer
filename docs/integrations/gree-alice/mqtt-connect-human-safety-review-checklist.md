# GREE-ALICE-23: CONNECT-only human safety review checklist

## Purpose

This checklist is a human review gate for a possible future CONNECT-only safety decision.

It does not approve live MQTT use. It only defines what a human reviewer must confirm before any later stage can even request a live CONNECT-only probe.

## Current decision

```text
Human safety review status: not approved
Live CONNECT gate: blocked
SUBSCRIBE gate: blocked
PUBLISH gate: blocked
Device control gate: blocked
```

## Required inputs for review

The reviewer must use masked/local evidence only. Do not paste raw credentials, tokens, device identifiers, MAC addresses, account identifiers, PCAP payloads, or CSV exports into repository files.

Required evidence:

```text
1. Latest masked CONNECT dry-run report exists.
2. Latest readiness gate report exists.
3. Readiness gate says ready-for-human-live-safety-review only.
4. Readiness gate still says live CONNECT is blocked.
5. Readiness gate still says SUBSCRIBE is blocked.
6. Readiness gate still says PUBLISH is blocked.
7. Readiness gate still says device control is blocked.
8. No raw secret or device/account identifier appears in committed docs or tests.
```

## Human checklist

The reviewer must answer every item before any later safety stage can be proposed.

```text
[ ] I verified the report was generated from masked inputs only.
[ ] I verified the report does not contain raw token/password/auth values.
[ ] I verified the report does not contain raw device key values.
[ ] I verified the report does not contain raw MAC/account identifiers.
[ ] I verified the report does not contain packet payload exports.
[ ] I verified the report does not contain private CSV artifacts.
[ ] I verified CONNECT is still blocked by default.
[ ] I verified SUBSCRIBE is still blocked.
[ ] I verified PUBLISH is still blocked.
[ ] I verified device control is still blocked.
[ ] I verified this review does not require API integration.
[ ] I verified this review does not require Telegram integration.
[ ] I verified this review does not require runtime config.
[ ] I verified this review does not require deployment changes.
[ ] I verified this review does not require migrations.
[ ] I verified no third-party repository/source names are added to public docs.
```

## Required reviewer decision

The review result must be one of:

```text
blocked-review-incomplete
blocked-safety-risk
ready-for-separate-connect-only-safety-stage
```

The value ready-for-separate-connect-only-safety-stage does not allow live CONNECT by itself. It only allows creating a separate explicit safety stage proposal.

## Explicit non-goals

```text
No live MQTT CONNECT.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No API integration.
No Telegram integration.
No runtime configuration.
No deployment change.
No migration.
No artifact commit.
No raw credential commit.
No token/password/device key/MAC/account identifier commit.
```

## Stage exit criteria

GREE-ALICE-23 can be closed only when:

```text
1. This checklist is committed.
2. Guard tests for this checklist pass.
3. Existing GREE-ALICE guard tests still pass.
4. PROJECT_STATE.md points to the next safe non-live step or explicitly keeps live MQTT blocked.
```

## Next allowed step

The next step may be a separate proposal for a CONNECT-only safety review outcome, but live MQTT remains blocked until a later explicit stage says otherwise.

```text
Next possible stage: GREE-ALICE-24 — CONNECT-only safety review decision record
Still blocked: live CONNECT, SUBSCRIBE, PUBLISH, device control
```
