# AE-ISO52016-002 Step 07 - physical verification orchestration

This stage moves the stable verification orchestration for the ISO52016-inspired physical room model chain into a C# tool.

## Scope

The previous physical model stages added:

- Step 01 physical node model builder;
- Step 02 surface and construction expansion;
- Step 03 per-surface hourly boundary profile support;
- Step 04 hourly operation profile support;
- Step 05 physical room simulation service adapter;
- Step 06 physical model diagnostics.

Step 07 does not add new thermal equations and does not change the Matrix solver. It adds:

- `AssistantEngineer.Tools.Iso52016PhysicalVerification`;
- a thin wrapper script `scripts/iso52016/verify-iso52016-physical-model-chain.ps1`;
- stage documentation, manifest, and guard tests.

## Verification responsibilities

The C# verifier owns the durable orchestration checks:

- required file presence for physical stages 01-06;
- manifest stage id and work item checks;
- claim-boundary checks;
- Matrix all-verification hook checks;
- physical model test-gate execution unless `--skip-tests` is used.

The `.ps1` wrapper remains only as a developer/CI entrypoint.

## Claim boundary

This is an ISO52016-inspired physical verification orchestration stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.