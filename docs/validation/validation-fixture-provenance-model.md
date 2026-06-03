# Validation Fixture Provenance Model (P9-03)

## Purpose

Define a canonical provenance model for validation fixtures/evidence so evidence strength and claim boundaries stay explicit.

## Scope

This model applies to validation fixtures, manifests, and registries referenced by:

- `docs/validation/*`
- `tests/AssistantEngineer.Tests/Validation/*`
- `tests/AssistantEngineer.Tests/Calculations/*`
- validation-oriented scripts/tools inventories

## Non-claims

- No calculation physics change claim.
- No expected numerical values change claim.
- No pyBuildingEnergy full parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.
- No production certified claim.

## Provenance categories

- `ManualFixture`
- `IndependentReferenceFixture`
- `InternalInvariant`
- `ReleaseGateManifest`
- `ExternalToolReferenceCandidate`
- `DonorMethodologyReference`
- `HistoricalSmoke`
- `PlannedPlaceholder`
- `UnknownNeedsReview`

## Required metadata fields

- `fixtureId`
- `path`
- `area`
- `provenanceCategory`
- `evidenceStrength`
- `sourceType`
- `sourceDescription`
- `sourceDateOrVersion`
- `generatedBy`
- `reviewedBy`
- `lastReviewedDate`
- `expectedValuePolicy`
- `allowedClaim`
- `forbiddenClaims`
- `relatedTests`
- `knownLimitations`

## Evidence strength levels

- `InternalInvariant`
- `ManualReferenceAnchor`
- `IndependentReferenceFixture`
- `ExternalToolCandidate`
- `CrossImplementationCandidate`
- `FormalValidationNotClaimed`

## Allowed claims by provenance category

- `ManualFixture`: manual engineering reference fixture, validation anchor
- `IndependentReferenceFixture`: independent reference fixture, validation anchor
- `InternalInvariant`: internal invariant, reference-informed
- `ReleaseGateManifest`: governance-ready evidence manifest, release-gate manifest
- `ExternalToolReferenceCandidate`: external tool reference candidate, comparison candidate
- `DonorMethodologyReference`: reference-informed methodology context
- `HistoricalSmoke`: historical smoke evidence (non-certifying)
- `PlannedPlaceholder`: planned evidence placeholder only
- `UnknownNeedsReview`: classification pending; no strong validation claim

## Forbidden claims

- full pyBuildingEnergy parity
- EnergyPlus parity
- ASHRAE 140 validated
- ISO certified
- fully validated
- production certified

## Placeholder/planned evidence policy

- Planned items must be marked `PlannedPlaceholder`.
- Planned items are not counted as achieved evidence.
- Planned items must include a `proposedFollowUpStage`.

## Numeric expected values policy

- P9-03 is metadata/docs/test cleanup only.
- Fixture expected numerical values remain unchanged.

## Review/update policy

- Provenance entries are updated when fixture source/provenance metadata changes.
- Reclassification to stronger evidence requires concrete repository evidence and claim-boundary review.
