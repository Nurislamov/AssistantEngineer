# Engineering Core V1 Owner Handoff

## What has been closed

Engineering Core V1 is closed as an engineering formula gate.

The closed scope includes:

- design-point heating and cooling load;
- transmission;
- ventilation and infiltration sensible loads;
- internal sensible gains;
- window solar gains;
- solar position and isotropic surface irradiance;
- room/floor/building aggregation;
- single thermal zone path;
- simplified hourly heat balance;
- EPW/PVGIS 8760 weather import;
- annual true hourly 8760 integration;
- simplified ground heat transfer;
- simplified adjacent boundary handling;
- simplified DHW;
- simplified system final/primary energy;
- equipment capacity sizing.

## Where to look first

Start with:

    docs/releases/EngineeringCoreV1Manifest.json
    docs/releases/EngineeringCoreV1ReleaseManifest.md
    docs/calculations/EngineeringCoreV1Scope.md
    docs/calculations/EngineeringCoreV1ReleaseNotes.md
    docs/calculations/EngineeringCoreV1DeveloperGuide.md

## How to verify health

Run:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

For manifest consistency:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

CI should also run:

    .github/workflows/engineering-core-v1.yml

## Public/API status

Application status endpoint:

    GET /api/v1/calculations/engineering-core/v1/status

It exposes:

- ClosedV1 status;
- formula gates;
- explicit non-claims;
- out-of-scope v1 items;
- planned validation;
- annual 8760 requirements;
- documentation files.

## Frontend visibility

Frontend users should see:

- Engineering Core V1 status on dashboard;
- calculationDisclosure in report UI before raw JSON;
- warnings;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items;
- documentation references.

Key frontend files:

    src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx
    src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx
    src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx
    src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx

## Do not claim

Do not claim:

- exact EnergyPlus parity;
- exact pyBuildingEnergy parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in v1.

## Next recommended work

Recommended next work:

1. add first real EnergyPlus reference fixture;
2. add validation report generator;
3. add frontend validation-case display;
4. add report export templates that include calculationDisclosure;
5. plan latent/moisture psychrometrics as a future module;
6. plan equipment performance curves / part-load modeling as a future module.

## Escalation checklist

If a future change breaks Engineering Core V1 verification:

1. inspect failing test name;
2. check whether FormulaAuditMatrix changed;
3. check whether diagnostics Error/Warning/Info rules changed;
4. check whether status endpoint still matches manifest;
5. check whether frontend still renders status/disclosure;
6. check whether documentation non-claims are still visible;
7. rerun .\scripts\engineering-core\verify-engineering-core-v1.ps1.
