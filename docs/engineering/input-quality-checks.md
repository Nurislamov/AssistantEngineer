# Input Quality Checks

## Purpose

The Input Quality Check layer provides engineering-grade input diagnostics before simulation or sizing runs.
It complements hard validation by reporting warnings, missing context, suspicious values, and readiness signals.

## Scope

This layer covers:

- building-level input quality checks;
- room-level input quality checks;
- envelope and geometry sanity checks;
- ventilation and setpoint completeness checks;
- assumptions/default usage diagnostics;
- readiness diagnostics for engineering workflow orchestration.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Difference between validation and input quality

`BuildingInputValidationService` remains the hard validation and correction layer.
The Input Quality Check layer does not replace it and does not mutate source data.
Input quality diagnostics are advisory readiness diagnostics with severity grading.

## Severity model

- `Info`: informational signal; no readiness impact.
- `Warning`: suspicious or incomplete input; calculation can proceed with caution.
- `Error`: invalid input that may compromise result quality; not considered ready.
- `Blocking`: missing/invalid critical input; calculation is not ready.

## Diagnostic categories

- Geometry
- Envelope
- Ventilation
- InternalGains
- Solar
- Ground
- Weather
- DomesticHotWater
- SystemEnergy
- Units
- Assumptions
- CalculationReadiness

## Diagnostic code list

- `IQ-BLD-001`: Building not found.
- `IQ-BLD-010`: Building has no floors.
- `IQ-BLD-011`: Building has no rooms.
- `IQ-BLD-020`: Missing climate zone/context.
- `IQ-BLD-030`: Ground-contact boundaries without explicit ground metadata.
- `IQ-ROOM-001`: Room not found.
- `IQ-ROOM-010`: Invalid room area.
- `IQ-ROOM-011`: Invalid room height.
- `IQ-ROOM-012`: Invalid room volume.
- `IQ-ROOM-020`: Missing envelope data.
- `IQ-ROOM-030`: Suspicious window-to-floor area ratio.
- `IQ-ROOM-040`: Missing ventilation configuration.
- `IQ-ROOM-041`: Invalid ventilation airflow.
- `IQ-ROOM-050`: Invalid U-value.
- `IQ-ROOM-051`: Suspicious U-value.
- `IQ-ROOM-060`: Invalid SHGC.
- `IQ-ROOM-070`: Missing/defaulted setpoints.
- `IQ-ROOM-080`: Invalid people count.
- `IQ-ASSUMP-001`: Calculation preference defaults used.
- `IQ-UNITS-001`: Unit-bearing field requires explicit unit documentation.

## Calculation readiness interpretation

- `IsCalculationReady = true` when there are no `Error` or `Blocking` diagnostics.
- `HasWarnings = true` means engineering review is recommended before trust-sensitive decisions.
- `HasBlockingIssues = true` means input repair is required before calculation execution.

## Relationship to assumptions registry

Assumption-related diagnostics should align with `docs/engineering/engineering-assumptions-registry.md`.
When defaults are used, quality diagnostics should point to assumptions that need explicit ownership or audit.

## Relationship to units governance

Unit-related diagnostics should align with `docs/engineering/units-governance.md`.
Where possible, diagnostic fields and metadata should use unit-explicit naming consistent with `docs/engineering/units-registry.json`.

## Relationship to observability diagnostics policy

Input quality execution logs and event codes should align with `docs/architecture/observability-diagnostics-policy.md` and `docs/architecture/observability-diagnostic-events.json`.

## Future UI usage

A future API endpoint may expose input quality reports for workflow readiness dashboards.
A future report section may include input quality summary and unresolved warnings.
This step is intentionally service-layer and governance-only, with no public route changes.
