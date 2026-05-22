# Scripts/Tools Rationalization (P8-06)

## Purpose

Rationalize scripts/tools wrapper surface and document safe boundaries for future simplification without changing release/runtime behavior.

## Scope

- classify `scripts/` wrappers, `AssistantEngineer.Tools.*` projects, and CI workflow wrappers;
- identify wrapper/tool duplication patterns;
- document keep/migrate/deprecate candidates for future stages;
- keep release-ready and CI semantics unchanged.

## Non-claims

- No release gate semantics change claim.
- No calculation physics change claim.
- No public API route change claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No DB row-level security claim.
- No global EF query filter claim.
- No production security certification claim.

## Current scripts/tools surface

- Reviewed scripts: `68`
- Reviewed tools: `13`
- Reviewed workflows: `6`
- Total reviewed entries: `87`

## Release-critical wrappers

- `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`
- `scripts/engineering-core/verify-engineering-core-v1.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-smoke.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-contracts.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-validation.ps1`
- `scripts/iso52016/assert-iso52016-matrix-release-ready.ps1`
- `.github/workflows/engineering-core-v1-release-ready.yml`
- `.github/workflows/iso52016-matrix-release-ready.yml`

## Tooling-critical C# tools

- `tools/AssistantEngineer.Tools.EngineeringCoreRelease/AssistantEngineer.Tools.EngineeringCoreRelease.csproj`
- `tools/AssistantEngineer.Tools.EngineeringCoreVerification/AssistantEngineer.Tools.EngineeringCoreVerification.csproj`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/AssistantEngineer.Tools.OwnershipBackfill.csproj`
- `tools/AssistantEngineer.Tools.Iso52016Verification/AssistantEngineer.Tools.Iso52016Verification.csproj`

## Wrapper/tool duplication findings

- Engineering Core wrappers are intentionally thin and delegate execution semantics to C# tools.
- Validation and artifact-generation wrappers repeat orchestration entrypoints but remain useful as stable operator/CI interfaces.
- ISO52016 wrappers are mostly wrapper-only invocations with low logic duplication risk.

## Candidates for future migration

- `scripts/engineering-core/new-energyplus-validation-fixture.ps1` (toward richer typed tool command).
- `tools/AssistantEngineer.Tools.EnergyPlusFixtureAuthoring/AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj` (policy/helper extraction).

## Candidates for future deprecation review

- No immediate deprecation candidates were marked safe-to-remove in P8-06.
- Any deprecation requires replacement path + CI/workflow impact validation.

## CI/release-ready impact

- No workflow trigger semantics changed.
- No release-ready wrapper default behavior changed.
- No CI gate was removed or weakened.

## Safe wrapper policy

- Keep wrappers stable when they are CI/operator contract boundaries.
- Treat tool command-line contracts as compatibility surfaces.
- Allow future migration only behind characterization/coverage tests.

## Remaining limitations

- Some wrappers still use overlapping orchestration entrypoints by design.
- Further convergence requires staged migration to avoid CI/release drift.

## Next steps

- P8-07 terminology and claims-surface cleanup.
