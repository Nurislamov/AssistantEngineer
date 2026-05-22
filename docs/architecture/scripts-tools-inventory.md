# Scripts and Tools Inventory (P8-06)

## Purpose

Classify scripts, tools, and CI workflow wrappers by operational role and risk boundary, without changing release/runtime semantics.

## Scope

- script wrappers versus C# tool commands;
- release-critical wrappers and tooling-critical commands;
- CI workflow wrapper surface;
- conversion/deprecation candidates for future stages.

## Categories

- `KeepAsWrapper`
- `ConvertToToolCandidate`
- `DeprecatedCandidate`
- `ReleaseGateCritical`
- `GeneratedArtifactProducer`
- `ToolingCritical`
- `UnknownNeedsReview`

## Classification notes

- Release-ready and profile scripts are stable wrappers and must preserve deterministic gate semantics.
- `AssistantEngineer.Tools.EngineeringCoreRelease` and `AssistantEngineer.Tools.OwnershipBackfill` remain critical governance tooling boundaries.
- Workflow YAML files are treated as wrapper contracts for CI visibility and release gate composition.
- Conversion/deprecation candidates are documented for later stages only; no scripts/tools are removed in P8-06.

## Current inventory summary

- `PowerShell` scripts reviewed: `68`
- `CSharpTool` projects reviewed: `13`
- `GitHubWorkflow` wrappers reviewed: `6`
- Total reviewed entries: `87`

Canonical machine-readable inventory: `docs/architecture/scripts-tools-inventory.json`.

## Non-claims

- No release gate semantics change claim.
- No runtime behavior change claim.
- No calculation physics change claim.
- No public API route change claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No DB row-level security claim.
- No global EF query filter claim.
- No full tenant isolation claim.
- No production security certification claim.
