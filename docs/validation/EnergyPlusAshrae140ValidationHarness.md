# EnergyPlus / ASHRAE 140 / BESTEST-style validation anchor Harness

## Purpose

This folder defines the offline validation harness structure for future EnergyPlus and ASHRAE 140-style validation.

The harness is fixture-based. It does not run EnergyPlus during normal unit tests.

The goal is to make future validation repeatable while keeping Engineering Core V1 claims honest.

## Current status

The current validation harness is a scaffold with smoke fixtures.

It is not full EnergyPlus comparison workflow.

It is not ASHRAE 140 certification.

It is not required for Engineering Core V1 formula closure.

## Validation approach

The validation harness compares AssistantEngineer outputs to committed reference values using documented tolerances.

The intended future workflow is:

1. Build a small EnergyPlus model.
2. Run EnergyPlus outside the unit-test suite.
3. Commit selected reference outputs as fixtures.
4. Compare AssistantEngineer results to those fixtures.
5. Document tolerances and known differences.
6. Keep non-claims visible.

## Initial smoke cases

The initial smoke cases are:

| Case id | Description |
|---|---|
| EP-SMOKE-001 | Single-zone transmission-only heating smoke case. |
| EP-SMOKE-002 | Single-zone solar cooling smoke case. |
| EP-SMOKE-003 | Single-zone internal gains cooling smoke case. |

## Metrics

Each validation metric includes:

- metric id;
- name;
- unit;
- AssistantEngineer value;
- reference value;
- tolerance percent;
- metric type;
- notes.

Metric types:

| Type | Meaning |
|---|---|
| NumericWithinTolerance | Compare numeric result within tolerance percent. |
| DirectionalTrend | Check that the result changes in the expected direction. |
| SameSign | Check that both values have the same sign. |

## Initial tolerances

Initial tolerances are intentionally conservative.

| Metric | Initial tolerance |
|---|---|
| Annual heating energy | 20 percent |
| Annual cooling energy | 20 percent |
| Peak heating load | 25 percent |
| Peak cooling load | 25 percent |
| Directional solar/internal gains response | directional only |

These tolerances must be reviewed when real EnergyPlus fixtures are committed.

## Required non-claims

Every validation case must keep these non-claims visible where applicable:

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.

## Relationship to Engineering Core V1

Engineering Core V1 is closed as an engineering formula gate.

This validation harness is a future validation layer.

The harness must not change FormulaAuditMatrix status from PlannedValidation to ClosedV1 without committed validation fixtures, documented tolerances and passing comparison tests.
