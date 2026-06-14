# Telegram Closed Beta Deployment Activation Dry Run

## Purpose

ED-22C provides a deterministic local deployment activation dry-run before any separately approved EquipmentDiagnostics Telegram activation. It is closed beta only, not production or public release, and it performs no deployment or activation.

## Dry-Run Scope

- Verify the provider-neutral deployment scaffold, safe placeholder environment example, deployment operation scripts, Telegram operation-script inventory, and ED-22A/ED-22B evidence references.
- Confirm Telegram disabled by default and chat ID discovery disabled by default.
- Confirm no generated verification artifacts are committed.
- Generate a local JSON report and Markdown summary for manual review.

## Out Of Scope

- No Telegram network calls and no setWebhook execution.
- No Telegram activation, real deployment, production/public release, long polling, DB/audit persistence, external monitoring, AI, RAG, or vector search.
- No real secrets in Git, no real domains, and no real chat IDs.
- No Docker requirement or Docker Compose execution.

## Prerequisites

- A clean local repository checkout with the committed deployment scaffold and operation scripts.
- Git and PowerShell available locally.
- Optional deployment validators available in `scripts/deployment/`.
- ED-22A release evidence and ED-22B release candidate documents available for reference.

## Command Usage

```powershell
.\scripts\equipment-diagnostics\prepare-telegram-closed-beta-deployment-dry-run.ps1 `
  -BaseRef origin/master `
  -SkipDockerComposeConfig
```

For a focused script smoke, also pass `-SkipDeploymentScaffoldValidation`, `-SkipProductionEnvValidation`, and `-SkipReleaseEvidenceReference`. Skips remain visible in the report. The runner never reads `deploy/.env`; it validates only the committed safe placeholder example when that validation is enabled.

## What Is Checked

- Required deployment files and operation scripts exist.
- Telegram operation scripts exist as reviewed inventory targets only.
- The environment example keeps Telegram and chat ID discovery disabled and keeps secret/chat-list placeholders empty.
- Docker Compose preserves disabled Telegram and chat ID discovery defaults.
- Reviewed deployment docs and scripts contain no Telegram token-like value or non-placeholder URL host.
- Git tracks no generated content under `artifacts/verification`.

## What Is Not Checked

- No Telegram API, setWebhook, getWebhookInfo, or deleteWebhook operation is called.
- No real token, webhook secret, domain, chat ID, or deployment environment value is required or collected.
- No runtime endpoint, Telegram command, Docker command, provider deployment, external monitoring, or audit store is exercised.

## Generated Artifacts Policy

Generated evidence is local and must not be committed. The runner writes only:

- `artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run/deployment-dry-run-summary.md`
- `artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run/deployment-dry-run-report.json`

## Expected Generated Outputs

The JSON report records generation time, base reference, branch, head, PASS/FAIL status, blockers, warnings, deterministic checks, generated-artifact paths, and limitations. The Markdown summary provides a short manual-review boundary.

## Manual Review Checklist

- [ ] Dry-run status is `PASS`; blockers are zero and every warning is reviewed.
- [ ] Generated evidence remains ignored and uncommitted.
- [ ] No real secrets in Git, no real domains, and no real chat IDs are present.
- [ ] Telegram disabled by default and chat ID discovery disabled by default remain unchanged.
- [ ] No Telegram network calls, no setWebhook execution, and no long polling occurred.
- [ ] No DB/audit persistence or external monitoring is implied.
- [ ] Runtime catalog is only final-answer source.
- [ ] Manual-codebook/staging/preview are not final diagnosis.
- [ ] Vendor manual coverage remains partial; no completeness claim is made.

## Relation To Earlier Evidence

ED-22A collects deterministic release evidence and an ED-21B-compatible goal-run report. ED-22B defines the release-candidate review boundary, operator limitation card, and manual smoke matrix. ED-22C adds required pre-activation deployment-scaffold evidence without activating Telegram or replacing either earlier review.
