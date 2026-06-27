# AssistantEngineer Project State

## Current Stage

ED-24GEC - Gree equipment diagnostics knowledge expansion.

Current production status: PASS after GMV6 fresh manual delta import.

Completed substages:
- ED-24GEC.11 - GMV6 manual import verification.
- ED-24GEC.12 - GMV Mini VRF manual import.
- ED-24GEC.12.1 - GMV Mini routing/search fix.
- ED-24GEC.12.2 - GMV Mini visible wording polish.
- ED-24GEC.13 - Inventory X series and 9 series Flex sources.
- ED-24GEC.13A - GMV6 fresh manual delta review/import, production PASS.

## Current Branch

master

Latest confirmed commits:
```text
b3bafc9c ED-24GEC.13A Import GMV6 fresh manual delta codes
17ae17ff ED-24GEC.13 Inventory X series and 9 series Flex sources
2c1b8253 ED-24TD.3 Fix stale full-test baseline failures
b16c2438 ED-24TD.2 Document full test baseline failures
2c6f7efd ED-24TD.1 Fix hanging published API embedded H5 test
```

## Last Completed Work

### GMV6 Fresh Manual Delta

ED-24GEC.13A is closed as production PASS.

Fresh manual:
- File: JF00304129, Export T1_R410A_GMV6 GMV Service Manual (Asia Pacific), D.2.pdf.
- Document: GC202203-IV.
- Fresh manual inventory: 263 codes.

Old compared manual:
- File: JF00304235, export T3_R410A_GMV6 GMV Service Manual (Saudi Arabia), B .3.pdf.
- Document: GC202005-I.
- Old manual inventory: 255 codes.

Imported GMV6 delta codes:
```text
A9
n1
qA
qC
qH
qP
qU
Uy
```

Runtime result after ED-24GEC.13A:
- GMV6 runtime count: 263 cards.
- GMV Mini runtime count: 136 cards.
- Total Gree runtime count: 399 cards.

GMV6 package counts after ED-24GEC.13A:
```text
Indoor package: 60
Outdoor package: 121
Status package: 44
Debugging package: 38
```

### GMV Mini

GMV Mini VRF diagnostics remain imported, production-tested, and unchanged by ED-24GEC.13A.

Source manual:
- manualId: gree-gmv-mini-service-manual.
- file: SERVICE_MANUAL_GMV_MINI.pdf.
- title: DC Inverter VRF System Service Manual (R410A).

Runtime result:
- GMV Mini runtime count: 136 cards.
- GMV Mini routing/search: fixed.
- GMV Mini visible wording: polished.
- GMV Mini status: closed.

GMV Mini categories:
```text
Indoor/controller: 27
Outdoor/protection: 62
Status/debug: 47
```

Confirmed behavior:
- Explicit `Gree GMV Mini ...` and `Gree Mini ...` requests stay in GMV Mini.
- Mini-to-GMV6 fallback is blocked.
- `Gree n2` remains ambiguous and asks for GMV6 or GMV Mini.
- Mini visible text no longer contains mixed phrases like `Set master unit`, `neispravnost for`, `of outdoor`, `Water overf...`, or `driven board for`.

## Current Blocker

No active production blocker for GMV6 or GMV Mini diagnostics.

## Important Decisions

- GMV6, GMV Mini, GMV X, and GMV9 Flex runtime packages must remain separate.
- Gree website/support cards are secondary/reference evidence only.
- A code is added to a runtime series only when that series service manual confirms the code and meaning.
- GMV-W / Versati / U-Match / Multi Split / Chiller / FCU are not mixed into GMV6, GMV Mini, GMV X, or GMV9 Flex.
- `Ho` / `HO` is not a separate card; it is visual input routed to canonical `H0`.
- `E6` was not added to GMV Mini because the GMV Mini service manual did not confirm a precise E6 runtime entry.
- User-visible text must not mention internal process words such as `runtime`, `staging`, `support-catalog`, `raw`, `sourceMeaning`, or `machine translated`.
- Public documentation and UI must avoid claims like `pyBuildingEnergy parity` or exact EnergyPlus matching.

## Files Changed Recently

Key recent areas:
```text
data/equipment-diagnostics/error-knowledge/gree/gmv6/**
data/equipment-diagnostics/error-knowledge/gree/gmv-mini/**
data/equipment-diagnostics/error-knowledge/packages/**
data/equipment-diagnostics/manual-library/manuals.json
data/reference/gree-official-support-error-catalog/staging/**
tests/AssistantEngineer.Tests/EquipmentDiagnostics/**
```

Recent notable tests:
```text
GreeGmv6FreshManualDelta13ATests
GreeGmv6ManualImport11Tests
GreeGmvMiniManualImport12Tests
GreeGmvMiniRouting12_1Tests
GreeGmvMiniVisibleWording12_2Tests
EquipmentDiagnosticTelegramAdapterTests
ErrorKnowledgeJsonValidationTests
GreeGmvRemainingRuntimeCardsTests
ManualCoverageRegistryTests
```

## Validation Status

Recent validation:
```text
ED-24GEC.13A:
- GreeGmv6FreshManualDelta13ATests: 10/10 passed.
- EquipmentDiagnosticTelegram: 396/396 passed.
- Focused regression pack: 118/118 passed.
- ManualCoverageRegistryTests: 8/8 passed.
- Full baseline dotnet test .\AssistantEngineer.sln: 4843/4843 passed.
- git diff --check: clean.
- Production Telegram smoke: PASS.

ED-24GEC.13:
- Inventory X series and 9 series Flex sources completed.
- GMV6, GMV Mini, GMV X, and GMV9 Flex runtime packages kept separate.

ED-24GEC.12.2:
- Narrow set: 101/101 passed.
- Required set 1: 426/426 passed.
- Required set 2 without known hanging smoke: 85/85 passed.
- git diff --check: passed.
```

Production Telegram smoke checks passed after ED-24GEC.13A.

## Deployment Status

Latest production baseline is master after:
```text
b3bafc9c ED-24GEC.13A Import GMV6 fresh manual delta codes
```

Production Telegram smoke: PASS.

## Next Step

Recommended next steps:
1. ED-24GEC.14 - GMV X service manual import.
2. Then run a separate stage for GMV9 Flex service manual import.
3. Do not mix GMV X, GMV9 Flex, GMV6, or GMV Mini runtime packages.
