# Release-ready Observability Audit

## Purpose

This audit documents release-ready observability and performance diagnostics for the Engineering Core V1 release-ready gate, without changing release gate semantics.

## Scope

- `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1` wrapper behavior;
- `tools/AssistantEngineer.Tools.EngineeringCoreRelease` orchestration diagnostics;
- stage timing and summary visibility;
- timeout-risk visibility and failure diagnostics;
- safe logging/redaction posture.

## Non-claims

- No production security certification claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No full multi-tenant isolation claim yet.
- No DB row-level security claim.
- No global EF query filter claim.
- No certified/certification claim.

## Current release-ready flow

- Release-ready PowerShell wrapper delegates orchestration to `AssistantEngineer.Tools.EngineeringCoreRelease`.
- Orchestration runs restore/build/regeneration/smoke/contracts/manifest/full verification/git status stages.
- Stage completion status and duration are printed.

## Observability gaps

- Stage-level start/end UTC and command context were not consistently visible for every stage.
- Failure output did not always provide a deterministic failed-stage diagnostics block.
- Machine-readable summary output was not available for automated analysis of slow stages.

## Performance hotspots

- Full verification stage dominates runtime.
- Frontend build repeats across profiles and contributes measurable overhead.
- Artifact regeneration chain and profile fan-out produce large total wall-clock duration.

## Timeout risks

- Long-running release-ready execution can approach CI timeout thresholds, especially when full verification is enabled.
- Lack of machine-readable stage summary made it harder to profile recurring slow spots over time.
- Child-process orchestration is synchronous; if a child process blocks, progress diagnosis depends on console output cadence.

## Safe diagnostics policy

- No secret or connection-string values should be emitted in release-ready diagnostics.
- Failure diagnostics must expose stage/command/exit-code/duration without payload leakage.
- Diagnostics must not hide or downgrade gate failures.

## Proposed improvements

- Add deterministic stage summary table including stage/status/exit-code/duration/command.
- Add stage start/end UTC diagnostics per stage (default on, optional quiet mode).
- Add explicit failure diagnostics block with hint to relevant artifacts/log context.
- Add optional machine-readable summary JSON output for local/CI profiling.

## Implemented improvements

- Added stage start/end UTC and stage exit code output in orchestration stage runner.
- Added deterministic stage summary table in release-ready summary.
- Added failure diagnostics block: failed stage, command, exit code, duration, diagnostics hint.
- Added optional `--output-summary-json <path>` pipeline and wrapper passthrough.
- Added `--quiet-stages` wrapper/tool passthrough to reduce stage timestamp verbosity for local diagnostics only.
- Added safe command diagnostics redaction for connection-string-like tokens.

## Remaining limitations

- No built-in kill-timeout policy for child processes (intentional to avoid changing gate semantics).
- Runtime still depends on full verification coverage; no default skip behavior added.
- Stage summary JSON is optional and caller-managed.

## CI visibility relationship

- Release-ready observability output is an input for CI checks visibility operations and triage.
- P7-05 defines the GitHub checks visibility contract and runbook that consume release-ready summary/timing diagnostics.
- Observability improvements here do not assert that GitHub checks are always present for every commit context.

## Next steps

- P7-05: CI/GitHub checks visibility with surfaced stage-duration trends from release-ready runs.
- P8-06: scripts/tools rationalization keeps release-critical wrapper boundaries explicit without changing gate semantics.
- P9-00: validation-roadmap refresh uses these observability artifacts as planning evidence only and does not imply formal validation completion.
- P9-01: ISO52016 decomposition review uses observability evidence as architecture-planning input only and does not imply formula, expected-value, or release-boundary changes.
- P9-03: fixture provenance cleanup classifies evidence strength and planned placeholders; it does not modify release-ready gate semantics or expected numerical values.
