# AE-ISO52016-002 Step 08 - physical model chain release-ready gate

This stage adds a release-ready gate for the ISO52016-inspired physical room/zone model chain.

## Scope

Step 08 does not add new calculation physics and does not replace the existing Matrix solver.

It adds a guarded release gate around the physical model chain built in the previous steps:

- Step 01 physical node model builder contracts and three-node fallback;
- Step 02 explicit surface/construction expansion;
- Step 03 per-surface hourly boundary profiles;
- Step 04 operation profiles and Matrix boundary conductance overrides;
- Step 05 physical room simulation service adapter;
- Step 06 physical diagnostics profile;
- Step 07 C# verification orchestration.

## Verification architecture

The durable orchestration lives in:

- `tools/AssistantEngineer.Tools.Iso52016PhysicalVerification`

The PowerShell release gate is intentionally thin:

- `scripts/iso52016/assert-iso52016-physical-model-chain-release-ready.ps1`

This follows the Engineering Core V1 pattern: `.ps1` remains a developer/CI entrypoint, while durable checks live in C#.

## Release-ready checks

The C# tool verifies:

- physical chain source files exist;
- stage documents and manifests exist;
- release-ready gate files exist;
- claim boundaries are explicit;
- positive full parity/equivalence claims are absent;
- Matrix-all chain keeps discoverability hooks;
- physical C# tests pass unless explicitly skipped.

## Claim boundary

This is an ISO52016-inspired physical model release gate with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.
