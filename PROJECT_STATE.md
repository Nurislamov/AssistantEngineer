# AssistantEngineer Project State

## Current stage

ED-24GEC — Gree equipment diagnostics knowledge expansion.

Current production status: PASS after GMV6 and GMV Mini stabilization.

Completed substages:
- ED-24GEC.11 — GMV6 manual import verification.
- ED-24GEC.12 — GMV Mini VRF manual import.
- ED-24GEC.12.1 — GMV Mini routing/search fix.
- ED-24GEC.12.2 — GMV Mini visible wording polish.

## Current branch

master

Latest confirmed commits:
```text
a8b281a3 ED-24GEC.12.2 Polish GMV Mini visible wording
4e53206b ED-24GEC.12.1 Fix GMV Mini routing and wording
8b864303 ED-24GEC.12 Import GMV Mini manual codes
4e43d185 ED-24GEC.11 Import missing GMV6 manual codes
```

## Last completed work

### GMV6

GMV6 diagnostics are closed against the service manual.

Source manual:
- Service Manual for GMV6 v_2020.09.
- manualId: gree-gmv6-service-manual-2020-09.
- document: GC202001-I.

Runtime result:
- GMV6 manual inventory: 255 codes.
- GMV6 runtime count: 255 codes.
- New missing GMV6 runtime JSON after full scan: 0.
- GMV6 status: closed.

Confirmed Telegram behavior:
- Gree FH / Gree GMV6 FH returns GMV6 FH.
- Gree GMV6 n2 returns GMV6 n2.
- Gree H0 returns GMV6 H0.
- Gree Ho / Gree HO routes to H0 with visual-code clarification.
- Gree n2 without series asks the user to choose GMV6 or GMV Mini.

### GMV Mini

GMV Mini VRF diagnostics are imported and production-tested.

Source manual:
- manualId: gree-gmv-mini-service-manual.
- file: SERVICE_MANUAL_GMV_MINI.pdf.
- title: DC Inverter VRF System Service Manual (R410A).

Runtime result:
- GMV Mini runtime count: 136 cards.
- Total Gree runtime count: 391 cards.
- GMV Mini routing/search: fixed.
- GMV Mini visible wording: polished.
- GMV Mini status: closed.

GMV Mini categories:
```text
Indoor/controller: 27
Outdoor/protection: 62
Status/debug: 47
```

Production Telegram checks passed for:
```text
Gree GMV Mini 01
Gree GMV Mini L3
Gree GMV Mini P1
Gree GMV Mini nC
Gree GMV Mini UE
Gree GMV Mini d3
Gree GMV Mini b1
Gree GMV Mini E0
Gree GMV Mini P0
Gree GMV Mini n2
Gree n2
```

Confirmed behavior:
- Explicit `Gree GMV Mini ...` and `Gree Mini ...` requests stay in GMV Mini.
- Mini-to-GMV6 fallback is blocked.
- `Gree n2` remains ambiguous and asks for GMV6 or GMV Mini.
- Mini visible text no longer contains mixed phrases like `Set master unit`, `neispravnost for`, `of outdoor`, `Water overf...`, or `driven board for`.

## Current blocker

No active production blocker for GMV6 or GMV Mini diagnostics.

Known technical debt:
- `PublishedApiAssemblyLoadsEmbeddedGreeH5` can hang during broad test filters. It should be fixed as a separate technical debt item.

## Important decisions

- GMV6 and GMV Mini are handled from their own service manuals, not from website cards alone.
- Gree website/support cards are secondary/reference evidence only.
- A code is added to a runtime series only when that series service manual confirms the code and meaning.
- GMV-W / Versati / U-Match / Multi Split / Chiller / FCU are not mixed into GMV6 or GMV Mini.
- `Ho` / `HO` is not a separate card; it is visual input routed to canonical `H0`.
- `E6` was not added to GMV Mini because the GMV Mini service manual did not confirm a precise E6 runtime entry.
- User-visible text must not mention internal process words such as `runtime`, `staging`, `support-catalog`, `raw`, `sourceMeaning`, or `machine translated`.
- Public documentation and UI must avoid claims like `pyBuildingEnergy parity` or exact EnergyPlus matching.

## Files changed recently

Key recent areas:
```text
data/equipment-diagnostics/error-knowledge/gree/gmv6/**
data/equipment-diagnostics/error-knowledge/gree/gmv-mini/**
data/equipment-diagnostics/error-knowledge/packages/**
data/equipment-diagnostics/manual-library/manuals.json
data/reference/gree-official-support-error-catalog/staging/**
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/**
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/**
tests/AssistantEngineer.Tests/EquipmentDiagnostics/**
.ae-tools/generate_gmv_mini_import_12.py
.ae-tools/polish_gmv_mini_visible_wording_12_2.py
```

Recent notable tests:
```text
GreeGmv6ManualImport11Tests
GreeGmvMiniManualImport12Tests
GreeGmvMiniRouting12_1Tests
GreeGmvMiniVisibleWording12_2Tests
EquipmentDiagnosticTelegramAdapterTests
ErrorKnowledgeJsonValidationTests
GreeGmvRemainingRuntimeCardsTests
```

## Validation status

Recent validation:
```text
ED-24GEC.11:
- GMV6 inventory/runtime: 255/255.
- Wide filter without known hanging smoke: 85/85 passed.
- EquipmentDiagnosticTelegram: 396/396 passed.
- ED-24GEC.11 targeted tests: 10/10 passed.

ED-24GEC.12:
- GMV Mini runtime: 136.
- Total Gree runtime: 391.
- ErrorKnowledgeJsonValidationTests + related Gree runtime tests: 86 passed.
- EquipmentDiagnosticTelegram: 396 passed.
- GreeGmvMiniManualImport12Tests: 3 passed.
- Targeted registry/manual coverage: 13 passed.

ED-24GEC.12.1:
- EquipmentDiagnosticTelegram: 396 passed.
- Mini/import/validation/runtime/wording set without known hanging smoke: 89 passed.
- GreeGmvMiniRouting12_1Tests + GreeGmvMiniManualImport12Tests: 30 passed.
- git diff --check: passed.

ED-24GEC.12.2:
- Narrow set: 101/101 passed.
- Required set 1: 426/426 passed.
- Required set 2 without known hanging smoke: 85/85 passed.
- git diff --check: passed.
```

Production Telegram smoke checks after deployment passed for GMV Mini routing and wording.

## Deployment status

Latest production deployment tested after ED-24GEC.12.2.

Deployment command used:
```bash
cd /opt/assistantengineer
git fetch origin
git reset --hard origin/master
docker compose --env-file ./deploy/.env -f ./deploy/docker-compose.yml up -d --build assistantengineer-api
```

Production bot behavior confirmed through Telegram screenshots.

## Next step

Recommended next steps:
1. Commit this `PROJECT_STATE.md` update.
2. Decide the next direction:
   - GMV-W / Versati manual import as a separate future stage;
   - or fix technical debt around `PublishedApiAssemblyLoadsEmbeddedGreeH5`;
   - or continue improving Telegram diagnostic UX after GMV6/GMV Mini knowledge base stabilization.
3. Do not mix GMV-W / Versati into GMV6 or GMV Mini.
