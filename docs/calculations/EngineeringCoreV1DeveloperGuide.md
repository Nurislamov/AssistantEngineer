# Engineering Core V1 Developer Guide

## Purpose

This guide explains how to maintain Engineering Core V1 without accidentally weakening the calculation guarantees or making false parity claims.

Engineering Core V1 is closed as an engineering formula gate, not as an exact external simulation parity gate.

## Main source of truth

The main source of truth for formula readiness is:

    tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/FormulaAudit/FormulaAuditMatrix.cs

Supporting guards:

    tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/FormulaAudit/FormulaAuditMatrixTests.cs
    tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/FormulaAudit/EngineeringCoreV1ReadinessGuardTests.cs
    tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/FormulaAudit/EngineeringCoreV1ScopeDocumentationTests.cs
    tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/FormulaAudit/EngineeringCoreV1ReleaseDocumentationTests.cs

## When adding a new calculation formula

When adding a new formula, add or update:

1. production service/engine;
2. DTO/input/output contracts;
3. diagnostics;
4. unit tests;
5. edge-case tests;
6. FormulaAuditMatrix entry;
7. report/API disclosure if the result reaches users.

A formula should not be marked ClosedV1 unless:

- the formula is implemented;
- units are documented;
- diagnostics are available;
- invalid mandatory inputs fail;
- tests cover normal and edge cases;
- limitations are explicitly documented.

## Diagnostics rule

Do not return successful calculation results containing CalculationDiagnosticSeverity.Error.

Use:

- Error for invalid mandatory input;
- Warning for fallback, simplification, missing optional assumptions and partial sources;
- Info for method/source/metadata.

Examples:

| Situation | Severity |
|---|---|
| negative mandatory area | Error |
| invalid efficiency <= 0 | Error |
| missing optional COP/efficiency | Warning |
| monthly adapter instead of true 8760 | Warning |
| synthetic weather | Warning |
| true hourly simulation source used | Info |

## Naming rule for ISO/EN-inspired modules

If a class, DTO or result uses standard-like wording such as Iso52016, Iso13370, EN15316 or EN12831, it must clearly state whether it is simplified/inspired.

Correct wording:

- ISO52016-inspired simplified hourly heat-balance model;
- ISO13370-inspired simplified ground model;
- EN15316-inspired simplified system energy model;
- EN12831-3-inspired simplified DHW demand model.

Forbidden wording unless separately validated:

- full ISO 52016 implementation;
- full ISO 13370 implementation;
- no EnergyPlus parity claim;
- no ASHRAE 140 covered claim;
- no pyBuildingEnergy parity claim;
- no ExternalParityCovered claim.

## FormulaAuditMatrix status meanings

| Status | Meaning |
|---|---|
| ClosedV1 | Formula gate is closed for engineering-core v1 with documented limitations. |
| OutOfScopeV1 | Intentionally excluded from engineering-core v1. |
| PlannedValidation | Future validation layer, not production formula gate. |
| Partial | Not allowed after Engineering Core V1 release unless a new future formula is being actively developed and guarded. |

After Engineering Core V1 release, the matrix must not contain remaining Partial items for the closed formula set.

## Weather requirements

EPW and PVGIS weather imports must normalize to 8760 hourly records.

Required behavior:

- 8760 output for non-leap annual profile;
- leap-day normalization where applicable;
- Jan-Dec chronological ordering;
- invalid/missing solar radiation normalized safely;
- incomplete profiles rejected.

## Annual energy requirements

Annual hourly energy integration is closed only for true hourly records.

True annual 8760 requires:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

Monthly adapter and synthetic weather may exist for compatibility and diagnostics, but they must not be shown as true hourly annual simulation.

## Report/API disclosure requirements

When calculation results reach the user, the response/report must expose:

- core status;
- calculation scope;
- calculation method;
- actual method;
- warnings;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items;
- documentation files.

Current report disclosure contract:

    AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common.CalculationDisclosure

Current status endpoint:

    GET /api/v1/calculations/engineering-core/v1/status

## Future validation

EnergyPlus / ASHRAE 140 validation is planned, but not required to close Engineering Core V1 formulas.

Future validation must be comparative with documented tolerances, not exact watt-by-watt parity.

See:

    docs/calculations/EnergyPlusAshrae140ValidationPlan.md

## Pre-commit checklist

Before committing calculation changes:

    dotnet test .\AssistantEngineer.sln --filter "FormulaAudit|EngineeringCoreV1"
    dotnet test .\AssistantEngineer.sln

Also check:

    git status

Recommended commit style:

- Close formula/gate engineering core v1
- Expose status/disclosure/diagnostics in layer
- Document scope/validation/non-claims
