# AssistantEngineer Project State

## Current stage

ED-24GMVFLEX.5 — PROJECT_STATE checkpoint after GMV9 Flex production pass.

GMV9 Flex diagnostics are CLOSED / production PASS.

## Current branch

master

## Current commits

- Current master: bb57ebaa
- PR #51 merge commit: bb57ebaa
- Implementation commit: 69c04ca2
- Previous stable master before GMV9 Flex merge: 6de11046

## Last completed work

ED-24GMVFLEX.1-3 was completed, merged to master, and passed CI.

Scope:
- Audited and normalized existing GMV9 Flex diagnostic knowledge.
- Preserved GMV9 Flex package counts:
  - outdoor: 120
  - indoor: 60
  - status: 43
  - debugging: 37
  - total: 260
- Removed forbidden visible service/source wording from GMV9 Flex Telegram diagnostic answers.
- A0 and A2 were normalized as neutral status/informational answers.
- bJ, bn, and C0 received practical troubleshooting checks from the GMV9 Flex service manual troubleshooting section.
- 173 cards are linked to Troubleshooting evidence.
- 87 cards remain Error Indication only.
- ManualVerified / High metadata, manualId, and document code GC202512-I were preserved.
- GMV-450WML/A-X(D) model coverage was added for GMV9 Flex.
- Equipment map was synchronized to the existing Imported convention.
- Runtime code was not changed; GMV9 Flex was already active through embedded JSON glob and EquipmentDiagnosticsJsonKnowledgeSource.
- GMV6, GMV X, and GMV Mini diagnostic content were not changed.

ED-24GMVFLEX.4 was completed on production.

Production:
- VPS: assistantengineer-beta-01
- Production repo path: /opt/assistantengineer
- Production commit: bb57ebaa
- API container recreated and running.
- Telegram polling started.
- Telegram deleteWebhook on startup succeeded.
- Production logs were clean during smoke: no unhandled exception, JSON parse error, duplicate key error, or polling failure observed.

Telegram production smoke PASS:
- Gree GMV9 Flex A0
- Gree GMV9 Flex A2
- Gree GMV9 Flex bJ
- Gree GMV9 Flex BJ
- Gree GMV9 Flex bj
- Gree GMV9 Flex bn
- Gree GMV9 Flex BN
- Gree GMV9 Flex C0
- Gree GMV9 Flex GMV-450WML/A-X(D) C0
- /last checked after C0

## Current blocker

None for GMV9 Flex production close.

## Known follow-up notes

1. /last preserves the last code C0, but the display currently shows it as "Gree C0" without the matched GMV9 Flex series label.
   Suggested backlog:
   ED-24UX.LAST — Improve /last display to preserve matched series/model label.

2. VPS repository has old untracked env backup files under deploy/:
   - deploy/.env.before-ed-24ops2-20260630T021054Z
   - deploy/.env.before-operator-inbox-20260630T021726Z

   These files did not block GMV9 Flex deployment because they are old backup files and not runtime code.
   Suggested backlog:
   ED-24OPS.CLEANUP — Move VPS env backups outside repo or ignore backup pattern safely.

## Important decisions

- GMV9 Flex was not imported again from scratch.
- Existing 260 GMV9 Flex cards were normalized and tested.
- Manual evidence was aligned against the GMV9 Flex service manual Chapter 3:
  - Error Indication
  - Troubleshooting
  - Common Malfunctions reviewed but not used as code-specific evidence
- No PDF/manual binaries were committed.
- No generated ZIP/manual-review artifacts were committed.
- No production env changes were made.
- No migrations were added.
- No deployment scripts were changed.
- PROJECT_STATE.md is updated only after production PASS.

## Files changed recently

ED-24GMVFLEX.1-3 changed:
- data/equipment-diagnostics/error-knowledge/gree/gmv9-flex/**
- data/equipment-diagnostics/equipment-catalog/gree-vrf-equipment-map.json
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmv9FlexImport15Tests.cs

ED-24GMVFLEX.5 changes:
- PROJECT_STATE.md

## Validation status

CI on PR #51:
- 7/7 checks PASS.
- Engineering Core V1 PASS.
- Engineering Core V1 Contracts PASS.
- Engineering Core V1 Smoke PASS.
- Engineering Core V1 Validation PASS.
- ISO52016 Matrix release-ready PASS.
- EquipmentDiagnostics Branch Readiness PASS.

Master after merge:
- master at bb57ebaa.
- origin/master at bb57ebaa.
- master push checks PASS.

Local validation reported before merge:
- Restore PASS.
- Build PASS with existing nullable warnings.
- GMV9 focused tests PASS: 39/39.
- Telegram/JSON/catalog focused tests PASS: 176/176.
- Full backend PASS: 5258/5258.
- Branch readiness PASS.
- git diff --check PASS.
- Engineering Core verifier PASS in CI.

Production validation:
- Deploy PASS.
- API container running.
- Telegram polling clean.
- Telegram GMV9 Flex smoke PASS.
- Logs clean for the smoke window.

## Next step

Recommended next stage:

ED-24GMVFLEX.6 — Optional final GMV9 Flex review archive / documentation checkpoint

or move directly to the next diagnostic/manual series after confirming priority.

Backlog candidates:
- ED-24UX.LAST — Improve /last display to preserve matched series/model label.
- ED-24OPS.CLEANUP — Move VPS env backups outside repo or ignore backup pattern safely.
