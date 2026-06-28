# AssistantEngineer Project State

## Current Stage

ED-24GEC - Gree equipment diagnostics knowledge expansion.

Current production status: PASS after GMV X diagnostics import and stabilization.

Completed substages:
- ED-24GEC.11 - GMV6 manual import verification.
- ED-24GEC.12 - GMV Mini VRF manual import.
- ED-24GEC.12.1 - GMV Mini routing/search fix.
- ED-24GEC.12.2 - GMV Mini visible wording polish.
- ED-24GEC.13 - Inventory X series and 9 series Flex sources.
- ED-24GEC.13A - GMV6 fresh manual delta review/import, production PASS.
- ED-24GEC.14 - GMV X service manual import.
- ED-24GEC.14.1 - GMV X visible wording encoding corruption fix.
- ED-24GEC.14.2 - GMV X visible wording grammar polish.

## Current Branch

master

Latest confirmed commits:
```text
ede84516 ED-24GEC.14.2 Polish GMV X visible wording grammar
99f73ef0 ED-24GEC.14.1 Fix GMV X visible wording encoding
f04b6fe5 ED-24GEC.14 Import GMV X manual codes
f42d7c1b Update project state after GMV6 fresh manual delta
b3bafc9c ED-24GEC.13A Import GMV6 fresh manual delta codes
```

## Last Completed Work

### GMV X Service Manual Import And Stabilization

ED-24GEC.14 is closed as production PASS.

Source manual:
- File: JF00305173, Outlet T1R410A50 & 60HzGMVX Heat Pump GMV Service Manual, A.3.pdf.
- Document: GC202209-I.

Imported GMV X runtime:
- Total: 263 cards.
- Indoor: 60 cards.
- Outdoor: 121 cards.
- Debugging: 38 cards.
- Status: 44 cards.

ED-24GEC.14.1 is closed: GMV X visible wording encoding corruption fixed.

Root cause:
- ED-24GEC.14 generated Russian visible fields through a lossy encoding path, so Cyrillic in GMV X `texts[]` became question marks.

Fix result:
- 263 GMV X runtime JSON files repaired.
- No `???` remains in GMV X visible fields.
- Counts unchanged.

ED-24GEC.14.2 is closed: GMV X visible wording grammar polished.

Fixed phrases:
```text
Код относится к наружного блока -> Код относится к наружному блоку
Код относится к внутреннего блока -> Код относится к внутреннему блоку
формулировка относится к наружного блока -> формулировка относится к наружному блоку
формулировка относится к внутреннего блока -> формулировка относится к внутреннему блоку
```

Production smoke after ED-24GEC.14.2:
- `Gree GMV X E0` works and uses readable Russian.
- `Gree GMV X d9` works and uses readable Russian.
- `Gree GMV X qP` works and uses readable Russian.
- `Gree GMV X n2` works and uses readable Russian.
- `Gree n2` ambiguity remains GMV6 / GMV Mini.
- GMV Mini selection after ambiguity still works.

Runtime result after ED-24GEC.14.2:
- GMV6 runtime count: 263 cards.
- GMV Mini runtime count: 136 cards.
- GMV X runtime count: 263 cards.
- Total Gree runtime count: 662 cards.

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

No active production blocker for GMV6, GMV Mini, or GMV X diagnostics.

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
data/equipment-diagnostics/error-knowledge/gree/gmv-x/**
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
GreeGmvXImport14Tests
GreeGmvXVisibleWording14_1Tests
EquipmentDiagnosticTelegramAdapterTests
ErrorKnowledgeJsonValidationTests
GreeGmvRemainingRuntimeCardsTests
ManualCoverageRegistryTests
```

## Validation Status

Recent validation:
```text
ED-24GEC.14.2:
- GMV X focused pack: 32/32 passed.
- EquipmentDiagnosticTelegram: 396/396 passed.
- JSON/GMV6/GMV Mini regression pack: 92/92 passed.
- Full baseline dotnet test .\AssistantEngineer.sln: 4875/4875 passed.
- git diff --check: clean.
- Production Telegram smoke: PASS.

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

Production Telegram smoke checks passed after ED-24GEC.14.2.

## Deployment Status

Latest production baseline is master after:
```text
ede84516 ED-24GEC.14.2 Polish GMV X visible wording grammar
```

Production Telegram smoke: PASS.

## Next Step

Recommended next steps:
1. ED-24GEC.15 - GMV9 Flex service manual import.
2. Keep GMV9 Flex as a separate runtime series/package.
3. Do not mix GMV9 Flex with GMV6, GMV Mini, or GMV X runtime packages.
