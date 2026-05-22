# Terminology Claims Surface Cleanup (P8-07)

## Purpose

Reduce ambiguous terminology and claim-risk wording across architecture/security/governance artifacts by applying a canonical vocabulary.

## Scope

- docs claims/terminology review for architecture/security/governance surfaces;
- guardrail test alignment for allowed/forbidden claims;
- no runtime/API/calculation behavior changes.

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

## Scan coverage

- `docs/architecture`
- `docs/security`
- `docs/adr`
- `tests/AssistantEngineer.Tests/Architecture`
- selected wording checks under `src` and `tools` for claim surfaces.

## Claims corrected

- Added canonical allowed/forbidden claims vocabulary in `docs/architecture/terminology-and-claims-vocabulary.md/.json`.
- Added explicit P8-07 references in security governance index, release boundary linkage, and architecture map.
- Normalized P8-07 stage tracking in readiness inventory and guardrails catalog.

## Claims intentionally retained as non-claims

- No full pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validated claim.
- No ISO/security certification claim.
- No full tenant isolation claim.
- No ownership backfill executed / production apply enabled claim.

## Terminology normalized

- Prefer `reference-informed` and `validation anchors`.
- Prefer `write-path intentionally disabled` and `governance-ready`.
- Prefer `tenant-aware read integration for selected paths`.

## Code and test naming candidates

- Potential follow-up candidate: rename remaining legacy wording fragments that use broad `production-ready` phrasing where scope is governance-only.
- Potential follow-up candidate: reduce wording-coupled duplication in legacy claim checks during P8-08.

## Deferred renames

- Public API/DTO naming renames are deferred (out of P8-07 scope).
- Broad source-level naming churn with compatibility risk is deferred to dedicated refactor stages.

## Risk assessment

- Main risk remains wording drift across a large governance document set.
- Canonical vocabulary plus dedicated tests reduces risk of accidental overclaim statements.

## Verification

- Build and test suite validation.
- Release-ready script validation.
- Ownership-backfill apply-disabled validation with redaction check.

## Next steps

- P8-08 governance-test brittleness reduction phase 2.
