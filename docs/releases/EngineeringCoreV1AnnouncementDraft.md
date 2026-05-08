# Engineering Core V1 Announcement Draft

## Short announcement

Engineering Core V1 is now closed as an engineering formula gate.

The release closes the main HVAC calculation-core scope for design-point heating/cooling loads, simplified hourly heat balance, EPW/PVGIS 8760 weather gates, annual true hourly 8760 integration, simplified ground/adjacent/DHW/system energy paths, equipment capacity sizing, diagnostics, report disclosures, frontend visibility and release traceability.

## Recommended wording

Use:

    Engineering Core V1 is closed as an engineering formula gate with documented limitations.

Do not use:

    No "EnergyPlus comparison workflow achieved" claim.
    No "ASHRAE 140 / BESTEST-style validated" claim.
    Full ISO 52016 implemented.

## What changed

Engineering Core V1 now includes:

- FormulaAuditMatrix as source of truth;
- validation flow where Error diagnostics fail the calculation;
- EPW/PVGIS 8760 gates;
- true hourly annual 8760 rule;
- simplified ISO/EN-inspired scope documentation;
- Engineering Core status endpoint;
- diagnostics catalog endpoint;
- report calculationDisclosure;
- frontend status/disclosure/diagnostics panels;
- API/OpenAPI/Postman contract package;
- report/export disclosure policy;
- EnergyPlus/ASHRAE 140-style validation registry;
- release evidence;
- traceability matrix;
- CI workflow;
- contribution and release readiness guards.

## Non-claims to keep visible

This release does not claim:

- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in V1.

## Verification command

Before announcement, run:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

## Next phase

Recommended next phase:

1. add first real EnergyPlus smoke reference fixture;
2. generate validation report from the first fixture;
3. keep validation comparative and tolerance-based;
4. plan latent/moisture psychrometrics as a future module;
5. plan equipment part-load/performance-curve modeling as a future module.
