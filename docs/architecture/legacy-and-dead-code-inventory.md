# Legacy and Dead Code Inventory (P8-00)

## Purpose

Track potential legacy/dead-code cleanup candidates after P5/P6/P7 without removing any runtime behavior in this stage.

## Scope

- legacy code/test/documentation candidates;
- stale or transitional artifacts;
- consolidation/removal candidates for later approved stages.

## Categories

- `CandidateForReview`
- `CandidateForConsolidation`
- `CandidateForRemovalLater`
- `KeepCritical`
- `UnknownNeedsReview`

## Inventory

- EngineeringWorkflow API-named application contracts/services (`CandidateForConsolidation`): namespace cleanup candidate, not dead code.
- Route-inventory ignore-list entries (`CandidateForReview`): explicit deferred governance coverage that needs staged reduction.
- Long governance phrase assertions in architecture tests (`CandidateForConsolidation`): safety useful, but brittle/duplicated.
- EnergyPlus fixture authoring TODO placeholder metadata (`CandidateForReview`): provenance scaffolding incomplete.
- Ownership backfill governance docs/tests chain (`KeepCritical`): intentionally broad safety boundary; not dead code.

## Review rules

- No code/test/doc deletion in P8-00.
- Each candidate requires explicit stage and safety justification before removal.
- Runtime behavior and calculation physics are out of scope for this inventory.

## Non-claims

- No runtime behavior change claim.
- No calculation physics change claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No full tenant isolation claim.
- No production security certification claim.
