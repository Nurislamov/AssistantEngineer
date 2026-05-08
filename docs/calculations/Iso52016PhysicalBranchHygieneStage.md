# AE-ISO52016-002 Step 14 - physical branch hygiene stage

This stage adds a repository hygiene gate around the ISO52016 physical model chain.

## Purpose

The previous physical-chain work introduced a large set of C# contracts, builders, diagnostics, services, manifests, verification wrappers, and release gates. During integration with `origin/master`, the most fragile failure mode was not calculation logic. It was branch hygiene:

- rebase still in progress;
- conflict markers left in source/docs/scripts/json;
- invalid JSON after conflict resolution;
- temporary `ae-iso52016-*.ps1` patch scripts accidentally left in the repository root;
- release gates executed while the repository was in an unresolved git state.

Step 14 introduces a C# owned hygiene tool and keeps PowerShell as a thin wrapper.

## Added tool

- `tools/AssistantEngineer.Tools.RepositoryHygieneVerification`

The tool checks:

- no git rebase/merge is in progress;
- required ISO52016 physical chain files exist;
- tracked text/json/script/source files do not contain git conflict markers;
- JSON files parse successfully;
- optional clean working tree check;
- optional root patch script check.

## Wrappers

- `scripts/iso52016/assert-iso52016-physical-branch-hygiene.ps1`
- `scripts/iso52016/verify-iso52016-physical-branch-hygiene-stage.ps1`

The wrappers are intentionally thin. Durable checks live in C#.

## Claim boundary

This is a repository hygiene and verification orchestration stage with validation/internal engineering anchors only.

It is not a new solver, not complete ISO 52016 numerical equivalence, not StandardReference numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 validation.

No generated artifacts are introduced by this step.