# AE-ISO52016-002 Step 09 - physical scenario anchors stage

This stage adds deterministic physical room model scenario anchors on top of the ISO52016-inspired physical node/surface/boundary/operation chain.

## Scope

Step 09 does not add a new solver and does not change the existing physical builder. It adds deterministic scenario tests that exercise the already-owned C# calculation path:

- physical model request;
- physical node/surface/boundary/operation builder;
- Matrix hourly solver request;
- Matrix hourly solver execution;
- hourly node-gain conservation checks.

The anchors intentionally cover more than topology-only checks. They prove that representative scenarios can be built and solved end-to-end without relying on generated artifacts or external reference claims.

## Scenario anchors

The stage adds anchors for:

- aggregated three-node fallback with operation profile ventilation and gain splits;
- explicit surface and ground boundary profiles with per-surface boundary temperatures;
- adjacent conditioned and adjacent unconditioned boundary mappings;
- hourly node-gain conservation from source gains to distributed Matrix node gains;
- finite Matrix solver outputs for the deterministic scenarios.

## Claim boundary

This is an ISO52016-inspired physical scenario anchor stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.