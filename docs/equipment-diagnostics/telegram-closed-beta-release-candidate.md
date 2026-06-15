# EquipmentDiagnostics Telegram Closed Beta Release Candidate

## Purpose

This document defines the reviewed release-candidate boundary before inviting Telegram closed-beta operators. It is closed beta only, not production or public release, and does not activate Telegram.

## Release Candidate Scope

- Existing deterministic EquipmentDiagnostics bot flow and Telegram webhook transport.
- Existing access policy, safety/provenance boundaries, health/readiness checks, sanitized logging, and rollback scripts.
- ED-20A beta readiness, ED-21B goal-run validation, ED-22A release evidence, ED-22C deployment activation dry-run, ED-22D activation checklist/runbook, operator limitation card, and manual smoke matrix.

## Out Of Scope

- Production/public release, automatic activation, real deployment, long polling, database or audit persistence, external monitoring, AI/RAG/vector search, or broad vendor-manual completeness.
- Runtime answers from manual-codebook, staging, or preview data.

## Required Generated Evidence

Generate locally and do not commit:

- `artifacts/verification/equipment-diagnostics/telegram-closed-beta/release-evidence-summary.md`
- `artifacts/verification/equipment-diagnostics/telegram-closed-beta/release-evidence-report.json`
- `artifacts/verification/equipment-diagnostics/telegram-closed-beta/telegram-closed-beta-goal-run-report.json`
- `artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run/deployment-dry-run-summary.md`
- `artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run/deployment-dry-run-report.json`
- `artifacts/verification/equipment-diagnostics/telegram-activation-checklist/activation-checklist-summary.md`
- `artifacts/verification/equipment-diagnostics/telegram-activation-checklist/activation-checklist-report.json`

## Required Verification Before Activation

- `dotnet restore`
- `dotnet build AssistantEngineer.sln`
- Targeted Telegram deterministic tests
- Full backend tests
- `prepare-telegram-closed-beta-release-evidence.ps1`
- `prepare-telegram-closed-beta-deployment-dry-run.ps1 -SkipDockerComposeConfig`
- `prepare-telegram-closed-beta-activation-checklist.ps1`
- ED-20A beta readiness report
- ED-21B `goal-run-report` validator
- Manual smoke matrix review

## Manual Review Points

- Zero blockers; every warning reviewed.
- Generated artifacts remain ignored and uncommitted.
- No real secrets in Git, real domains, chat IDs, raw logs, PDFs, or manual files.
- Telegram is disabled by default and chat ID discovery is disabled by default.
- `AllowedChatIds` and `DeniedChatIds` are reviewed before activation.
- Runtime catalog is the only final-answer source.
- Manual-codebook, staging, and preview are not final diagnosis.

## Activation Preconditions

- Select a VPS/domain and prepare HTTPS later under a separately approved activation stage.
- Create the BotFather credential only during approved activation.
- Generate the webhook secret only during approved activation.
- Run `setWebhook` and `getWebhookInfo` during activation, not ED-22B.
- Review ED-22C deployment dry-run evidence before any separately approved activation; ED-22C does not execute webhook operations.
- Review the ED-22D activation runbook and use its smoke evidence template; ED-22D does not execute webhook operations.

## Rollback

1. Delete the webhook using the reviewed rollback script.
2. Set `TELEGRAM_IS_ENABLED=false`.
3. Restart the stack.
4. Check health and readiness.
5. Collect sanitized logs only.

## Release Decision

- **Ready for closed beta activation:** required evidence passes, blockers are zero, warnings and smoke results are reviewed.
- **Needs manual review:** evidence is complete but a reviewer must resolve or accept documented warnings.
- **Blocked:** any required verification, safety boundary, access policy, or rollback check fails.

## Required Limitations

- Closed beta only; not production or public release.
- No long polling, database/audit persistence, or external monitoring.
- Runtime catalog is the only final-answer source.
- Manual-codebook, staging, and preview are not final diagnosis.
- Vendor manual coverage is partial; no completeness claim is made.
