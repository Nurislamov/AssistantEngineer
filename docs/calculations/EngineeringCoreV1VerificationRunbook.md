# Engineering Core V1 Verification Runbook

## Purpose

This runbook defines the standard verification command for Engineering Core V1.

The verification script checks the backend, frontend and documentation guards that protect the Engineering Core V1 closure claims.

## Main command

From the repository root:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Recommended full local gate before merge/release:

    dotnet restore AssistantEngineer.sln
    dotnet build AssistantEngineer.sln --no-restore
    dotnet test AssistantEngineer.sln
    npm --prefix .\src\Frontend ci
    npm --prefix .\src\Frontend run build
    .\scripts\engineering-core\verify-engineering-core-v1.ps1
    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

The command runs:

- frontend TypeScript/Vite build;
- formula audit tests;
- Engineering Core V1 status tests;
- report disclosure tests;
- documentation guard tests;
- frontend visibility guard tests;
- EPW/PVGIS 8760 weather gate tests;
- annual true hourly 8760 gate tests;
- simplified hourly heat-balance tests;
- single thermal zone tests;
- simplified ground tests;
- simplified adjacent-zone tests;
- EnergyPlus/ASHRAE 140 validation harness guard tests;
- full backend test suite.

## Fast mode

For a faster local check:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Fast mode skips the final full backend test suite, but still runs the engineering-core-focused filters.

## Skip frontend build (emergency override only)

When working on backend-only machines without frontend dependencies:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -SkipFrontend

Use this only as temporary local fallback. The normal engineering gate runs without this flag, and release readiness must be validated with frontend checks enabled.

## Skip full dotnet suite

For focused checks:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -SkipFullDotnet

This runs the filtered engineering-core tests but skips the final full backend suite.

## What this verifies

The script verifies that:

- Engineering Core V1 formula gates remain ClosedV1;
- no FormulaAuditMatrix formula items remain Partial;
- validation flow does not allow successful results with Error diagnostics;
- EPW and PVGIS weather import gates normalize to 8760 records;
- annual energy has a true hourly 8760 scenario;
- hourly heat-balance and single-zone gates remain closed;
- simplified ground and adjacent-zone gates remain closed;
- status endpoint/facade exposes ClosedV1 and non-claims;
- heating/cooling reports expose calculationDisclosure;
- frontend displays Engineering Core V1 status and report disclosures;
- docs keep ISO, EnergyPlus, ASHRAE 140 and pyBuildingEnergy non-claims visible;
- EnergyPlus/ASHRAE 140 validation remains a future comparative harness, not a v1 parity claim.

## Required before merge

Before merging calculation-core changes, run:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Then confirm:

    git status

## Expected success output

The script ends with:

    Engineering Core V1 verification completed successfully.

If any step fails, fix the failing test/build before committing.

## Related documents

- docs/calculations/EngineeringCoreV1Scope.md
- docs/calculations/EngineeringCoreV1ReleaseNotes.md
- docs/calculations/EngineeringCoreV1ApiExamples.md
- docs/calculations/EngineeringCoreV1DeveloperGuide.md
- docs/calculations/EnergyPlusAshrae140ValidationPlan.md
- docs/validation/EnergyPlusAshrae140ValidationHarness.md
- docs/frontend/EngineeringCoreV1StatusPanel.md
- docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md
- docs/releases/EngineeringCoreV1.md
