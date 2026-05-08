# Engineering Core V1 Report Disclosure Panel

## Purpose

The frontend must show calculation disclosure information in normal report UI.

The disclosure panel is used for backend heating and cooling reports that contain:

    calculationDisclosure

The panel makes the Engineering Core V1 scope visible to users.

## Component

Frontend component:

    src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx

The component accepts an unknown report object and renders only when the object contains a valid calculationDisclosure shape.

## Integration

The report panel renders EngineeringCoreDisclosurePanel before raw report JSON:

    src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx

This keeps the disclosure visible while preserving the raw JSON inspector for debugging.

## Displayed fields

The panel displays:

- coreStatus;
- calculationScope;
- calculationMethod;
- actualMethod;
- warnings;
- assumptions;
- explicitNonClaims;
- outOfScopeV1;
- documentationFiles.

## UX rule

Warnings, assumptions and non-claims must be visible in normal report UI.

They must not be hidden only inside raw JSON or debug-only views.

## Required non-claims

The panel must support showing:

- no exact EnergyPlus numerical equivalence claim;
- no exact StandardReference numerical equivalence claim;
- no ASHRAE 140 / BESTEST-style validation anchor coverage claim;
- no full ISO 52016 node/matrix solver equivalence claim;
- no latent/moisture/humidity calculation claim.

## Out-of-scope v1 visibility

The panel must show out-of-scope v1 items such as:

- HVAC.LATENT_LOAD;
- HVAC.MOISTURE_BALANCE;
- detailed psychrometric supply-air treatment;
- detailed HVAC plant simulation.
