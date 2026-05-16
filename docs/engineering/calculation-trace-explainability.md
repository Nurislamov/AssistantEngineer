# Calculation Trace Explainability

## Purpose

This foundation defines an explainable engineering calculation trace model that communicates how a result is composed from component contributions, assumptions, excluded effects, and diagnostics references.

## Scope

This scope covers:

- room heating and cooling decomposition traces;
- ventilation and infiltration contribution traces;
- solar gains contribution traces;
- ground boundary contribution traces;
- domestic hot water contribution traces;
- system energy chain contribution traces;
- machine-readable trace artifacts for future report and UI integration.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Trace principles

- Trace explains component composition of results.
- Trace is not the source of calculation truth.
- Trace must not recompute production physics differently from production outputs.
- Trace may perform arithmetic consistency checks for explainability diagnostics.
- Trace must declare assumptions used for interpretation.
- Trace must declare excluded effects and reasons.
- Trace lines must use explicit units.
- Trace must preserve references to diagnostics and input quality findings.

## Trace section model

- `EngineeringCalculationTrace`: root object with trace scope, subject identity, calculation type, sections, assumptions, exclusions, diagnostic references, and metadata.
- `EngineeringCalculationTraceSection`: explainability section such as transmission, ventilation, infiltration, solar, ground, final load, assumptions, diagnostics.
- `EngineeringCalculationTraceLine`: explainability line with label, optional formula, optional inputs, explicit unit, value, explanation, and source.
- `EngineeringCalculationTraceAssumption`: assumption entry with status and optional registry reference.
- `EngineeringCalculationTraceExcludedEffect`: effect intentionally excluded from the represented chain.
- `EngineeringCalculationTraceDiagnosticReference`: diagnostic link preserved from validation or input quality layers.

## Relationship to input quality

Trace may include diagnostic references from `docs/engineering/input-quality-checks.md`, including input readiness warnings such as missing ventilation configuration or missing climate context.

## Relationship to assumptions registry

Trace assumptions should reference `docs/engineering/engineering-assumptions-registry.md` where assumption identifiers and status governance are maintained.

## Relationship to units governance

Trace lines and metadata must follow `docs/engineering/units-governance.md` and `docs/engineering/units-registry.json` for explicit units and conversion consistency.

## Relationship to observability diagnostics policy

Trace generation and lifecycle diagnostics should follow `docs/architecture/observability-diagnostics-policy.md` and use event taxonomy from `docs/architecture/observability-diagnostic-events.json`.

## Future integration

- Future API endpoint can expose explainability traces.
- Future report sections can include trace appendix blocks.
- Future UI trace viewer can render sections, formulas, assumptions, exclusions, and diagnostics.

This step is intentionally foundation-only and does not change existing public API routes or existing calculation outputs.
