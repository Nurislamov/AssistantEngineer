# GREE-ALICE Release Readiness Audit

## Purpose

Stop scope drift in the GREE-ALICE workstream and record the release decision after the offline bridge, smoke, runbook, and localhost HTTP smoke stages.

## Current release decision

Current Yandex Smart Home production release status: NOT READY.
Current internal/offline engineering release status: NEAR READY.

The next stage should be release-oriented: `GREE-ALICE-RC1 — cut internal offline release candidate`.

## What is already done

- Isolated offline `GreeAliceBridge` bounded context.
- Offline Yandex Smart Home API skeleton.
- Offline account/device registry and registry import/admin boundary.
- VRF/GMV gateway and child-unit model with stable dummy Yandex device IDs.
- Account linking boundary.
- Provider readiness package.
- Local smoke harness.
- Operator runbook.
- PowerShell local smoke script.
- Localhost-only HTTP smoke boundary.
- Safety guards, documentation, and tests.

## What is releasable now

An internal/offline engineering release candidate is near-ready. It can release the current local/offline bridge boundary, deterministic dummy/template behavior, local smoke harness, runbook, and localhost-only HTTP smoke process for engineering review.

## What is not releasable now

A real Yandex Smart Home production release is not releasable now. It remains blocked by missing real Yandex provider registration, production OAuth implementation, production endpoint, secure secret storage, production deployment, live Gree+ approval, and production operator approval.

## Release types

### Internal/offline engineering release

This is close to a release candidate because it has:

- offline bridge contracts;
- offline Yandex Smart Home skeleton;
- VRF/GMV child-unit model;
- registry/import boundary;
- account linking boundary;
- provider readiness docs;
- local smoke harness;
- operator runbook;
- PowerShell smoke script;
- localhost-only HTTP smoke;
- tests PASS.

### Real Yandex Smart Home production release

This is NOT READY because it does not have:

- real Yandex provider registration;
- production OAuth implementation;
- real production callback/authorize/token endpoints;
- secure secret storage plan implemented;
- production endpoint configured;
- production deployment wiring;
- live read-only Gree+ integration approved;
- live control approved;
- operator production pilot approval.

## Internal/offline engineering release status

Current internal/offline engineering release status: NEAR READY.

Remaining work before RC is packaging and evidence discipline, not another abstract boundary. The RC should verify the current commit, local smoke harness, localhost HTTP smoke where available, docs, and masked evidence.

## Yandex Smart Home production release status

Current Yandex Smart Home production release status: NOT READY.

No document, checklist, smoke script, or local dummy endpoint should claim production readiness.

## Release gap matrix

| Area | Current status | Release impact | Blocker level | Next action |
|---|---|---|---|---|
| Offline API skeleton | Present and tested | Supports internal/offline RC | Non-blocker for internal RC | Keep stable |
| VRF/GMV child units | Present with dummy exposure | Supports internal/offline RC | Non-blocker for internal RC | Keep gateway hidden |
| Registry import/admin | Offline-template only | Supports local review | Non-blocker for internal RC | Keep manual review boundary |
| Account linking boundary | Offline-template only | Supports local model review | Production blocker | Add real OAuth only in a separate approved stage |
| Provider readiness package | Present, NOT READY | Prevents false production claims | Production blocker | Keep production status honest |
| Local smoke harness | Present and passing | Supports internal/offline RC | Non-blocker | Run for RC evidence |
| Local HTTP smoke | Present and localhost-only | Supports internal/offline RC | Non-blocker if local API available | Run for RC evidence |
| Yandex provider registration | Not done | Blocks real Yandex release | Critical production blocker | Prepare submission only after RC decision |
| OAuth implementation | Dev-only local slice only | Blocks real Yandex release | Critical production blocker | Design and approve production storage/deployment separately |
| Production endpoint | Not configured | Blocks real Yandex release | Critical production blocker | Requires hosting/deployment approval |
| Secrets/token storage | Not implemented | Blocks real Yandex release | Critical production blocker | Implement secure secret storage before production |
| Live Gree+ read-only integration | Not approved | Blocks real production behavior | Critical production blocker | Approve read-only pilot first |
| Live Gree+ control | Not approved | Blocks device control | Critical production blocker | Postpone until safety approval |
| MQTT | Blocked | Blocks MQTT-based runtime paths | Critical production blocker | Postpone |
| Production deploy | Disabled | Blocks production release | Critical production blocker | Approve deployment package separately |
| Operator approval | NOT APPROVED | Blocks production pilot | Critical production blocker | Record explicit operator decision |
| Security review | Required, not approved for production | Blocks production release | Critical production blocker | Complete before production pilot |

