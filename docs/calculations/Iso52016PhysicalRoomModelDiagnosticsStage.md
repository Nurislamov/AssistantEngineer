# AE-ISO52016-002 Step 06 - physical room model diagnostics stage

This stage adds deterministic diagnostics for the ISO52016-inspired physical room/zone model after it has been translated into the existing Matrix solver request.

## Scope

Step 06 does not add a new solver and does not change the Matrix heat-balance algorithm. It adds a C# diagnostics layer for inspecting the physical-to-Matrix translation created by the previous physical stages.

The diagnostics layer reports:

- matrix zone code and air node id;
- node count and node ids;
- internal conductance link count;
- boundary conductance link count and boundary ids;
- total node heat capacity;
- total declared internal and boundary conductance;
- hourly source total gains from the room hourly input profile;
- hourly distributed node heat gains in the generated Matrix request;
- hourly gain-balance error;
- hourly boundary conductance override counts and maxima.

## Engineering purpose

The diagnostics make the physical model auditable before deeper application integration. They help guard these common mistakes:

- losing heat gains during convective/radiative/solar distribution;
- accidentally duplicating or removing physical nodes;
- losing ventilation boundary override links;
- collapsing surface-specific boundaries back into a shared boundary id;
- changing topology without updating stage documentation and manifests.

## Validation anchors

The deterministic tests cover:

- aggregated three-node fallback topology;
- explicit surface and operation-profile topology;
- gain conservation from source hourly profile to generated Matrix node gains;
- propagation of builder validation failures.

## Claim boundary

This is an ISO52016-inspired physical room model diagnostics stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.
