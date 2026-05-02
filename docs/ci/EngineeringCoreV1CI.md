# Engineering Core V1 CI

## Purpose

The Engineering Core V1 CI workflow protects the closed calculation-core gates in pull requests and branch pushes.

Workflow file:

    .github/workflows/engineering-core-v1.yml

Main verification command:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

## What CI verifies

The workflow verifies:

- frontend TypeScript/Vite build;
- backend restore;
- formula audit matrix;
- Engineering Core V1 status endpoint/facade;
- report disclosures;
- frontend visibility guards;
- EPW/PVGIS 8760 weather gates;
- annual true hourly 8760 gate;
- simplified hourly heat-balance gate;
- single thermal zone gate;
- simplified ground gate;
- simplified adjacent-zone gate;
- EnergyPlus/ASHRAE 140 validation harness scaffold;
- scope/release/developer/frontend documentation guards;
- full backend test suite.

## Trigger policy

The workflow runs on:

- pull requests;
- pushes to main/master/develop/Energy_Calculation_Parity;
- manual workflow_dispatch.

## Tooling

The workflow uses:

- windows-latest runner;
- actions/checkout@v4;
- actions/setup-dotnet@v4;
- .NET 10 SDK;
- actions/setup-node@v4;
- Node.js 22;
- npm ci for frontend dependencies;
- Engineering Core V1 verification PowerShell script.

## Why this exists

Engineering Core V1 is closed as an engineering formula gate.

The CI workflow prevents accidental regression in:

- ClosedV1 formula-gate status;
- diagnostics rules;
- 8760 weather/annual assumptions;
- frontend visibility of warnings/non-claims;
- report disclosure visibility;
- future EnergyPlus/ASHRAE 140 validation non-claims.

## Non-claims

Passing this CI workflow does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full detailed HVAC plant simulation.

## Local equivalent

Before pushing calculation-core work, run:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

For a faster local pre-check:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

## Expected result

The workflow should end with:

    Engineering Core V1 verification completed successfully.

If CI fails, fix the failing build/test/documentation guard before merging.

## CI profiles

Additional profile workflows are documented in:

    docs/ci/EngineeringCoreV1CIProfiles.md

Profile workflows:

    .github/workflows/engineering-core-v1-smoke.yml
    .github/workflows/engineering-core-v1-contracts.yml
    .github/workflows/engineering-core-v1-release-ready.yml
