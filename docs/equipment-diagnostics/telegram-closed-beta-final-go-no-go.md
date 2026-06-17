# Telegram Closed Beta Final Go/No-Go Evidence

## Purpose

ED-22E adds the final deterministic evidence layer before a separately approved manual Telegram activation. It answers whether repository evidence supports moving to manual operational activation or whether blockers and warnings still require review.

This is closed beta only; not for production or public launch. It is not activation, deployment, a runtime feature, or a public release decision.

## When To Run

Run after ED-22A release evidence, ED-22C deployment dry-run, and ED-22D activation-checklist guidance are available, and before any real server-side activation step.

## Inputs And Evidence Sources

- Base reference, branch, and head commit collected safely, including detached HEAD.
- ED-22A release evidence pack.
- ED-22C deployment dry-run.
- ED-22D activation checklist and activation runbook.
- EquipmentDiagnostics branch readiness.
- Focused backend and EquipmentDiagnostics tests.

## Command

```powershell
.\scripts\equipment-diagnostics\prepare-telegram-closed-beta-final-go-no-go.ps1 -BaseRef origin/master
```

Focused safe smoke mode may use every explicit skip switch. Skipped checks remain warnings and produce `MANUAL_REVIEW_REQUIRED`; they are never silently treated as completed evidence.

## Go Criteria

- All required ED-22 documents, evidence scripts, and manual activation tools exist.
- Enabled deterministic checks pass.
- Blockers are zero.
- Generated evidence remains ignored and uncommitted.
- A named operator and reviewer confirm access policy, safety boundary, warning disposition, smoke plan, and rollback readiness.

## No-Go Criteria

- Any required file or enabled deterministic check fails.
- Generated verification artifacts are tracked or staged.
- A secret, real domain, real chat ID, unsafe diagnostic instruction, access-policy failure, or rollback-readiness gap is found.
- Telegram or chat ID discovery committed safe defaults are changed.

## Warning Review Rules

- Skipped checks are warnings and require manual review.
- Warnings do not automatically block report generation, but activation cannot proceed until every warning is explicitly accepted or resolved.
- `GO_WITH_WARNINGS_REVIEWED` may be recorded only by the approved reviewers in the final handoff after documenting each accepted warning.
- Unreviewed warnings keep the decision at `MANUAL_REVIEW_REQUIRED`.

## Manual Approval Requirements

- Operator and reviewer identities are recorded with placeholders in the final handoff.
- The activation window and rollback owner are assigned.
- Every blocker is resolved and every warning is reviewed.
- Rollback access and the sanitized smoke evidence plan are confirmed before activation.

## Generated Artifacts Policy

The runner writes only ignored local evidence:

- `artifacts/verification/equipment-diagnostics/telegram-final-go-no-go/final-go-no-go-summary.md`
- `artifacts/verification/equipment-diagnostics/telegram-final-go-no-go/final-go-no-go-report.json`

Generated artifacts, log dumps, PDFs, and manual files must not be committed.

## Safety And Operational Boundary

- No real secrets in Git.
- No real domains in Git.
- No real chat IDs in Git.
- Telegram disabled by default.
- Chat ID discovery disabled by default.
- Chat ID discovery may be enabled only temporarily during setup.
- Polling disabled by default.
- No DB/audit persistence.
- No external monitoring.
- Runtime catalog remains the only final-answer source.
- Manual-codebook/staging/preview are not final diagnosis.
- Do not claim complete vendor manual coverage.
- Do not bypass protections or safety devices.
- No hazardous electrical/refrigerant instructions through the bot.
- There is no AI/RAG/vector-search capability.

## What This Does Not Validate

- Final go/no-go does not call Telegram network.
- Final go/no-go does not execute setWebhook/getWebhookInfo/deleteWebhook.
- It does not read `deploy/.env`, require a real token/domain/chat ID, run Docker, or alter server configuration.
- It does not validate real webhook delivery, public HTTPS, external monitoring, audit persistence, or real smoke results.
- It does not change calculation physics, public calculation API routes, ISO52016 behavior, runtime catalog behavior, appsettings, or Docker Compose.

## Transition To Real Activation

A passing report permits only the committed ED-22F `telegram-closed-beta-release-tag-and-handoff.md` procedure or manual activation planning. ED-22F creates no tag automatically. Real activation remains a separate, explicitly approved human operation following `telegram-closed-beta-activation-runbook.md`.

## Rollback Readiness Expectations

Before activation, assign a rollback owner, confirm access to the reviewed manual rollback tool, preserve Telegram and discovery disabled defaults in Git, and prepare sanitized rollback evidence. ED-22E does not execute rollback.

## Final Decision States

- `GO_FOR_MANUAL_ACTIVATION`: enabled deterministic checks passed with zero blockers and zero warnings; manual approval is still required.
- `GO_WITH_WARNINGS_REVIEWED`: all warnings were explicitly reviewed and accepted in the final handoff.
- `NO_GO_BLOCKED`: one or more blockers must be resolved before activation.
- `MANUAL_REVIEW_REQUIRED`: evidence generation passed, but warnings or incomplete manual approvals remain.