## Critical blockers

- real Yandex provider registration;
- production OAuth implementation;
- production endpoint;
- secure secret storage;
- production deployment;
- live Gree+ read-only approval;
- live control approval;
- operator production pilot approval;
- production security review.

## Non-blockers

- Offline API skeleton for internal/offline RC.
- VRF/GMV child-unit dummy exposure for internal/offline RC.
- Local smoke harness.
- Localhost-only HTTP smoke.
- Operator runbook and masked evidence templates.
- Existing tests and static safety guards.

## Scope that must be postponed

- live Gree+ control;
- MQTT;
- multi-account rollout;
- production deploy;
- real Yandex provider submission;
- live OAuth;
- admin UI;
- automatic Gree Cloud discovery auto-expose;
- broad VRF control.

## Shortest RC path

### RC path option A — internal/offline release

1. `GREE-ALICE-RC1 — cut internal offline release candidate`.
2. `GREE-ALICE-RC1-SMOKE — run documented local smoke and archive masked evidence`.
3. `GREE-ALICE-RC1-DOCS — final release notes and tag decision`.

### RC path option B — real Yandex pilot

1. `GREE-ALICE-54 — implement local-only OAuth contract endpoints behind offline/test mode`.
2. `GREE-ALICE-55 — production hosting/secrets/deployment approval package`.
3. `GREE-ALICE-56 — Yandex provider submission/pilot checklist`.

Recommended next stage: `GREE-ALICE-RC1 — cut internal offline release candidate`.

## Recommended next stage

`GREE-ALICE-RC1 — cut internal offline release candidate`.

Do not make the next default step another abstract boundary stage.

## RC1 decision

Internal/offline RC1 can be cut after full validation PASS.

Yandex Smart Home production release remains NOT READY.

Recommended next track after RC1:
GREE-ALICE-PILOT-1 — implement real Yandex OAuth/provider minimal skeleton.

## PILOT-1A contract decision

The real pilot track starts with design-only contract work:

- [Yandex OAuth provider pilot contract](./yandex-oauth-provider-pilot-contract.md)
- [Yandex OAuth provider pilot config example](./yandex-oauth-provider-pilot-config.example.json)

PILOT-1A does not implement runtime OAuth, real provider registration, real credentials/tokens, production deploy, live Gree+ Cloud, MQTT, or device control.

Recommended next implementation stage:
GREE-ALICE-PILOT-1B — implement dev-only Yandex OAuth/provider skeleton.

## PILOT-1B dev-only vertical slice

PILOT-1B adds local/dev-only OAuth endpoints and bearer-protected provider smoke mode:

- `GET /oauth/authorize`;
- `GET /oauth/callback`;
- `POST /oauth/token`;
- Provider Adapter API calls with Bearer token in `PrivateSkillDevOnly` mode;
- in-memory token records only;
- dummy/offline devices only;
- `/action` remains dry-run fail-closed.

This does not change the production release decision. Real Yandex Smart Home production release remains NOT READY.

## Final decision

Internal/offline engineering release: NEAR READY.

Real Yandex Smart Home production release: NOT READY.

Proceed to internal/offline RC packaging and evidence, not production release.
