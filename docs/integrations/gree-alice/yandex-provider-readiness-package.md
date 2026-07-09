# Yandex Provider Readiness Package

## Purpose

This stage adds an offline-only readiness package for future Yandex Smart Home provider preparation, manual review, and publication planning.

It explains what is present, what remains blocked, and what must be manually approved before any real provider registration or pilot.

## Current Status

Provider readiness is NOT READY by default.

Real provider registration is NOT APPROVED.

Real OAuth is not implemented.

Real Yandex credentials/tokens must not be stored in repo.

Production endpoint is not configured.

Production deploy is not enabled.

Live Gree+ Cloud control is disabled.

MQTT is blocked.

Device control remains fail-closed.

## Out Of Scope

This stage does not add real Yandex provider registration, real OAuth, OAuth endpoints, production callbacks, production URLs, production credentials, live calls to Yandex, live calls to Gree+ Cloud, MQTT, device control, production runtime wiring, deployment changes, migrations, admin UI, or admin endpoints.

## Provider Readiness Status

```text
Yandex provider readiness: NOT READY
Provider registration: NOT APPROVED
Production credentials: FORBIDDEN IN REPO
Real OAuth: NOT IMPLEMENTED
Production deploy: DISABLED
Live Gree+ Cloud control: DISABLED
MQTT: BLOCKED
Device control: FAIL-CLOSED
```

## Smart Home API Endpoint Readiness

```text
GET  /v1.0/user/devices        offline-contract-present
POST /v1.0/user/devices/query  offline-contract-present
POST /v1.0/user/devices/action offline-contract-present-fail-closed
POST /v1.0/user/unlink         offline-contract-present
future OAuth authorize         not-implemented
future OAuth token             not-implemented
future OAuth callback          not-implemented
```

OAuth endpoints are not implemented and must not be added without a separate approved stage.

## Account Linking Readiness

The account-linking boundary exists in offline-template mode.

Yandex users must map to a bridge account and explicit registry scope before devices are returned.

Unknown and unlinked users fail closed.

## Registry And Device Exposure Readiness

The registry import/admin boundary exists in offline-template mode.

Devices are exposed only through reviewed registry state. Gree Cloud discovery must not auto-expose devices.

Stable Yandex IDs and room binding remain required for exposed devices.

## VRF/GMV Child-Unit Readiness

The VRF/GMV gateway and child-unit model exists.

Gateway remains internal by default.

Child units can be exposed only as reviewed Yandex user devices with stable IDs and room binding.

## Manual Smoke Plan

Manual smoke is required before any provider publication decision.

Smoke remains local/offline only and includes build, tests, static safety scans, local bridge health, `/devices`, `/query`, `/action` fail-closed, `/unlink`, account-linking template, scoped registry template, VRF child-unit exposure, unknown user/device fail-closed checks, and no-secret/no-MQTT/no-production-wiring scans.

## Security Review

Security review is required and NOT APPROVED by default.

The review must verify no secrets in repository, no real Yandex credential material, no real access/refresh token material, no real Gree credentials, no device keys, no MAC-like identifiers, masked evidence only, least-scope registry exposure, unknown/unlinked users fail closed, action fail-closed, and MQTT blocked.

## Operator Checklist

Operator approval is NOT APPROVED by default.

Before publication, a responsible operator must record commit, validation, smoke evidence, security review, provider publication decision, OAuth decision, production endpoint decision, read-only pilot decision, control pilot decision, rollback owner, kill-switch owner, and monitoring owner.

## Publication/Submission Checklist

Submission status is NOT APPROVED.

Publication requires separate review of Smart Home contracts, account linking, OAuth implementation, secret storage outside repository, registry scope, VRF child-unit exposure, stable IDs, security, smoke, monitoring, rollback, kill-switches, read-only pilot approval, and control pilot approval if any.

## What Remains Blocked

Real provider registration remains blocked.

Real OAuth remains blocked.

Real Yandex credentials/tokens remain forbidden in repo.

Production endpoint and deploy remain disabled.

Live Gree+ Cloud control remains disabled.

MQTT remains blocked.

Device control remains fail-closed.

## Future Stages

Future stages may add local provider smoke harness, production provider registration review, OAuth implementation design, token storage design, and live read-only pilot work. Each must be separately approved.

## Next Stage

GREE-ALICE-50 should add a local Yandex provider smoke harness. It should remain local/offline and should not enable real OAuth, production provider registration, live Gree+ Cloud control, MQTT, device control, or production wiring.
