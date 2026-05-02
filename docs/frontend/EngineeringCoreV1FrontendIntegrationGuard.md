# Engineering Core V1 Frontend Integration Guard

## Purpose

This guard documents and tests the frontend integration points that make Engineering Core V1 visible to users.

The frontend must not silently hide calculation limitations in raw JSON or debug-only screens.

## Guarded files

The guard checks these frontend files:

    src/Frontend/src/entities/calculation/types.ts
    src/Frontend/src/shared/api/apiRoutes.ts
    src/Frontend/src/shared/api/queryKeys.ts
    src/Frontend/src/entities/calculation/api/calculationsApi.ts
    src/Frontend/src/entities/calculation/model/useEngineeringCoreStatus.ts
    src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx
    src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx
    src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx
    src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx

## Required UX

The dashboard must display Engineering Core V1 status.

Report UI must display calculationDisclosure before raw report JSON.

Visible disclosure must include:

- warnings;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items;
- documentation files.

## Required non-claims

The frontend must support visible display of:

- no exact EnergyPlus numerical parity claim;
- no exact pyBuildingEnergy numerical parity claim;
- no ASHRAE 140 validation coverage claim;
- no full ISO 52016 node/matrix solver parity claim;
- no latent/moisture/humidity calculation claim.

## Test

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreFrontendIntegrationGuardTests"
