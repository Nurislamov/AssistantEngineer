# EnergyPlus / ASHRAE 140-style Validation Case Registry

## Purpose

This registry defines the future comparative validation backlog for Engineering Core V1.

Structured registry:

    docs/validation/EnergyPlusValidationCaseRegistry.json

Generated readiness report:

    docs/reports/EngineeringCoreV1ValidationReadiness.md

## Status

The registry status is PlannedValidation.

It is not exact EnergyPlus parity.

It is not ASHRAE 140 certification.

It is not required to close Engineering Core V1 formula gates.

## Included stages

| Stage | Meaning |
|---|---|
| Smoke | Small deterministic or placeholder reference cases used to validate direction and magnitude. |
| SimplifiedEnergyPlusComparison | Future simplified EnergyPlus comparison cases. |
| Ashrae140Style | Future ASHRAE 140-style sensitivity cases, not certification. |

## Initial cases

- EP-SMOKE-001: transmission-only heating smoke case.
- EP-SMOKE-002: solar cooling smoke case.
- EP-SMOKE-003: internal gains cooling smoke case.
- ASHRAE140-STYLE-001: lightweight vs heavyweight envelope sensitivity.
- ASHRAE140-STYLE-002: window orientation solar sensitivity.

## Required case metadata

Every case must include:

- caseId;
- name;
- stage;
- status;
- source;
- weatherSource;
- geometry;
- envelope;
- internalGains;
- ventilation;
- hvacControl;
- metrics;
- assumptions;
- knownDifferences;
- nonClaims.

## Required metric metadata

Every metric must include:

- metricId;
- name;
- unit;
- type;
- tolerancePercent;
- direction.

Allowed metric types:

- NumericWithinTolerance;
- DirectionalTrend;
- SameSign.

## Required non-claims

Every validation case must keep relevant non-claims visible:

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.

## Generation

Regenerate readiness report:

    .\scripts\engineering-core\generate-engineering-core-v1-validation-readiness.ps1

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationCaseRegistryTests"
