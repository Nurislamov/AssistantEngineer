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

- `PowerShell` scripts reviewed: `100`
- `CSharpTool` projects reviewed: `14`
- `GitHubWorkflow` wrappers reviewed: `8`
- Total reviewed entries: `122`

ED-19C adds two provider-neutral operations scripts for sanitized incident log collection and offline redaction.
They produce ignored local artifacts only and must never persist raw log input.

ED-20A adds one thin EquipmentDiagnostics wrapper for deterministic closed-beta readiness reports under ignored artifacts.

ED-21A adds committed goal-protocol governance templates under `docs/engineering-workflow/`. They are documentation, not scripts, tools, generated reports, or runtime automation, so inventory counts remain unchanged.

ED-21B extends the existing EquipmentDiagnostics verification tool with the deterministic `goal-run-report` command and adds its committed validator documentation/schema references.

ED-22A adds one secret-free local runner for ignored Telegram closed-beta release evidence packs.

ED-22B adds committed Telegram closed-beta release-candidate, operator-limitation, and smoke-matrix documentation. It changes no script/tool counts or runtime behavior.

ED-22C adds one secret-free local deployment activation dry-run runner that produces ignored review evidence without Docker, Telegram network calls, webhook execution, or activation.

ED-22D adds one secret-free local activation-checklist generator plus committed manual activation and sanitized smoke-evidence guidance. It performs no activation or network operation.

ED-22E adds one secret-free local final go/no-go evidence generator plus committed decision and placeholder-only handoff guidance. It performs no activation, deployment, or network operation.

ED-22F adds one secret-free local release tag/handoff checklist generator plus committed manual annotated-tag and placeholder-only release-notes guidance. It creates and pushes no tag and performs no activation or network operation.

ED-SEC.1 adds one local production secret rotation candidate generator. It writes ignored artifacts by default, does not read or edit `deploy/.env`, and must not be used to paste generated secrets into chats/issues/logs.

ED-24OPS.1 adds one secret-free, offline wrapper for focused Gree diagnostics smoke tests. It invokes the existing test and Telegram adapter infrastructure without calling Telegram or changing runtime data.

ED-24SRC.1 adds one ignored-artifact audit producer and one bounded GMV6 visible-wording repair utility. Neither script
calls external services, changes runtime counts, or imports manual binaries.

ED-24SRC.3 adds one ignored-artifact GMV6 closure inventory producer. It reads runtime JSON and package metadata, writes
planning reports under `artifacts/verification/equipment-diagnostics/`, and does not edit diagnostic cards.

ED-24GMVX.1 adds one ignored-artifact GMV X closure inventory producer. It reads runtime JSON and package metadata,
writes planning reports under `artifacts/verification/equipment-diagnostics/`, and does not edit diagnostic cards.

ED-24GMVX.17 adds one ignored-artifact GMV X review-bundle exporter. It reads runtime JSON, validates GMV X counts,
renders review markdown/CSV reports, writes ZIP bundles outside Git by operator choice, and does not edit diagnostic cards.

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
