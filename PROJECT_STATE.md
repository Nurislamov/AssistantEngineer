# AssistantEngineer Project State

## Current stage

ED-24GMVX.17 CLOSED / production PASS

## Current branch

master

## Last completed work

GMV X is CLOSED / production PASS. ED-24GMVX.16 closed the repository runtime catalog, production deploy/smoke
validated that runtime commit, and ED-24GMVX.17 fixed the local GMV X review-bundle exporter plus script inventory
metadata without changing runtime diagnostic cards. GMV6 remains CLOSED / production PASS.

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

None for ED-24GMVX.17.

## Important decisions

- GMV6 is now considered CLOSED after full local validation, archive review, production deploy, and Telegram smoke.
- GMV6 remains CLOSED / production PASS and is not part of the GMV X inventory stage.
- GMV X is CLOSED / production PASS after the ED-24GMVX.16 runtime commit was deployed and smoked from master.
- ED-24GMVX.17 is exporter/documentation/project-state only; it does not require a new production restart because runtime diagnostic cards did not change.
- ED-24GMVX.15 discovered that `LL` has its own GMV X troubleshooting section and repaired it as detailed hydro-box / water-flow-switch diagnostics rather than table-only text.
- ED-24GMVX.16 keeps final status/debugging table-only cards as statuses, settings, service-process indications, or debugging indications; possible causes remain empty and no detailed troubleshooting procedure is invented.
- sourceNote remains non-rendered by Telegram; visible Telegram output uses card title, summary, causes, check steps, recommended action, and safety text.
- Telegram formatter now prefers localized card CheckSteps; CompactChecks is only a fallback when a card has no check steps.
- db keeps its existing package-compatible metadata boundary, but visible text and guards keep it as debugging/status wording rather than a normal fault.
- Production did not require migrations or environment changes.
- PDF/manual binaries were not committed.
- Generated GMV X review archives and verification artifacts remain ignored/outside Git.

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

Local final validation after ED-24SRC.16a:

- dotnet build: PASS, 0 warnings / 0 errors
- EquipmentDiagnostics: PASS, 1141/1141
- Telegram: PASS, 645/645
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests: PASS, 10/10
- Full suite: PASS, 5165/5165
- Branch readiness: PASS, 0 blockers
- Runtime counts unchanged: Gree 1296; GMV6 263 = 121 outdoor / 60 indoor / 44 status / 38 debugging

ED-24GMVX.16 inventory snapshot:

- GMV X total: 263
- GMV X outdoor: 121
- GMV X indoor: 60
- GMV X status: 44
- GMV X debugging: 38
- AlreadyRepaired: 263
- DetailedProcedureAvailable: 0
- StatusOrPrompt: 0
- TableOnlySafe: 0
- ManualSectionNeedsReview: 0
- Conflict: 0
- Unclassified: 0
- GMV X CLOSED: yes

Completed in ED-24GMVX.16:

- Safe status table-only repair: `A9`, `AL`, `An`, `Ay`, `n1`, `n3`, `n5`, `nb`, `nJ`, `nn`, `nU`, `qA`, `qC`, `qH`, `qP`, `qU`.
- Safe debugging table-only repair: `C1`, `C7`, `CU`, `U5`, `Ud`, `Un`, `Uy`.
- The 23 final cards retain exact Error Indication meanings with empty causes and safe service handoff.
- No GMV6, GMV6 HR, GMV Mini, GMV9 Flex, U-Match R32, or ERV B Series cards were changed.

ED-24GMVX.16 local validation:

- dotnet restore .\AssistantEngineer.sln: PASS
- dotnet build .\AssistantEngineer.sln --no-restore: PASS, 6 existing nullable warnings / 0 errors
- GMV X inventory runner: PASS, 263 rows; AlreadyRepaired 263 / DetailedProcedureAvailable 0 / TableOnlySafe 0 / ManualSectionNeedsReview 0; StatusOrPrompt 0; GMV X CLOSED = yes
- EquipmentDiagnostics filter: PASS, 1205/1205
- Telegram filter: PASS, 646/646
- Webhook filter: PASS, 10/10
- Full suite: PASS, 5229/5229
- git diff --check: PASS

ED-24GMVX.17 local and CI-equivalent validation:

- GMV X review-bundle exporter: PASS; 263 cards exported, counts outdoor 121 / indoor 60 / status 44 / debugging 38, parse check PASS, count check PASS, pattern flags 0.
- Exporter ZIP generated outside Git for manual review at `C:\Users\user\Downloads\gmvx-card-review-fixed\gmvx-card-review-20260704-130737-14ca9056.zip`.
- Engineering Core V1 verification: PASS; exact `scripts\engineering-core\verify-engineering-core-v1.ps1` completed successfully with full backend suite 5229/5229.
- Additional full backend test run with hang diagnostics: PASS, 5229/5229.
- dotnet restore .\AssistantEngineer.sln: PASS.
- dotnet build .\AssistantEngineer.sln --no-restore: PASS, 6 existing nullable warnings / 0 errors.
- EquipmentDiagnostics filter: PASS, 1205/1205.
- Telegram filter: PASS, 646/646.

Final local GMV6 archive review after ED-24SRC.16a:

- HEAD: 759fa53fa5ddce02d53f6bad3433b213e374a5ec
- GMV6 JSON: 263
- Russian text rows: 789
- Flagged rows: 0
- Inventory: gmv6Closed = true
- RepairClass: AlreadyRepaired = 263

Production validation:

- VPS deployment performed from master for ED-24GMVX.16 runtime commit `14ca9056`.
- Telegram polling: PASS.
- Telegram smoke: PASS for the selected detailed, table/status, indoor, status, and debugging GMV X samples.
- Manual GMV X archive review: PASS, 263/263 cards; outdoor 121 / indoor 60 / status 44 / debugging 38.
- Known non-blocking UX follow-up: long detailed steps in E1/E2-style cards can still be visually long in Telegram; no diagnostic correctness issue remains.

Migrations/env/artifacts:

- Migrations: none
- Env changes: none
- PDF/manual binaries committed: none
- Runtime diagnostic card changes in ED-24GMVX.17: none
- Generated ZIP/manual-review artifacts committed: none

## Next step

Recommended next stage:

- No blocking GMV X closure work remains. Optional next stage: monitor CI after the ED-24GMVX.17 exporter/doc/state commit reaches `origin/master`.

Keep GMV6 closed and out of scope.
