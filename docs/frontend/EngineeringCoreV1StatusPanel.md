# Engineering Core V1 Frontend Status Panel

## Purpose

The frontend dashboard displays Engineering Core V1 status through the backend endpoint:

    GET /api/v1/calculations/engineering-core/v1/status

The panel exists to make calculation scope and limitations visible to users.

## Files

Frontend integration files:

    src/Frontend/src/entities/calculation/types.ts
    src/Frontend/src/entities/calculation/api/calculationsApi.ts
    src/Frontend/src/entities/calculation/model/useEngineeringCoreStatus.ts
    src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx
    src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx

## Displayed information

The panel displays:

- Engineering Core version;
- ClosedV1 status;
- formula gate count;
- EPW/PVGIS 8760 gate status;
- annual hourly 8760 gate status;
- annual 8760 requirements;
- out-of-scope v1 items;
- explicit non-claims;
- documentation file links.

## Required non-claims

The panel must keep these limitations visible:

- no exact EnergyPlus numerical equivalence claim;
- no exact StandardReference numerical equivalence claim;
- no ASHRAE 140 / BESTEST-style validation anchor coverage claim;
- no full ISO 52016 node/matrix solver equivalence claim;
- no latent/moisture/humidity calculation claim.

## UX rule

Warnings, assumptions and non-claims must not be hidden behind debug-only UI.

The dashboard panel should remain visible in normal application use so that calculation results are not interpreted as full external-simulator equivalence.
