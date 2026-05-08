# Engineering Core V1 API Contract Snapshots

## Purpose

This folder contains checked-in API contract snapshots for Engineering Core V1.

Snapshots make the application-visible contract easy to inspect without running the API.

## Files

- docs/api/engineering-core-v1/status.sample.json
- docs/api/engineering-core-v1/diagnostics-catalog.sample.json
- docs/api/engineering-core-v1/engineering-core-v1.http

## Endpoints

Status endpoint:

    GET /api/v1/calculations/engineering-core/v1/status

Diagnostics catalog endpoint:

    GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

## Generation

Regenerate snapshots from manifest and diagnostics catalog:

    .\scripts\engineering-core\generate-engineering-core-v1-api-contract-snapshots.ps1

The generator uses:

- docs/releases/EngineeringCoreV1Manifest.json
- docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json

## Verification

Snapshots are guarded by:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ApiContractSnapshotTests"

The tests verify:

- sample JSON files exist;
- sample HTTP file exists;
- status sample matches manifest status and closed gates;
- diagnostics sample matches diagnostics catalog codes and rules;
- endpoints are documented;
- non-claims remain visible.

## Contract rules

Status response must expose:

- coreName;
- version;
- status;
- formulaGatesClosed;
- weather8760GatesClosed;
- annualHourly8760GateClosed;
- successfulResultsMustNotContainErrorDiagnostics;
- formulaGates;
- explicitNonClaims;
- outOfScopeV1;
- plannedValidation;
- requiredAnnual8760Flags;
- documentationFiles.

Diagnostics catalog response must expose:

- catalogName;
- version;
- status;
- rules;
- diagnostics;
- code;
- severity;
- category;
- userMessage;
- userAction;
- closedV1Gate.

## Non-claims

Snapshots must keep these non-claims visible:

- no exact EnergyPlus numerical equivalence;
- no exact StandardReference numerical equivalence;
- no ASHRAE 140 / BESTEST-style validation anchor coverage;
- no full ISO 52016 node/matrix solver equivalence;
- no latent/moisture/humidity support in v1.

## OpenAPI and REST client artifacts

Additional contract artifacts:

- docs/api/engineering-core-v1/openapi.fragment.yml
- docs/api/engineering-core-v1/postman_collection.json
- docs/api/engineering-core-v1/ConsumerGuide.md
- docs/api/engineering-core-v1/CHANGELOG.md

Guard test:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1OpenApiContractTests"
