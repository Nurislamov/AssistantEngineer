# Equipment Diagnostics Staging

This folder is for manual-backed diagnostic candidates before they are promoted into the runtime JSON catalog.

Staging files are review artifacts only. They are excluded from embedded runtime knowledge resources and must not be loaded by `EquipmentDiagnosticsJsonKnowledgeSource`.

This staging area is part of the ED-10 manual-ingestion and bot-readiness pack. It supports reviewed catalog growth, but it is not a runtime catalog, API endpoint, database import path, Telegram integration, or AI/RAG/vector search layer.

## Runtime Boundary

The approved runtime catalog remains under production knowledge folders such as:

- `Knowledge/gree/gree-gmv.json`
- `Knowledge/gree/gree-chiller.json`

Files under `Knowledge/staging/` are not runtime data. A staged candidate becomes runtime data only when a reviewer copies an approved entry into a production catalog JSON file and the normal module validation passes.

## Review Status

Allowed `reviewStatus` values:

- `Draft`
- `NeedsManualCheck`
- `ReadyForReview`
- `ApprovedForCatalog`
- `Rejected`

`Draft` and `NeedsManualCheck` candidates stay in staging. `ApprovedForCatalog` requires explicit source evidence and review notes before promotion.

`ReadyForReview` means ready for engineering review, not runtime approval. `ApprovedForCatalog` means the candidate can be copied into production JSON only after validator checks pass and the change is reviewed in a normal PR.

## Staging Examples

This folder contains review examples:

- `templates/gree-manual-entry.template.json`: draft candidate template for new findings.
- `examples/gree-gmv-ready-for-review.sample.json`: a deterministic `ReadyForReview` sample that is valid for review but not promotion-ready.
- `examples/gree-gmv-invalid-insufficient-evidence.sample.json`: an invalid sample that proves approval fails when evidence is insufficient.

These files are not runtime data and are excluded from embedded runtime catalog resources.

Runtime search and catalog index results must not include staging sample codes. Staging examples may contain placeholders or intentionally invalid candidates only when they are clearly marked as non-runtime review artifacts.

## Staging Validator

`Application/Knowledge/Staging/EquipmentDiagnosticsStagingValidator.cs` provides deterministic validation for candidate files. It checks:

- required candidate, source, diagnostic step, measurement, and safety fields;
- allowed `reviewStatus`, `sourceType`, `evidenceLevel`, and `proposedConfidence` values;
- `ManualVerified` evidence rules;
- `ManualPageVerified` manual title and page requirements;
- `CrossChecked` evidence notes;
- `ApprovedForCatalog` source evidence;
- duplicate candidate keys in one staging file;
- candidate key conflicts with the production runtime catalog;
- unsafe diagnostic wording.

The validator is not a runtime catalog loader and is not exposed through a public API endpoint.

Validation results include a report summary:

- total candidates;
- error, warning, and info counts;
- normalized candidate keys;
- issues grouped by candidate key;
- `PromotionReady`;
- `HasBlockingIssues`.

`PromotionReady` is true only for error-free `ApprovedForCatalog` candidates. It is false for `Draft`, `NeedsManualCheck`, and `ReadyForReview` because those statuses still require review before runtime promotion.

## Promotion Checklist

1. Capture the manual observation as a staging candidate.
2. Verify manufacturer, series, category, model, and code against the installed equipment family.
3. Fill source evidence from the exact service manual or cross-check record.
4. Confirm limitations, applicability, and affected series/models.
5. Keep instructions safe and technician-scoped.
6. Review validator report grouping by candidate key.
7. Pass schema, staging validator, and module tests.
8. Copy the candidate into a production catalog JSON file only after review approval.

## Evidence and Confidence

- `UnverifiedSeed`: deterministic seed or placeholder knowledge.
- `ManualReferenced`: a real manual is identified, but an exact page is not verified.
- `ManualPageVerified`: exact manual and page evidence are present.
- `CrossChecked`: multiple sources or records were compared and notes explain the cross-check.

`ManualVerified` confidence is reserved for entries with `ManualPageVerified` or `CrossChecked` evidence. Do not invent manual titles, versions, document codes, pages, sections, or quotes. Do not store long copyrighted manual text; use short identifiers and page/section references, with a minimal quote only when needed for review.

## Future Assistant Or Bot Use

Staging does not add Telegram, AI, RAG, vector search, or public endpoints. Future assistant/UI flows should use approved runtime API fields:

1. Search deterministic catalog entries with the existing error-code search endpoint.
2. Fetch the selected diagnostic case.
3. Render `shortSummary`, `sourceSummary`, `confidenceExplanation`, `recommendedNextChecks`, `safetyBoundary`, and `verificationRequired`.
4. Avoid inventing diagnostic text outside the response DTO.

The current runtime Gree entries remain `SeededEngineeringKnowledge` / `UnverifiedSeed` / `Low` unless exact source evidence is promoted through production JSON review.

Future UI, Telegram, or assistant integrations must consume approved runtime DTO fields or the deterministic formatter output. They must not promote staging text directly and must not generate new diagnosis text outside the reviewed runtime response.
