# Terminology And Claims Vocabulary (P8-07)

## Purpose

Define one canonical terminology and claims vocabulary for engineering/security/governance artifacts so wording stays precise and non-claims stay explicit.

## Scope

This vocabulary applies to:

- architecture docs in `docs/architecture`;
- security governance docs in `docs/security`;
- ADR wording for boundary and claims statements;
- governance tests that assert claims wording.

## Non-claims

- No calculation physics change claim.
- No public API route change claim.
- No DTO shape change claim.
- No authorization behavior change claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No DB row-level security claim.
- No global EF query filter claim.
- No production security certification claim.

## Engineering terminology

- Prefer `governance-ready` over broad `production-ready` when scope is docs/tests/governance.
- Prefer `behavior-lock characterization` for tests that freeze existing behavior before refactor.
- Prefer `refactor-only` for internal decomposition with unchanged runtime contracts.

## Calculation validation terminology

- Prefer `validation anchors` for manual/reference comparison fixtures.
- Prefer `reference-informed` for methodology influence without numerical equivalence claims.
- Prefer `manual engineering validation fixtures` for curated scenario sets.

## Reference-method terminology

- Prefer `pyBuildingEnergy-style methodology reference`.
- Prefer `EnergyPlus-style naming fixture` only for naming-style traceability.
- Prefer `ASHRAE/BESTEST-style anchor boundary` only in non-claim context.

## Security and tenant terminology

- Prefer `tenant-aware read integration for selected paths`.
- Prefer `route protection is options-controlled`.
- Prefer `staged rollout` and explicit `protectionStage` metadata.

## Ownership backfill terminology

- Prefer `write-path intentionally disabled`.
- Prefer `dry-run/evidence/signoff/readiness capable`.
- Prefer `apply mode designed but disabled`.

## Allowed claims

- `reference-informed`
- `pyBuildingEnergy-style methodology reference`
- `validation anchors`
- `manual engineering validation fixtures`
- `governance-ready`
- `write-path intentionally disabled`
- `tenant-aware read integration for selected paths`
- `route protection is options-controlled`
- `dry-run/evidence/signoff/readiness capable`

## Forbidden claims

- `full pyBuildingEnergy parity`
- `EnergyPlus parity`
- `ASHRAE 140 validated`
- `ISO certified`
- `full tenant isolation`
- `production security certified`
- `SOC2 compliant`
- `ISO27001 compliant`
- `ownership backfill executed`
- `production apply enabled`
- `DB RLS enabled`
- `global EF query filters enabled`

## Preferred wording

- Use `no-claim` phrasing explicitly (for example: `No production security certification claim.`).
- Use `staged` and `options-controlled` for rollout behavior.
- Use `selected paths` instead of blanket phrases like `all endpoints`.

## Deprecated wording

- `parity` when used as a positive capability claim.
- `certified`/`compliant` without formal certification evidence.
- `full isolation` when current boundary is staged/partial.
- `apply enabled` when apply boundary is intentionally disabled.

## Examples

- Preferred: `Route protection is options-controlled and staged.`
- Preferred: `Validation anchors are reference-informed and not parity claims.`
- Forbidden: `EnergyPlus parity is achieved.`
- Forbidden: `Production apply enabled and ownership backfill executed.`
