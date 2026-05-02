# Engineering Core V1 Documentation Index

## Status

Engineering Core V1 is closed as an engineering formula gate.

## Start here

- docs/releases/EngineeringCoreV1.md
- docs/releases/EngineeringCoreV1Manifest.json
- docs/releases/EngineeringCoreV1ReleaseManifest.md
- docs/releases/EngineeringCoreV1ReleaseChecklist.md
- docs/releases/EngineeringCoreV1OwnerHandoff.md

## Scope and release

- docs/calculations/EngineeringCoreV1Scope.md
- docs/calculations/EngineeringCoreV1ReleaseNotes.md
- docs/calculations/EngineeringCoreV1ApiExamples.md
- docs/calculations/EngineeringCoreV1DeveloperGuide.md
- docs/calculations/EngineeringCoreV1VerificationRunbook.md

## Diagnostics

- docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json
- docs/calculations/EngineeringCoreV1DiagnosticsCatalog.md
- docs/calculations/EngineeringCoreV1DiagnosticsCatalogApi.md
- docs/frontend/EngineeringCoreV1DiagnosticsUx.md

## Frontend visibility

- docs/frontend/EngineeringCoreV1StatusPanel.md
- docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md
- docs/frontend/EngineeringCoreV1FrontendIntegrationGuard.md

## Validation

- docs/calculations/EnergyPlusAshrae140ValidationPlan.md
- docs/validation/EnergyPlusAshrae140ValidationHarness.md
- docs/validation/EnergyPlusValidationCaseTemplate.md

## Operations and support

- docs/runbooks/EngineeringCoreV1OperationalRunbook.md
- docs/troubleshooting/EngineeringCoreV1Troubleshooting.md
- docs/ci/EngineeringCoreV1CI.md
- docs/contributing/EngineeringCoreV1ContributionGuide.md

## Architecture decisions

- docs/adr/0001-engineering-core-v1-closure-policy.md

## Generated evidence

- docs/reports/EngineeringCoreV1ReleaseEvidence.md

Generate it with:

    .\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1

## Required non-claims

Engineering Core V1 does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in v1.

## API contract package

- docs/api/engineering-core-v1/openapi.fragment.yml
- docs/api/engineering-core-v1/postman_collection.json
- docs/api/engineering-core-v1/status.sample.json
- docs/api/engineering-core-v1/diagnostics-catalog.sample.json
- docs/api/engineering-core-v1/engineering-core-v1.http
- docs/api/engineering-core-v1/ConsumerGuide.md
- docs/api/engineering-core-v1/CHANGELOG.md

## Report contract snapshots

- docs/reports/engineering-core-v1/heating-report.sample.json
- docs/reports/engineering-core-v1/cooling-report.sample.json
- docs/reports/engineering-core-v1/annual-energy-disclosure.sample.json
- docs/reports/engineering-core-v1/README.md
- docs/reports/engineering-core-v1/ConsumerGuide.md

## Validation case registry

- docs/validation/EnergyPlusValidationCaseRegistry.json
- docs/validation/EnergyPlusValidationCaseRegistry.md
- docs/reports/EngineeringCoreV1ValidationReadiness.md
- scripts/engineering-core/generate-engineering-core-v1-validation-readiness.ps1

## Traceability matrix

- docs/traceability/EngineeringCoreV1TraceabilityMatrix.json
- docs/traceability/EngineeringCoreV1TraceabilityMatrix.md
- docs/traceability/README.md
- scripts/engineering-core/generate-engineering-core-v1-traceability-matrix.ps1
