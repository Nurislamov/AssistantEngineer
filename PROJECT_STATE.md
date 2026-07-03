# AssistantEngineer Project State

## Current stage

ED-24GMVX.12 CLOSED / manual-section review PASS

## Current branch

master

## Last completed work

GMV X manual-section review was resolved for the six ED-24GMVX.12 cards. GMV6 remains CLOSED / production PASS.

Final GMV6 closure / production commits:

- 85818153 ED-24SRC.14c Close GMV6 manual-bound diagnostic cards
- 1b5bb1bd ED-24SRC.15 Show manual-bound GMV6 check steps in Telegram
- 704a59b ED-24SRC.16 Fix GMV6 review wording findings
- 759fa53f ED-24SRC.16a Polish final GMV6 visible wording

Production Telegram smoke verified these GMV6 codes:

- Gree GMV6 b1
- Gree GMV6 E1
- Gree GMV6 E2
- Gree GMV6 J8
- Gree GMV6 P0
- Gree GMV6 L1
- Gree GMV6 d3
- Gree GMV6 db
- Gree GMV6 AJ
- Gree GMV6 U0

The smoke confirmed:

- real card CheckSteps are rendered in Telegram instead of the old generic fallback;
- GMV6 titles are shown as Gree GMV6, not generic Gree GMV;
- detailed cards show manual-bound diagnostic checks;
- table-only/status/debugging cards stay short and safe;
- AJ is a filter-clean reminder;
- db and U0 are shown as status/debugging or commissioning information, not standalone faults;
- no visible manual / source / packageId / table-provenance wording leaks into Telegram.

## Current blocker

None for ED-24GMVX.12.

## Important decisions

- GMV6 is now considered CLOSED after full local validation, archive review, production deploy, and Telegram smoke.
- GMV6 remains CLOSED / production PASS and is not part of the GMV X inventory stage.
- GMV X is NOT CLOSED. ED-24GMVX.12 resolves all manual-review cards; 92 table-only cards remain open.
- sourceNote remains non-rendered by Telegram; visible Telegram output uses card title, summary, causes, check steps, recommended action, and safety text.
- Telegram formatter now prefers localized card CheckSteps; CompactChecks is only a fallback when a card has no check steps.
- db keeps its existing package-compatible metadata boundary, but visible text and guards keep it as debugging/status wording rather than a normal fault.
- Production did not require migrations or environment changes.
- PDF/manual binaries were not committed.

## Files changed recently

Key recent areas:

- data/equipment-diagnostics/error-knowledge/gree/gmv6/**
- src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramResponseFormatter.cs
- 	ests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeManualBoundCardRepairTests.cs
- 	ests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramFormatterTests.cs
- docs/equipment-diagnostics/manual-bound-card-repair.md
- docs/equipment-diagnostics/manual-coverage.md
- docs/equipment-diagnostics/README.md
- scripts/equipment-diagnostics/invoke-gmv6-manual-bound-closure-inventory.ps1
- scripts/equipment-diagnostics/invoke-gmvx-manual-bound-closure-inventory.ps1
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXManualBoundInventoryTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXStatusPromptRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorSensorRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorEFRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorFRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorHRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXOutdoorJPRepairTests.cs
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvXIndoorDetailedBatch1RepairTests.cs

## Validation status

Local final validation after ED-24SRC.16a:

- dotnet build: PASS, 0 warnings / 0 errors
- EquipmentDiagnostics: PASS, 1141/1141
- Telegram: PASS, 645/645
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests: PASS, 10/10
- Full suite: PASS, 5165/5165
- Branch readiness: PASS, 0 blockers
- Runtime counts unchanged: Gree 1296; GMV6 263 = 121 outdoor / 60 indoor / 44 status / 38 debugging

ED-24GMVX.12 inventory snapshot:

- GMV X total: 263
- GMV X outdoor: 121
- GMV X indoor: 60
- GMV X status: 44
- GMV X debugging: 38
- AlreadyRepaired: 171
- DetailedProcedureAvailable: 0
- StatusOrPrompt: 0
- TableOnlySafe: 92
- ManualSectionNeedsReview: 0
- Conflict: 0
- Unclassified: 0
- GMV X CLOSED: no

Completed in ED-24GMVX.12:

- Six GMV X manual-section review cards resolved: `d5`, `d8`, `dE`, `L2`, `L6`, `LH`.
- `d5`, `d8`, `dE`, `L2`, and `LH` are reserved/not-applied headings with no inferred causes or diagnostic flow.
- `L6` is a non-fault mode-conflict condition with the documented compatible-mode action.
- No GMV6, GMV6 HR, GMV Mini, GMV9 Flex, U-Match R32, or ERV B Series cards were changed.

ED-24GMVX.12 local validation:

- dotnet restore .\AssistantEngineer.sln: PASS
- dotnet build .\AssistantEngineer.sln --no-restore: PASS, 0 warnings / 0 errors
- GMV X inventory runner: PASS, 263 rows; AlreadyRepaired 171 / DetailedProcedureAvailable 0 / TableOnlySafe 92 / ManualSectionNeedsReview 0; StatusOrPrompt 0; GMV X CLOSED = no
- EquipmentDiagnostics filter: PASS, 1185/1185
- Telegram filter: PASS, 646/646
- Webhook filter: PASS, 10/10
- Full suite: PASS, 5209/5209
- git diff --check: PASS

Final local GMV6 archive review after ED-24SRC.16a:

- HEAD: 759fa53fa5ddce02d53f6bad3433b213e374a5ec
- GMV6 JSON: 263
- Russian text rows: 789
- Flagged rows: 0
- Inventory: gmv6Closed = true
- RepairClass: AlreadyRepaired = 263

Production validation:

- VPS deployment performed from master.
- Telegram smoke: PASS for the selected detailed, table/status, indoor, status, and debugging samples.
- Known non-blocking UX follow-up: long detailed steps in E1/E2-style cards can still be visually long in Telegram; no diagnostic correctness issue remains.

Migrations/env/artifacts:

- Migrations: none
- Env changes: none
- PDF/manual binaries committed: none

## Next step

Recommended next stage:

- ED-24GMVX.13 repair GMV X table-only diagnostics batch 1.

Do not attempt full GMV X repair in one commit. Keep GMV6 closed and out of scope.
