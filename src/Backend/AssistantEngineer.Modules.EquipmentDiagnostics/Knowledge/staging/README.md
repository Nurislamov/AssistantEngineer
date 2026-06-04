# Equipment Diagnostics Staging

This folder is for manual-backed diagnostic candidates before they are promoted into the runtime JSON catalog.

Staging files are review artifacts only. They are excluded from embedded runtime knowledge resources and must not be loaded by `EquipmentDiagnosticsJsonKnowledgeSource`.

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

## Promotion Checklist

1. Verify manufacturer, series, category, model, and code against the installed equipment family.
2. Fill source evidence from the exact service manual or cross-check record.
3. Confirm limitations, applicability, and affected series/models.
4. Keep instructions safe and technician-scoped.
5. Pass schema, staging validator, and module tests.
6. Copy the candidate into a production catalog JSON file only after review approval.

## Evidence and Confidence

- `UnverifiedSeed`: deterministic seed or placeholder knowledge.
- `ManualReferenced`: a real manual is identified, but an exact page is not verified.
- `ManualPageVerified`: exact manual and page evidence are present.
- `CrossChecked`: multiple sources or records were compared and notes explain the cross-check.

`ManualVerified` confidence is reserved for entries with `ManualPageVerified` or `CrossChecked` evidence. Do not invent manual titles, versions, document codes, pages, sections, or quotes. Do not store long copyrighted manual text; use short identifiers and page/section references, with a minimal quote only when needed for review.
