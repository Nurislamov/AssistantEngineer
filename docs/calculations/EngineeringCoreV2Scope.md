# Engineering Core V2 Scope

## Position

AssistantEngineer Engineering Core V2 is an internal engineering stage that extends the standalone C# standard-based engineering calculation platform with deterministic multi-zone capabilities.

This scope is an internal engineering anchor and validation anchor.

## Supported in this stage

- multiple thermal zones in one coupled hourly solve;
- inter-zone conductance links between thermal zones;
- adjacent unconditioned boundary temperature coupling;
- same-use adiabatic-style adjacent boundary behavior;
- zone-level hourly heating/cooling needs;
- building-level hourly and annual/monthly heating/cooling summaries;
- deterministic fixture-based verification and stage traceability.
- opt-in EN15316-style useful-energy handoff from pipeline annual useful demand into circuit-level system-energy calculation.

## Not claimed in this stage

- no full ISO52016 compliance claim;
- no external validation coverage claim;
- no full airflow network coupling claim;
- no moisture/latent coupling claim;
- no detailed HVAC plant coupling claim.
- no full EN15316 compliance claim.

## Integration boundary

- Existing single-zone engineering behavior remains unchanged.
- Multi-zone capabilities are provided as a standard-based multi-zone calculation path with explicit non-claims.
- External tools, when referenced in the project, remain independent comparison workflows only.
