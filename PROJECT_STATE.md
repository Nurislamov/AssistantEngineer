# AssistantEngineer Project State

## Current stage

ED-24BOT.1 CLOSED / merged to master / CI PASS

## Current branch

master

## Last completed work

ED-24BOT.1 completed the first Telegram handler pipeline architecture refactor and was merged to `master`.

Merge commit:

- 15fe2919 Merge pull request #48 from Nurislamov/ed-24bot-1-telegram-handler-pipeline

Included commits:

- 3c23b76b ED-24BOT.1 Introduce Telegram handler pipeline
- 3895e232 ED-24BOT.1a Allow Telegram pipeline architecture readiness test

Previous base before ED-24BOT.1:

- 979ff00f Fix Program composition root migration command boundary
- 99b8eef0 ED-24GMVX.17 Close production pass and fix review exporter

ED-24BOT.1 changed architecture only:

- `EquipmentDiagnosticTelegramAdapter` was reduced from a large coordinator/God-object into a thin pipeline runner.
- Added deterministic Telegram update handler pipeline.
- Extracted guard, callback, and message/diagnostic handling paths.
- Registered handlers in DI in fixed order.
- Added architecture tests to protect the pipeline shape.
- Added a targeted readiness-policy allow-path for the new Telegram pipeline architecture test.
- Preserved existing Telegram runtime behavior and expected outputs.

No runtime diagnostic content was changed in ED-24BOT.1:

- JSON diagnostic cards: unchanged
- Diagnostic catalogs: unchanged
- Manuals/manifests: unchanged
- Production env: unchanged
- EF migrations: unchanged
- Generated ZIP/PDF/manual binaries: unchanged

Previously closed production milestones remain closed:

- GMV6: CLOSED / production PASS
- GMV X: CLOSED / production PASS

GMV X closure summary:

- ED-24GMVX.16 closed the repository runtime catalog.
- ED-24GMVX.17 fixed the local GMV X review-bundle exporter plus script inventory metadata without changing runtime diagnostic cards.
- GMV X production deploy/smoke PASS.
- Manual GMV X archive review PASS.
- Runtime card count: 263/263.
- Outdoor: 121.
- Indoor: 60.
- Status: 44.
- Debugging: 38.

GMV6 closure summary:

- GMV6 remains CLOSED / production PASS.
- Production Telegram smoke confirmed real card CheckSteps are rendered.
- GMV6 titles are shown as Gree GMV6, not generic Gree GMV.
- Table-only/status/debugging cards stay short and safe.
- No visible manual/source/packageId/table-provenance wording leaks into Telegram.

## Current blocker

None.

## Important decisions

- Telegram bot is production-ready for the current scale.
- Do not keep expanding `EquipmentDiagnosticTelegramAdapter`.
- Keep the current monolith for now; do not introduce Redis, Webhook migration, Hangfire, or a separate Telegram service prematurely.
- Use KISS/SOLID inside the current monolith first.
- Telegram adapter must remain a thin coordinator/pipeline runner.
- New Telegram feature paths should be implemented through handlers/pipeline structure, not by growing the adapter again.
- Branch readiness guards are useful and should remain active.
- ED-24BOT.1a fixed a false positive in readiness policy using an exact allow-path only for `tests/AssistantEngineer.Tests/Architecture/TelegramHandlerPipelineArchitectureTests.cs`.
- The general forbidden Telegram guard remains active.
- Do not weaken diagnostic card evidence rules or branch scope guards just to pass CI.
- Public documentation and UI must not claim `pyBuildingEnergy parity` or exact EnergyPlus parity.
- Preferred public wording remains:
  - standard-based
  - standard-inspired
  - external reference validation
  - engineering-core validation
- For Telegram diagnostic cards, visible user-facing answers must not leak internal provenance wording such as:
  - Подтвердите код
  - Сверьте модель
  - по таблице
  - основание
  - руководство
  - manual
  - source
  - packageId
  - карточка неисправности
- PDF/manual binaries and generated review archives must not be committed.

## Files changed recently

ED-24BOT.1 / ED-24BOT.1a key files:

- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/EquipmentDiagnosticsModuleServiceCollectionExtensions.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramAdapter.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/ITelegramUpdateHandler.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/TelegramUpdateHandlerPipeline.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Handlers/TelegramUpdateGuardHandler.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Handlers/TelegramCallbackUpdateHandler.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Handlers/TelegramMessageUpdateHandler.cs
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Verification/BranchReadinessVerificationService.cs
- tests/AssistantEngineer.Tests/Architecture/TelegramHandlerPipelineArchitectureTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticsManualIntakePipelineTests.cs

Recent GMV X / GMV6 areas kept for context:

- data/equipment-diagnostics/error-knowledge/gree/gmv6/**
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramResponseFormatter.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeManualBoundCardRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramFormatterTests.cs
- docs/equipment-diagnostics/manual-bound-card-repair.md
- docs/equipment-diagnostics/manual-coverage.md
- docs/equipment-diagnostics/README.md
- scripts/equipment-diagnostics/invoke-gmv6-manual-bound-closure-inventory.ps1
- scripts/equipment-diagnostics/invoke-gmvx-manual-bound-closure-inventory.ps1
- scripts/equipment-diagnostics/export-gmvx-card-review-bundle.ps1
- docs/architecture/scripts-tools-inventory.json
- docs/architecture/scripts-tools-inventory.md
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXManualBoundInventoryTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXStatusPromptRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorSensorRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorEFRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorFRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorHRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorJPRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXIndoorDetailedBatch1RepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXStatusDebuggingTableOnlyRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXFinalClosureTests.cs

## Validation status

ED-24BOT.1 local validation before commit:

- Restore: PASS
- Build: PASS, 0 warnings / 0 errors
- Telegram focused suite: PASS, 534/534
- New architecture tests: PASS, 4/4
- Full backend suite: PASS, 5234/5234
- Engineering Core V1 verification: PASS
- git diff --check: PASS
- JSON/cards/catalogs/manuals/env/migrations: unchanged

ED-24BOT.1a local validation:

- Branch readiness: PASS, 0 blockers, 0 forbidden
- Policy tests: PASS, 45/45
- Restore: PASS
- Build: PASS, 0 warnings / 0 errors
- Full backend suite: PASS, 5236/5236
- Engineering Core V1 verification: PASS
- git diff --check: PASS
- Runtime code, cards, JSON catalogs, manuals, manifests, env and migrations: unchanged

ED-24BOT.1 PR / CI validation:

- PR: #48
- GitHub Actions: PASS, 10/10 checks successful
- EquipmentDiagnostics Branch Readiness: PASS
- Deployment Dry Run: PASS
- Engineering Core V1 Contracts: PASS
- Engineering Core V1 Smoke: PASS
- Engineering Core V1 verification: PASS
- Engineering Core V1 Validation matrix: PASS
- ISO52016 Matrix release-ready: PASS

ED-24GMVX.17 validation retained for historical context:

- GMV X review-bundle exporter: PASS
- 263 cards exported
- Counts: outdoor 121 / indoor 60 / status 44 / debugging 38
- Parse check: PASS
- Count check: PASS
- Pattern flags: 0
- Engineering Core V1 verification: PASS
- Full backend suite: PASS, 5229/5229
- dotnet restore: PASS
- dotnet build: PASS, 6 existing nullable warnings / 0 errors
- EquipmentDiagnostics filter: PASS, 1205/1205
- Telegram filter: PASS, 646/646

Production validation retained for historical context:

- GMV6 production deploy/smoke: PASS
- GMV X production deploy/smoke: PASS
- Telegram polling: PASS
- Manual GMV X archive review: PASS, 263/263 cards
- Migrations: none
- Env changes: none
- PDF/manual binaries committed: none
- Generated ZIP/manual-review artifacts committed: none

## Next step

Recommended next stages:

1. ED-24CI.1 — Stabilize branch readiness diagnostics.
   - Make readiness failures easier to understand directly from CI console output.
   - Print blocker code, file, reason, and suggested fix without requiring manual inspection of a huge JSON report.
   - Keep guards strict; improve diagnostics rather than weakening policy.

2. ED-24GMVFLEX.1 — GMV Flex / GMV9 Flex inventory.
   - Start in a separate branch.
   - Inventory first, runtime cards later.
   - Do not mix Flex/Mini/LCAC in one branch.

3. ED-24GMVMINI.1 — GMV Mini inventory.
   - Separate branch after Flex inventory or after CI diagnostics stabilization.

Keep GMV6 and GMV X closed and out of scope unless a production regression appears.
