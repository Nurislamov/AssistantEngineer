# Engineering Core V1 Release Checklist

Use this checklist before tagging, merging or announcing Engineering Core V1.

## Required verification

Run:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Also run manifest-specific verification:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

Confirm:

- [ ] frontend build passes;
- [ ] backend tests pass;
- [ ] FormulaAuditMatrix has no Partial formula items;
- [ ] all ClosedV1 formula gates are listed in docs/releases/EngineeringCoreV1Manifest.json;
- [ ] EngineeringCoreStatusFacade exposes the same closed gates as the manifest;
- [ ] report disclosures expose warnings, assumptions, non-claims and out-of-scope items;
- [ ] frontend dashboard shows Engineering Core V1 status;
- [ ] frontend report UI shows calculationDisclosure before raw JSON;
- [ ] CI workflow exists and runs verification script.

## Required claims

Allowed:

- [ ] Engineering Core V1 is closed as an engineering formula gate.
- [ ] EPW/PVGIS weather import gates normalize to 8760 records.
- [ ] Annual hourly energy has a true 8760 scenario.
- [ ] Simplified ISO/EN-inspired modules are documented with limitations.
- [ ] EnergyPlus / ASHRAE 140 validation is planned.

Not allowed:

- [ ] exact EnergyPlus numerical parity;
- [ ] exact pyBuildingEnergy numerical parity;
- [ ] ASHRAE 140 validation coverage;
- [ ] full ISO 52016 node/matrix solver parity;
- [ ] full ISO 13370 implementation;
- [ ] full EN 15316 generation/distribution/storage/emission chain;
- [ ] latent/moisture/humidity support in v1.

## Annual energy release check

True hourly annual results may be described as true 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

Monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly annual simulation.

## Documentation check

Confirm these docs exist:

- docs/calculations/EngineeringCoreV1Scope.md
- docs/calculations/EngineeringCoreV1ReleaseNotes.md
- docs/calculations/EngineeringCoreV1ApiExamples.md
- docs/calculations/EngineeringCoreV1DeveloperGuide.md
- docs/calculations/EngineeringCoreV1VerificationRunbook.md
- docs/calculations/EnergyPlusAshrae140ValidationPlan.md
- docs/validation/EnergyPlusAshrae140ValidationHarness.md
- docs/frontend/EngineeringCoreV1StatusPanel.md
- docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md
- docs/ci/EngineeringCoreV1CI.md
- docs/contributing/EngineeringCoreV1ContributionGuide.md
- docs/releases/EngineeringCoreV1.md
- docs/releases/EngineeringCoreV1Manifest.json
- docs/releases/EngineeringCoreV1ReleaseManifest.md
- docs/releases/EngineeringCoreV1OwnerHandoff.md

## Final commands

    git status
    .\scripts\engineering-core\verify-engineering-core-v1.ps1
    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

If all checks pass, the release handoff is ready.
