# Telegram Closed Beta Release Tag And Handoff

## Purpose

ED-22F defines the committed procedure for selecting, tagging, documenting, and handing off an EquipmentDiagnostics Telegram closed-beta candidate.

This is closed beta only; not for production or public launch. ED-22F does not create a real tag from a feature branch, activate Telegram, deploy the product, or change runtime behavior.

## When To Use

Use after ED-22E passes, its warnings are reviewed, and the candidate changes are merged into `master`. Run the local ED-22F checklist before manually creating the annotated tag.

## Release Tag Candidate Scope

- The merged `master` commit covered by reviewed ED-22A through ED-22E evidence.
- Committed deterministic EquipmentDiagnostics Telegram closed-beta runtime and operational guidance.
- Placeholder-only release notes and handoff references.

## Prerequisites And Required Evidence

- ED-22A release evidence.
- ED-22C deployment dry-run.
- ED-22D activation checklist.
- ED-22E final go/no-go.
- Branch readiness with zero blockers.
- EquipmentDiagnostics tests.
- Full backend tests.
- Clean `git status --short` before the manual tag operation.

## Tag Naming Policy

Use `equipment-diagnostics-telegram-closed-beta-v<major>.<minor>.<patch>`. The recommended first candidate tag is:

`equipment-diagnostics-telegram-closed-beta-v0.1.0`

Reuse of an existing tag at another commit is a blocker. An existing local tag at the selected commit requires manual review before any push.

## Manual Annotated Tag Creation

Only after merge into `master`, manually run:

```powershell
git switch master
git pull
git status --short
git rev-parse HEAD
git tag -a equipment-diagnostics-telegram-closed-beta-v0.1.0 -m "EquipmentDiagnostics Telegram closed beta v0.1.0"
git push origin equipment-diagnostics-telegram-closed-beta-v0.1.0
```

Confirm `git rev-parse HEAD` matches the reviewed candidate commit before creating the tag. The ED-22F runner records these commands only as manual instructions and never executes tag creation or push.

## Handoff Checklist

- [ ] Selected commit is the reviewed merged `master` commit.
- [ ] ED-22E status, decision, blockers, and warnings are reviewed.
- [ ] Release tag name and annotated message are reviewed.
- [ ] `telegram-closed-beta-release-notes-template.md` is completed outside Git using sanitized evidence paths.
- [ ] Final handoff, operator prerequisites, activation window, and rollback owner are approved.
- [ ] Tag creation and push are performed manually by an authorized operator.

## Evidence Archive Expectations

Keep generated reports, completed handoff notes, release notes, sanitized smoke results, and rollback evidence in the approved evidence archive outside Git. Do not commit generated artifacts, raw logs, PDFs, manual files, secrets, domains, chat IDs, or raw Telegram payloads.

## Rollback Reference

The tagged commit is the immutable source reference for code rollback. To inspect or prepare rollback, fetch tags and review the tagged commit without changing active server configuration. Operational rollback follows `telegram-closed-beta-activation-runbook.md`; ED-22F does not execute rollback.

## Transition To ED-23A

After the annotated tag is manually created and the handoff is approved, proceed to ED-23A real server activation planning. Real activation happens only in ED-23A or a manually approved operational window.

## Safety And Operational Boundary

- No real secrets in Git.
- No real domains in Git.
- No real chat IDs in Git.
- Telegram disabled by default.
- Chat ID discovery disabled by default.
- No long polling.
- No DB/audit persistence.
- No external monitoring.
- Runtime catalog remains the only final-answer source.
- Manual-codebook/staging/preview are not final diagnosis.
- Do not claim complete vendor manual coverage.
- Do not bypass protections or safety devices.
- No hazardous electrical/refrigerant instructions through the bot.
- There is no AI/RAG/vector-search capability.
- Tag/handoff does not call Telegram network.
- Tag/handoff does not execute setWebhook/getWebhookInfo/deleteWebhook.

## What This Does Not Validate

- Real server configuration, HTTPS, webhook delivery, external monitoring, audit persistence, or real smoke results.
- Production or public launch suitability.
- Runtime, calculation physics, public calculation API routes, ISO52016 behavior, appsettings, or Docker Compose changes.

## Go/No-Go States After Tag Review

- `READY_TO_TAG_AFTER_MASTER_MERGE`: deterministic checks passed with no blockers or warnings; create the annotated tag manually after merge.
- `READY_WITH_MANUAL_REVIEW`: checks passed, but warnings or operator approvals require review before manual tag creation.
- `NO_GO_BLOCKED`: resolve blockers before creating or pushing a tag.
