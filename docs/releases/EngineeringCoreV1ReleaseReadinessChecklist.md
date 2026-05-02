# Engineering Core V1 Release Readiness Checklist

Use this checklist only after smoke/contracts profiles are green.

## Commands

- [ ] .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1
- [ ] .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1
- [ ] .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1
- [ ] .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1
- [ ] .\scripts\engineering-core\verify-engineering-core-v1.ps1
- [ ] dotnet test .\AssistantEngineer.sln
- [ ] git status

## Core closure

- [ ] Engineering Core V1 status is ClosedV1.
- [ ] FormulaAuditMatrix has no unclosed v1 formula gates.
- [ ] Manifest closedFormulaGates matches FormulaAuditMatrix ClosedV1 entries.
- [ ] Traceability matrix matches manifest, diagnostics catalog and validation registry.
- [ ] Diagnostics catalog exposes Error / Warning / Info rules.
- [ ] Successful results must not contain CalculationDiagnosticSeverity.Error.

## Weather and annual energy

- [ ] EPW 8760 gate is closed.
- [ ] PVGIS 8760 gate is closed.
- [ ] Annual true hourly 8760 gate is closed.
- [ ] True hourly annual claim requires EnergyDataSource = TrueHourlySimulation.
- [ ] True hourly annual claim requires IsTrueHourly8760 = true.
- [ ] True hourly annual claim requires HourlyRecordCount = 8760.

## User visibility

- [ ] Status endpoint exposes ClosedV1, formula gates and non-claims.
- [ ] Diagnostics catalog endpoint exposes userMessage and userAction.
- [ ] Heating/cooling reports expose calculationDisclosure.
- [ ] Frontend dashboard shows Engineering Core status.
- [ ] Frontend dashboard shows diagnostics catalog.
- [ ] Frontend report UI shows calculationDisclosure before raw JSON.
- [ ] Report/export disclosure policy exists.

## Generated contracts

- [ ] API contract snapshots exist.
- [ ] OpenAPI fragment exists.
- [ ] Postman collection exists.
- [ ] Report contract snapshots exist.
- [ ] Release evidence report exists.
- [ ] Validation readiness report exists.
- [ ] Traceability matrix exists.

## Future validation

- [ ] EnergyPlus / ASHRAE 140 validation remains PlannedValidation.
- [ ] Validation registry exists.
- [ ] Validation cases include tolerances, metrics, assumptions and known differences.
- [ ] Validation non-claims remain visible.

## Required non-claims

- [ ] No exact EnergyPlus numerical parity claim.
- [ ] No exact pyBuildingEnergy numerical parity claim.
- [ ] No ASHRAE 140 validation coverage claim.
- [ ] No full ISO 52016 node/matrix solver parity claim.
- [ ] No full ISO 13370 implementation claim.
- [ ] No full EN 15316 system chain claim.
- [ ] No latent/moisture/humidity support in v1.

## Decision

Engineering Core V1 may be declared closed when every required checkbox above is satisfied.

Approved status wording:

    Engineering Core V1 is closed as an engineering formula gate with documented limitations.

Forbidden status wording:

    EnergyPlus parity achieved.
    ASHRAE 140 validated.
    Full ISO 52016 implemented.
