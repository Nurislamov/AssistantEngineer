# Engineering Core V1 Contribution Guide

## Purpose

This guide defines the contribution rules for Engineering Core V1.

Engineering Core V1 is closed as an engineering formula gate. Future changes must preserve diagnostics, disclosures, documentation and non-claims.

## Pull request checklist

Every pull request that touches calculation-core behavior must use:

    .github/pull_request_template.md

Before review, run:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

For local pre-checks:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

## Formula changes

When adding or changing a formula:

1. update production engine/service;
2. document units;
3. add diagnostics;
4. add unit tests;
5. add edge-case tests;
6. update FormulaAuditMatrix;
7. update docs if scope or limitations change;
8. update report/API/frontend disclosure if user-visible output changes.

## Diagnostics rule

A successful calculation result must not contain CalculationDiagnosticSeverity.Error.

Use:

- Error for invalid mandatory input;
- Warning for fallback, simplification or missing optional assumption;
- Info for method/source/metadata.

## Required non-claims

Do not introduce claims of:

- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence;
- full ISO 13370 implementation;
- full EN 15316 system chain;
- detailed HVAC plant simulation;
- latent/moisture/humidity support in v1.

## 8760 rule

Annual energy can be described as true hourly 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

Monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly annual simulation.

## Reporting and frontend visibility

If a calculation result reaches users, ensure that warnings, assumptions, explicit non-claims and out-of-scope items remain visible.

Relevant frontend documents:

    docs/frontend/EngineeringCoreV1StatusPanel.md
    docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md
    docs/frontend/EngineeringCoreV1FrontendIntegrationGuard.md

Relevant report/API contract:

    AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common.CalculationDisclosure

## EnergyPlus / ASHRAE 140 / BESTEST-style validation anchor

Validation cases must be comparative with documented tolerances.

Use:

    .github/ISSUE_TEMPLATE/energyplus-validation-case.yml
    docs/validation/EnergyPlusValidationCaseTemplate.md
    docs/validation/EnergyPlusAshrae140ValidationHarness.md

Validation must not be described as exact watt-by-watt equivalence.

## Issue templates

Use:

    .github/ISSUE_TEMPLATE/engineering-core-formula.yml
    .github/ISSUE_TEMPLATE/energyplus-validation-case.yml

## CI

Engineering Core V1 CI is defined in:

    .github/workflows/engineering-core-v1.yml

The local equivalent is:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1
