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

- `PowerShell` scripts reviewed: `89`
- `CSharpTool` projects reviewed: `14`
- `GitHubWorkflow` wrappers reviewed: `8`
- Total reviewed entries: `111`

ED-19C adds two provider-neutral operations scripts for sanitized incident log collection and offline redaction.
They produce ignored local artifacts only and must never persist raw log input.

ED-20A adds one thin EquipmentDiagnostics wrapper for deterministic closed-beta readiness reports under ignored artifacts.

ED-21A adds committed goal-protocol governance templates under `docs/engineering-workflow/`. They are documentation, not scripts, tools, generated reports, or runtime automation, so inventory counts remain unchanged.

ED-21B extends the existing EquipmentDiagnostics verification tool with the deterministic `goal-run-report` command and adds its committed validator documentation/schema references.

ED-22A adds one secret-free local runner for ignored Telegram closed-beta release evidence packs.

ED-22B adds committed Telegram closed-beta release-candidate, operator-limitation, and smoke-matrix documentation. It changes no script/tool counts or runtime behavior.

ED-22C adds one secret-free local deployment activation dry-run runner that produces ignored review evidence without Docker, Telegram network calls, webhook execution, or activation.

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
