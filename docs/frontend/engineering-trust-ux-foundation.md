# Engineering Trust UX Foundation

## Purpose

This document defines the frontend foundation for engineering transparency in workflow UX.

## Scope

This step adds:

- input quality summary panel;
- calculation trace summary panel;
- assumptions summary panel;
- validation readiness panel;
- combined trust overview panel;
- conservative placeholder view models when backend fields are not available.

This step does not redesign the workflow shell and does not add backend API routes.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No third-party tool equivalence claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Panels

- `EngineeringInputQualitySummaryPanel`
- `EngineeringCalculationTraceSummaryPanel`
- `EngineeringAssumptionsSummaryPanel`
- `EngineeringValidationReadinessPanel`
- `EngineeringTrustOverviewPanel`

## Data source strategy

- Reuse existing workflow shell diagnostics and trace summary fields if present.
- Build trust view models in frontend from currently available state.
- Avoid new API calls when endpoints are not available.
- Mark unavailable data as unavailable instead of inferring unverified status.

## Current limitations

- Input quality details are derived from existing workflow diagnostics, not from a dedicated input-quality endpoint payload.
- Assumptions summary is conservative and may remain "not connected" without explicit metadata linkage.
- Trace panel shows summary availability and counts; it is not a full trace explorer.
- Validation readiness flags represent foundation coverage, not certification.

## Future backend API integration

- Add dedicated workflow trust endpoint(s) for input quality, assumptions, and trace metadata.
- Link frontend view models to backend observability and trace artifacts.
- Surface correlation identifiers and diagnostic event references when contracts are available.

## Future trace viewer

- Add detailed trace drill-down UI for sections, formulas, assumptions, and excluded effects.
- Add artifact-backed trace document viewer with pagination/filtering.

## Future validation dashboard

- Add manual fixture coverage status and tolerance comparison summaries from backend.
- Add validation history and trend visualization.
- Keep non-claims visible and explicit in all validation UX surfaces.

