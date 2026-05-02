# Engineering Core V1 Traceability Matrix

## Purpose

This folder contains the traceability matrix for Engineering Core V1.

The matrix links:

- release manifest;
- closed formula gates;
- diagnostics catalog;
- validation registry;
- API endpoints;
- backend visibility;
- frontend visibility;
- report disclosure visibility;
- documentation files;
- verification scripts;
- CI workflow;
- explicit non-claims.

## Files

- docs/traceability/EngineeringCoreV1TraceabilityMatrix.json
- docs/traceability/EngineeringCoreV1TraceabilityMatrix.md

## Generation

Regenerate the matrix:

    .\scripts\engineering-core\generate-engineering-core-v1-traceability-matrix.ps1

The generator reads:

- docs/releases/EngineeringCoreV1Manifest.json
- docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json
- docs/validation/EnergyPlusValidationCaseRegistry.json

## Verification

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1TraceabilityMatrixTests"

## Required traceability

The matrix must prove that:

- every manifest ClosedV1 formula gate appears in traceability;
- every traceability formula gate is ClosedV1;
- diagnostics reference known closed gates or known FormulaAuditMatrix gates;
- application endpoint is visible;
- frontend visibility files are listed;
- backend visibility files are listed;
- validation cases are listed;
- annual 8760 requirements are listed;
- out-of-scope v1 items are listed;
- planned validation is listed;
- non-claims remain visible.

## Non-claims

The traceability matrix does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- latent/moisture/humidity support in v1.
