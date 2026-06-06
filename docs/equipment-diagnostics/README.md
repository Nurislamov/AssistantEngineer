# Equipment Diagnostics

## ED-14A manual codebook

The non-runtime [manual codebook](manual-codebook.md) inventories reviewed code occurrences from the local Gree manual corpus. It separates fault/protection references from status, debugging, query, setting, controller/tool, and applicability contexts. Codebook entries are not runtime diagnostic cases and are never promoted automatically.

Equipment Diagnostics is an early backend module for deterministic diagnostic support around equipment error codes, likely causes, required measurements, safety notes, and manual references.

## Purpose

- Provide a clean domain and application foundation for HVAC equipment error-code diagnostics.
- Keep diagnostic knowledge separate from equipment catalog and equipment selection concerns.
- Expose a public facade that the API layer can compose without depending on module internals.
- Start with deterministic JSON seed data before persistence, import, or search infrastructure is introduced.

## Boundaries

- The module owns equipment diagnostic concepts: manufacturers, series, models, error codes, diagnostic cases, diagnostic steps, required measurements, manual references, and confidence.
- The module does not own room/building load calculation, equipment sizing, catalog persistence, report rendering, authentication, or UI.
- Domain and application code must not depend on EF Core, ASP.NET Core, Infrastructure, UI, or calculation implementation details.
- ED-00 has no database migration and no Infrastructure adapter.

## Why This Is Separate From Equipment

`AssistantEngineer.Modules.Equipment` is responsible for equipment catalog and selection workflows. Error-code diagnostics have different data, safety, provenance, and future search requirements. Keeping diagnostics in `AssistantEngineer.Modules.EquipmentDiagnostics` avoids mixing catalog sizing concerns with service troubleshooting knowledge.

## MVP Scope

ED-00 includes:

- Domain model records and enums for diagnostic concepts.
- Application contracts for searching error codes and retrieving diagnostic cases.
- A public facade for API composition.
- Deterministic in-memory seed examples:
  - Gree / GMV / H5
  - Gree / GMV / C7
  - Gree / Chiller / E6
- Tests for search behavior, normalization, diagnostic-case content, confidence claims, and module dependency boundaries.

ED-01 adds read-only API endpoints in `AssistantEngineer.Api`:

- `GET /api/v1/equipment-diagnostics/error-codes`
- `GET /api/v1/equipment-diagnostics/cases`

The API layer is a thin adapter over `IEquipmentDiagnosticsFacade`. Search normalization stays in the application service.

ED-02 adds a knowledge catalog layer:

- `Application/Knowledge/IEquipmentDiagnosticsKnowledgeSource.cs`
- `Application/Knowledge/InMemoryEquipmentDiagnosticsKnowledgeSource.cs`
- `Application/Knowledge/EquipmentDiagnosticsKnowledgeCatalog.cs`
- `Application/Knowledge/EquipmentDiagnosticsKnowledgeEntry.cs`

`InMemoryEquipmentDiagnosticsService` now depends on `IEquipmentDiagnosticsKnowledgeSource`. The service owns query behavior, normalization, filtering, and DTO mapping. The deterministic seeded diagnostic entries live in the knowledge catalog instead of inside the service.

ED-03 moves seeded entries into structured JSON files embedded in the module:

- `Knowledge/equipment-diagnostics.schema.json`
- `Knowledge/gree/gree-gmv.json`
- `Knowledge/gree/gree-chiller.json`

`EquipmentDiagnosticsJsonKnowledgeSource` reads embedded JSON resources through `EquipmentDiagnosticsKnowledgeJsonLoader`. The loader performs lightweight module validation without external schema-validator dependencies. The catalog remains deterministic and local. It is not persistence, not a manual-ingestion workflow, and not an AI/RAG search layer.

ED-04 adds a provenance/source model for every diagnostic entry. Each entry must state where its knowledge comes from, what evidence level supports it, and what limitations apply. The current Gree entries remain deterministic seed knowledge and are not manual verified.

ED-05 adds a small conservative Gree GMV catalog expansion. The added entries are deterministic seed guidance only:

- Gree / GMV / E1
- Gree / GMV / E3
- Gree / GMV / E4
- Gree / GMV / E5

They use `sourceType = SeededEngineeringKnowledge`, `evidenceLevel = UnverifiedSeed`, and `confidence = Low`. They do not provide manual titles, pages, sections, or quotes because no exact manual evidence is stored in this repository for those entries.

ED-06 adds a deterministic catalog index/query layer for future UI and assistant experiences:

- `GET /api/v1/equipment-diagnostics/catalog`
- manufacturer, series, category, and code facets
- total catalog counts
- source type and evidence level summaries
- deterministic sorting and duplicate key validation

The index is built from the existing JSON knowledge source through the application service. It is not a database, not a search engine, and not an AI/RAG/vector layer. Code matching remains deterministic and normalized so safe formatting differences such as whitespace or a hyphen in an error code do not break lookup.

ED-07 adds a manual-backed staging workflow for future catalog expansion:

- `Knowledge/staging/README.md`
- `Knowledge/staging/equipment-diagnostics-staging.schema.json`
- `Knowledge/staging/templates/gree-manual-entry.template.json`

Staging files are review artifacts only. They are excluded from embedded runtime knowledge resources and are not loaded by `EquipmentDiagnosticsJsonKnowledgeSource`. The runtime catalog remains the approved source of truth under production knowledge folders such as `Knowledge/gree/`.

ED-08 adds a deterministic staging validator and promotion guard:

- `Application/Knowledge/Staging/IEquipmentDiagnosticsStagingValidator.cs`
- `Application/Knowledge/Staging/EquipmentDiagnosticsStagingValidator.cs`
- staging candidate/result/issue models

The validator checks required fields, allowed review/source/evidence/confidence values, evidence rules for `ManualVerified`, duplicate staging keys, conflicts with the production runtime catalog, and unsafe diagnostic wording. It is not registered as a public API endpoint and does not load staging candidates into runtime knowledge.

ED-09A adds catalog expansion pack 1. It expands production JSON coverage while keeping every added entry as deterministic seed knowledge:

- Gree / GMV outdoor: F0, F1, F2, F3, L1, L2, P0, P1
- Gree / Chiller: E1, E2, E3, E4
- Gree / Indoor foundation: C5, E1, F0, H6

The indoor foundation uses the existing `VrfIndoorUnit` category and documents that indoor, duct, and split-family code meanings can vary by installed controller and model. All ED-09A entries use `sourceType = SeededEngineeringKnowledge`, `evidenceLevel = UnverifiedSeed`, and `confidence = Low`. They do not include manual titles, versions, document codes, pages, sections, quotes, or manual references.

ED-09B adds deterministic query quality and catalog QA improvements on top of the existing runtime JSON catalog:

- common code formatting such as `h5`, `H-5`, and `H 5` normalizes to the same lookup intent;
- series formatting such as `GMV` and `G-M-V` is normalized;
- conservative free-text search is supported through the existing error-code search endpoint with `query`;
- query matching is token-based and deterministic, not AI/RAG/vector search;
- exact code matches rank highest, manufacturer/series/category matches rank next, then title/tags, then meaning/likely causes;
- friendly category words such as `outdoor`, `indoor`, and `chiller` can help disambiguate shared codes;
- broader QA tests guard duplicate normalized keys, tag style, minimum safety notes, required measurements, diagnostic steps, provenance, staging exclusion, and unsafe wording.

Example deterministic query patterns:

- `query=gree gmv h5`
- `query=gmv outdoor e1`
- `query=chiller e6`
- `query=indoor h6`

Free-text matching remains conservative: all query tokens must match deterministic catalog fields. Ambiguous or unknown text returns an empty result rather than guessing.

ED-09C adds operator-facing response quality fields to diagnostic case responses. These fields are derived deterministically from the existing catalog entry:

- `shortSummary`
- `recommendedNextChecks`
- `confidenceExplanation`
- `sourceSummary`
- `applicabilitySummary`
- `safetyBoundary`
- `operatorNotes`
- `isManualVerified`
- `isSeedKnowledge`
- `verificationRequired`

The module does not invent new procedures for these fields. Recommended checks summarize the existing diagnostic steps and required measurements. Confidence and source explanations come from the existing `confidence` and `source` block. Applicability and limitations come from `source.applicableSeries`, `source.applicableModels`, and `source.limitations`. Safety boundaries come from the category and catalog safety notes.

For current Gree seed entries, operator-facing fields continue to state that the guidance is preliminary, seed knowledge, and requires verification against the exact installed model, controller, and service manual. Future Telegram, assistant, or UI surfaces should consume these fields instead of generating their own diagnostic wording.

ED-10A improves manual-ingestion and bot-readiness workflow without adding persistence, Telegram, AI, RAG, vector search, or new public endpoints:

- staging now includes review examples for `Draft`, `ReadyForReview`, and invalid insufficient-evidence candidates;
- staging examples remain excluded from runtime embedded knowledge;
- validator results include a deterministic report summary with total candidates, issue counts, candidate keys, grouped issues, `PromotionReady`, and `HasBlockingIssues`;
- `PromotionReady` is reserved for error-free `ApprovedForCatalog` candidates and does not apply to `Draft`, `NeedsManualCheck`, or `ReadyForReview`;
- future assistant or UI flows should search deterministically, fetch the diagnostic case, and render existing operator fields rather than inventing diagnostic prose.

ED-10B adds a deterministic assistant/operator consumption helper without changing API routes, DTO contracts, persistence, Telegram, AI, RAG, or vector search:

- `Application/Guidance/EquipmentDiagnosticOperatorGuidanceFormatter.cs`
- `Application/Guidance/EquipmentDiagnosticOperatorGuidanceMessage.cs`

The formatter consumes an existing `EquipmentDiagnosticCaseDto` and reshapes its operator-facing fields into a stable message structure: title, summary, verification banner, source line, recommended checks, safety line, operator notes, and footer. It does not create new diagnostic actions. Recommended checks come directly from `recommendedNextChecks`, safety text comes from `safetyBoundary`, and operator notes come from `operatorNotes`.

ED-10C adds contract examples and release-readiness guards for future bot/UI/Telegram consumers:

- `docs/equipment-diagnostics/examples/catalog-response.example.json`
- `docs/equipment-diagnostics/examples/error-code-search-response.example.json`
- `docs/equipment-diagnostics/examples/diagnostic-case-response.example.json`
- `docs/equipment-diagnostics/examples/operator-guidance-message.example.json`
- `docs/equipment-diagnostics/examples/staging-validation-report.example.json`

These examples are documentation/test fixtures only. They are not runtime knowledge, are not embedded resources, and are not loaded by `EquipmentDiagnosticsJsonKnowledgeSource`. Tests guard that examples remain valid JSON, avoid manual-verification claims without evidence, avoid unsafe diagnostic wording, and do not affect runtime catalog counts.

ED-10D finalizes the ED-10 manual-ingestion and bot-readiness pack with consistency and release-readiness QA:

- staging JSON, docs examples, and test fixtures remain non-runtime and outside embedded knowledge resources;
- runtime catalog counts are derived from production JSON files rather than fixed magic numbers;
- staging sample codes are not returned by runtime search or catalog index;
- seed runtime entries stay `SeededEngineeringKnowledge` / `UnverifiedSeed` / `Low`;
- unverified seed case DTOs keep `verificationRequired = true`;
- formatter output keeps the verification banner visible and reuses DTO checks/safety text;
- the public read routes remain limited to catalog, error-code search, and case retrieval.

ED-10A, ED-10B, ED-10C, and ED-10D together form a manual-ingestion and bot-readiness pack. They do not add persistence, admin import, Telegram/UI integration, AI/RAG/vector search, or new public endpoints.

ED-11A adds a deterministic manual-intake and promotion-readiness pipeline:

- application-level verification report models and report engine;
- dedicated `AssistantEngineer.Tools.EquipmentDiagnosticsVerification` C# CLI;
- thin `scripts/equipment-diagnostics/verify-equipment-diagnostics.ps1` wrapper;
- runtime catalog, staging candidate, staging example, and docs example QA sections;
- candidate readiness states and generic next actions;
- release-readiness and blocking-issue summary.

The pipeline validates and reports only. It never automatically writes or promotes runtime catalog entries. Runtime changes still require production JSON edits, full verification, and a reviewed pull request.

## JSON Catalog

Each JSON file contains an `entries` array. Each entry has:

- `manufacturer`, `seriesName`, `modelCode`, `category`, `code`, `title`, `meaning`, `severity`, `confidence`
- `likelyCauses[]`
- `diagnosticSteps[]` with `order`, `title`, `instruction`, `expectedResult`, `ifFailedAction`
- `requiredMeasurements[]` with `name`, `unit`, `description`, `requiredBeforeConclusion`
- `safetyNotes[]`
- `manualReferences[]`
- `source` with `sourceType`, `evidenceLevel`, manual evidence fields, `limitations[]`, `applicableModels[]`, and `applicableSeries[]`
- `tags[]`

To add a new manufacturer, series, or code:

1. Add or extend a JSON file under `Knowledge/{manufacturer}/`.
2. Keep arrays present even when a future entry has no optional content.
3. Use only defined `EquipmentCategory` and `DiagnosticConfidence` enum names.
4. Keep `confidence` below `ManualVerified` until exact manual evidence and page references are present in the repository.
5. Include safety notes and required measurements for every diagnostic case.
6. Include a `source` block for every entry.
7. Run `dotnet test AssistantEngineer.sln`; JSON loader tests validate required fields, enum values, safety text, embedded resources, provenance, and seeded behavior.
8. Check for duplicate `manufacturer`/`seriesName`/`category`/`code` combinations before adding another entry.

## Provenance and Source Model

Every JSON entry must include a `source` block. This is separate from the diagnostic `confidence` value:

- `confidence` describes how strongly AssistantEngineer should trust the diagnostic guidance.
- `evidenceLevel` describes the kind of evidence behind the entry.

Allowed `sourceType` values:

- `SeededEngineeringKnowledge`
- `ManufacturerDocumentation`
- `ServiceManual`
- `FieldObservation`
- `CrossCheckedManuals`

Allowed `evidenceLevel` values:

- `UnverifiedSeed`
- `ManualReferenced`
- `ManualPageVerified`
- `FieldObserved`
- `CrossChecked`

Current Gree GMV H5, Gree GMV C7, and Gree Chiller E6 entries use:

- `sourceType`: `SeededEngineeringKnowledge`
- `evidenceLevel`: `UnverifiedSeed`
- `confidence`: `Low`

They are deterministic seeded guidance only. They do not claim manual page verification, and their `source.manualTitle`, `source.page`, and `source.quote` fields remain `null`.

Rules for `ManualVerified`:

- `confidence = ManualVerified` is allowed only when `source.evidenceLevel` is `ManualPageVerified` or `CrossChecked`.
- `ManualPageVerified` requires a real `manualTitle` and `page`.
- Do not invent manual titles, versions, document codes, pages, sections, quotes, or citations.
- `quote` may be `null`; if present, it must be non-empty and must come from explicit manual evidence.
- `CrossChecked` requires notes or manual references explaining the cross-check.

How to add a manual-backed entry honestly:

1. Identify the exact installed equipment family and applicable manual.
2. Add the manual title/version/document code only if that evidence is explicit.
3. Add page and section only when the page/section is actually known.
4. Keep direct quotes short, exact, and only when necessary.
5. Set `evidenceLevel` no higher than the evidence supports.
6. Raise `confidence` to `ManualVerified` only after the module validation rules have explicit manual-page or cross-checked evidence.
7. Keep all safety notes intact and avoid protection-defeat instructions.

How to add seed entries honestly:

1. Use a small batch rather than a broad catalog dump.
2. Use `SeededEngineeringKnowledge` and `UnverifiedSeed`.
3. Keep `confidence = Low`.
4. Leave manual title, version, document code, page, section, and quote as `null`.
5. Keep wording preliminary and require verification against the exact service manual.
6. Include qualified-technician safety notes, required measurements, and source limitations.

Confidence levels:

- `Unknown`: insufficient knowledge to classify confidence.
- `Low`: preliminary deterministic seed, not manually verified.
- `Medium`: curated but still not exact manual-page verified.
- `High`: strong curated evidence, still below explicit manual verification.
- `ManualVerified`: reserved for future entries with explicit manual title/version/page evidence and audit rules.

## ED-07 Manual-Backed Staging Workflow

The staging workflow exists so manual evidence can be captured and reviewed before any entry reaches the runtime catalog. It does not add a public endpoint, database, import service, bot, or search engine.

Runtime source of truth:

- Production JSON files under `Knowledge/{manufacturer}/`.
- Embedded resources loaded by `EquipmentDiagnosticsJsonKnowledgeSource`.
- Application DTOs exposed through the existing read-only API routes.

Staging artifacts:

- Candidate JSON files under `Knowledge/staging/`.
- `equipment-diagnostics-staging.schema.json` for candidate shape and promotion rules.
- Templates under `Knowledge/staging/templates/`.

Staging files are not runtime data. `Draft` and `NeedsManualCheck` candidates stay in staging. `ApprovedForCatalog` is allowed only after source evidence is sufficient and the entry passes review.

To create a staging candidate from a service manual finding:

1. Verify manufacturer, series, category, model, and code.
2. Fill source evidence from the exact manual or cross-check record.
3. Confirm limitations, applicability, affected series, and affected models.
4. Keep instructions cautious, safe, and scoped to qualified technicians.
5. Run schema and module tests.
6. Copy the entry into a production catalog JSON file only after review.

Evidence level meanings in staging:

- `UnverifiedSeed`: placeholder or deterministic seed knowledge.
- `ManualReferenced`: a real manual is identified, but exact page evidence is not verified.
- `ManualPageVerified`: exact manual and page evidence are present.
- `CrossChecked`: multiple sources or records were compared and notes explain the check.

`ManualVerified` confidence is allowed only with `ManualPageVerified` or `CrossChecked` evidence. Do not invent manual titles, versions, document codes, pages, sections, or quotes. Do not store long copyrighted manual text; prefer short identifiers, page/section references, and a minimal quote only when needed for review.

## ED-08 Staging Validator And Promotion Guard

`EquipmentDiagnosticsStagingValidator` validates staging candidates before they can be considered for production catalog promotion. It is application-level code for tests, tooling, and future import workflows. It is not wired into the API and does not affect runtime search or catalog indexing.

Promotion rules:

- `Draft` and `NeedsManualCheck` are never runtime catalog entries.
- `ReadyForReview` means ready for engineering review, not runtime approval.
- `ApprovedForCatalog` requires sufficient source evidence and a validation pass.
- `ApprovedForCatalog` cannot use `UnverifiedSeed` evidence.
- `ManualVerified` requires `ManualPageVerified` or `CrossChecked` evidence.
- `ManualPageVerified` requires explicit `manualTitle` and `page`.
- `CrossChecked` requires evidence notes explaining the check.
- Candidate keys must not duplicate another staging candidate.
- Candidate keys must not conflict with production catalog entries unless a future explicit revision/update model is added.
- Promotion to production JSON must be a normal PR with tests.

The validator accepts staging JSON text or parsed candidate models. It reports deterministic issues with severity, code, path, and message. A valid `Draft` template may still produce an informational issue that it is not ready for runtime catalog use.

Validation result reports include:

- `TotalCandidates`
- `ErrorCount`, `WarningCount`, and `InfoCount`
- normalized `CandidateKeys`
- `IssuesByCandidateKey`
- `PromotionReady`
- `HasBlockingIssues`

Reviewers should treat `HasBlockingIssues = true` as a stop signal. `PromotionReady = false` is expected for draft and ready-for-review candidates; it means the candidate is still a review artifact, not runtime catalog data.

## ED-09A Catalog Expansion Pack 1

ED-09A increases the runtime catalog while preserving the same provenance rules:

- runtime JSON remains the approved source of truth;
- staging JSON remains excluded from runtime loading;
- all newly added records are unverified seed entries;
- no new public endpoints, persistence, Telegram, AI, RAG, or vector search are introduced;
- no manual-backed claims are made without exact source evidence.

Future manual-backed replacements or additions should start as staging candidates, pass `EquipmentDiagnosticsStagingValidator`, and move to production JSON only through a normal PR with tests. If a future entry uses `ManualVerified`, it must have `ManualPageVerified` or `CrossChecked` evidence and must not invent source metadata.

## ED-10A Manual-Ingestion And Bot-Readiness

ED-10A keeps runtime behavior Git-reviewed and deterministic:

1. Capture a manual observation as a staging candidate under `Knowledge/staging/`.
2. Keep placeholder or incomplete candidates at `Draft`, `NeedsManualCheck`, or `ReadyForReview`.
3. Run the staging validator and review the grouped report by candidate key.
4. Promote only through production JSON after evidence is sufficient, validator checks pass, and a normal PR review approves the change.
5. Keep runtime `ManualVerified` reserved for exact `ManualPageVerified` or `CrossChecked` evidence.

Future bot/UI flows should use the existing API in this order:

1. Search with `GET /api/v1/equipment-diagnostics/error-codes?manufacturer=Gree&query=...`.
2. Fetch the selected case with `GET /api/v1/equipment-diagnostics/cases`.
3. Render `shortSummary`, `sourceSummary`, `confidenceExplanation`, `recommendedNextChecks`, `safetyBoundary`, and `verificationRequired`.
4. Avoid adding diagnostic statements that are not present in the response DTO.

Example deterministic query prompts:

- `gree gmv h5`
- `gmv outdoor e1`
- `chiller e6`
- `indoor h6`

## ED-10B Deterministic Operator Guidance Pack

ED-10B provides an application-level formatter for future Telegram, UI, or assistant clients. It is intentionally small: clients should still call the existing search and case endpoints, then pass the returned `EquipmentDiagnosticCaseDto` through the formatter if they need a structured operator message.

Formatter output:

- `Title`
- `Summary`
- `VerificationBanner`
- `SourceLine`
- `RecommendedChecks`
- `SafetyLine`
- `OperatorNotes`
- `Footer`

The formatter uses DTO fields only. It does not read domain records, staging files, JSON resources, controllers, or external services. It does not add diagnostic procedures that are absent from `diagnosticSteps`, `requiredMeasurements`, `recommendedNextChecks`, or `operatorNotes`.

For current Gree runtime entries, the formatter keeps seed status visible:

- `VerificationBanner` states that verification is required for seed knowledge.
- `SourceLine` includes `SeededEngineeringKnowledge / UnverifiedSeed`.
- `RecommendedChecks` is exactly the DTO `recommendedNextChecks` collection.
- `SafetyLine` is exactly the DTO `safetyBoundary`.
- `Footer` is taken from the final DTO operator note when available.

This keeps future assistant/operator surfaces deterministic and auditable while manual-backed evidence, persistence, and UI integrations remain separate future work.

## ED-10C Contract Examples And Release-Readiness Guards

ED-10C locks down the consumption contract for future bot, UI, or Telegram surfaces without adding endpoints or runtime integrations.

Bot/UI clients should rely on these diagnostic case fields:

- `shortSummary`
- `recommendedNextChecks`
- `confidenceExplanation`
- `sourceSummary`
- `applicabilitySummary`
- `safetyBoundary`
- `operatorNotes`
- `isManualVerified`
- `isSeedKnowledge`
- `verificationRequired`
- `source.sourceType`
- `source.evidenceLevel`
- `source.limitations`
- `source.applicableSeries`
- `source.applicableModels`

For formatter-based clients, use `EquipmentDiagnosticOperatorGuidanceFormatter` and render:

- `Title`
- `Summary`
- `VerificationBanner`
- `SourceLine`
- `RecommendedChecks`
- `SafetyLine`
- `OperatorNotes`
- `Footer`

Examples under `docs/equipment-diagnostics/examples/` are deterministic contract documentation. They are intentionally small and should not be treated as runtime catalog data. They must not include real copyrighted manual text, invented manual titles/pages/quotes, or source metadata that is not present in the repository.

Future integrations must not generate their own diagnosis text. The safe flow remains:

1. Search deterministically through the existing error-code API.
2. Fetch the diagnostic case through the existing case API.
3. Render the DTO operator fields directly or pass the DTO into the deterministic formatter.
4. Keep seed/unverified status visible to the operator.
5. Require exact installed model, controller, and manual verification before final conclusion.

Manual-backed confidence still requires staging validation and reviewed promotion. Database/admin import, Telegram/UI integration, and RAG/manual evidence search remain postponed.

## ED-10D Final QA And Release Readiness

ED-10D is a cleanup and guardrail pass over the accumulated ED-10A/B/C work. It keeps the implementation scope unchanged and focuses on consistency:

- naming remains within the EquipmentDiagnostics application/module style;
- Application code stays free of Infrastructure, Telegram/UI, and AI/RAG dependencies;
- staging examples and docs examples are explicitly non-runtime;
- runtime knowledge still comes only from production JSON under `Knowledge/{manufacturer}/`;
- examples and staging samples are validated as JSON but are not embedded catalog resources;
- formatter tests guard that recommended checks and safety text are reused from DTO fields;
- QA tests guard that unverified seed entries do not invent manual evidence.

The existing public EquipmentDiagnostics API remains:

- `GET /api/v1/equipment-diagnostics/catalog`
- `GET /api/v1/equipment-diagnostics/error-codes`
- `GET /api/v1/equipment-diagnostics/cases`

No security endpoint inventory change is required because no route is added.

## ED-11A Manual Intake Pipeline

ED-11A turns the staging workflow into a repeatable local/CI engineering process without adding runtime infrastructure.

Run the full pipeline:

```powershell
.\scripts\equipment-diagnostics\verify-equipment-diagnostics.ps1
```

Available direct CLI commands:

- `validate-staging`
- `validate-runtime-catalog`
- `validate-doc-examples`
- `full-report`

The report includes runtime catalog totals, staging/docs file counts, normalized duplicate keys, candidate readiness, blocking issues, provenance/safety findings, and generic next actions. It intentionally omits nondeterministic timestamps.

Candidate readiness is one of:

- `NotReady`
- `ReadyForEngineeringReview`
- `ReadyForCatalogPromotion`
- `Blocked`

`ReadyForCatalogPromotion` means deterministic checks passed. It does not write runtime JSON and does not replace engineering review or a reviewed PR.

See [manual-intake-pipeline.md](manual-intake-pipeline.md) for the complete workflow.

## Automated Branch Readiness Verification

ED-11A+ adds one-command branch readiness verification for EquipmentDiagnostics work:

```powershell
.\scripts\dev\verify-branch-readiness.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics
```

The thin wrapper calls the EquipmentDiagnostics verification tool. It discovers branch, staged, unstaged,
and untracked files; applies the EquipmentDiagnostics scope policy; scans changed content for unsafe wording;
runs runtime/staging/docs QA; runs restore/build/test checks; and writes deterministic JSON and Markdown reports
under ignored `artifacts/verification/branch-readiness/`.

Developers do not need to manually copy diff, file-content, or search output into review notes. After the command
passes, the reviewed PR diff and GitHub checks remain the final promotion boundary. Staging candidates are never
promoted automatically. DB persistence, Telegram, AI/RAG, and vector search remain future work.

## ED-12A PR Automation And CI Readiness

Prepare a verified PR package with one command:

```powershell
.\scripts\dev\verify-and-prepare-pr.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics
```

The command fails fast unless branch readiness passes, then generates:

- `artifacts/verification/branch-readiness/branch-readiness-report.json`
- `artifacts/verification/branch-readiness/branch-readiness-report.md`
- `artifacts/verification/branch-readiness/pr-body.md`

The PR body is deterministic and derived from the readiness report. It contains bounded summaries, verification
commands, and the required safety/scope checklist without embedding raw command logs.

The focused `EquipmentDiagnostics Branch Readiness` GitHub Actions workflow reruns the same readiness policy for
relevant pull requests and uploads the ignored reports as CI artifacts. Developer flow is: create a feature
branch, implement, run the combined command, commit/push, use the generated PR body, and review the GitHub PR
diff/checks. Manual diff copy/paste is no longer required. Runtime catalog promotion still requires a reviewed
PR; DB, Telegram, AI/RAG, and vector search remain future stages.

## ED-13A GMV6 Manual-Backed Staging Pack

ED-13A begins real manual-backed intake for GMV6 outdoor diagnostics without changing the runtime catalog.

- Primary troubleshooting source: `Gree GMV6 DC Inverter VRF Units Service Manual`, document code `GC202001-I`.
- Secondary safety/owner confirmation source: GMV6 owner's manual.
- Indoor service manual `GC201711-V` is registry-only for a future indoor pack.
- Eight GMV6 outdoor candidates (`E1`, `E3`, `E4`, `F0`, `F1`, `F3`, `C5`, `C7`) remain `ReadyForReview` staging entries.
- Exact PDF/manual page and section anchors are recorded; long manual quotes are not stored.
- Local PDF files remain ignored under `artifacts/manual-intake/sources/` and are never committed.

The non-runtime source registry lives under `docs/equipment-diagnostics/manual-sources/`. Staging candidates are
not API/runtime knowledge and require a future reviewed promotion PR. No DB, Telegram, AI/RAG, or vector search
is introduced.

## API Routes

### Search Error Codes

`GET /api/v1/equipment-diagnostics/error-codes`

Query parameters:

- `manufacturer` required
- `code` optional
- `series` optional
- `modelCode` optional
- `category` optional
- `query` optional, deterministic token-based search across manufacturer, series, category, model code, code, title, meaning, likely causes, and tags

Unknown manufacturers or codes return `200 OK` with an empty array.

Example:

```http
GET /api/v1/equipment-diagnostics/error-codes?manufacturer=Gree&code=H5
```

Free-text example:

```http
GET /api/v1/equipment-diagnostics/error-codes?manufacturer=Gree&query=gmv%20outdoor%20e1
```

Example response:

```json
[
  {
    "manufacturer": "Gree",
    "seriesName": "GMV",
    "modelCode": null,
    "code": "H5",
    "title": "GMV protection alarm H5",
    "meaning": "Preliminary diagnostic entry for a Gree GMV H5 alarm. Verify the exact meaning against the service manual for the installed model before concluding.",
    "severity": "Service attention required",
    "category": 0,
    "confidence": 1,
    "sourceManual": null
  }
]
```

### Get Diagnostic Case

`GET /api/v1/equipment-diagnostics/cases`

Query parameters:

- `manufacturer` required
- `code` required
- `series` optional
- `modelCode` optional

If a case is not found, the endpoint returns `404 NotFound` with AssistantEngineer problem-details metadata.

Example:

```http
GET /api/v1/equipment-diagnostics/cases?manufacturer=Gree&series=GMV&code=H5
```

Example response excerpt:

```json
{
  "errorCode": {
    "manufacturer": "Gree",
    "seriesName": "GMV",
    "code": "H5",
    "confidence": 1
  },
  "likelyCauses": [
    "Outdoor unit protection condition reported by the control board."
  ],
  "diagnosticSteps": [
    {
      "order": 1,
      "title": "Confirm installed equipment identity",
      "instruction": "Record the outdoor unit model, GMV series, serial plate data, and controller-displayed error code.",
      "expectedResult": "Manufacturer, series, model, and displayed H5 code are confirmed before diagnosis continues.",
      "ifFailedAction": "Stop classification and obtain the exact model information."
    }
  ],
  "requiredMeasurements": [
    {
      "name": "Supply voltage",
      "unit": "V",
      "requiredBeforeConclusion": true
    }
  ],
  "safetyNotes": [
    "Electrical, compressor, inverter, refrigerant, and chiller protection checks must be performed by a qualified technician."
  ],
  "shortSummary": "Gree GMV H5: GMV protection alarm H5. Preliminary diagnostic entry for a Gree GMV H5 alarm. Verify the exact meaning against the service manual for the installed model before concluding.",
  "recommendedNextChecks": [
    "Step 1: Confirm installed equipment identity - Record the outdoor unit model, GMV series, serial plate data, and controller-displayed error code.",
    "Record Supply voltage (V) before drawing a conclusion."
  ],
  "confidenceExplanation": "Low confidence seeded guidance: use as preliminary diagnostic support only and verify the exact installed model, controller, and service manual before drawing a conclusion.",
  "sourceSummary": "SeededEngineeringKnowledge / UnverifiedSeed. No manual title/page evidence is attached to this runtime entry.",
  "applicabilitySummary": "Applicable series: GMV; no specific applicable models listed. Limitations: Use as preliminary diagnostic guidance only. Verify against the exact service manual for the installed equipment family before final conclusion.",
  "safetyBoundary": "VRF outdoor electrical, compressor, inverter, refrigerant, and protection checks must stay within qualified-technician service scope.",
  "operatorNotes": [
    "Do not treat this response as a final diagnosis.",
    "Verify the installed model, controller, and exact service manual before final conclusion.",
    "Record required measurements before drawing a conclusion.",
    "This runtime entry is deterministic seed knowledge and is not manual page verified."
  ],
  "isManualVerified": false,
  "isSeedKnowledge": true,
  "verificationRequired": true,
  "source": {
    "sourceType": "SeededEngineeringKnowledge",
    "evidenceLevel": "UnverifiedSeed",
    "manualTitle": null,
    "page": null,
    "quote": null,
    "notes": "Seeded deterministic diagnostic guidance. Not manually page-verified.",
    "limitations": [
      "Use as preliminary diagnostic guidance only.",
      "Verify against the exact service manual for the installed equipment family before final conclusion."
    ],
    "applicableModels": [],
    "applicableSeries": [
      "GMV"
    ]
  },
  "confidence": 1
}
```

### Get Catalog Index

`GET /api/v1/equipment-diagnostics/catalog`

Returns deterministic catalog facets for UI filters, bot menus, and predictable discovery flows. It exposes application DTOs only, not domain records.

Example:

```http
GET /api/v1/equipment-diagnostics/catalog
```

Example response excerpt:

```json
{
  "totalEntries": 23,
  "manufacturers": [
    {
      "manufacturer": "Gree",
      "normalizedManufacturer": "GREE",
      "count": 23
    }
  ],
  "series": [
    {
      "manufacturer": "Gree",
      "normalizedManufacturer": "GREE",
      "seriesName": "Chiller",
      "normalizedSeriesName": "CHILLER",
      "count": 5
    },
    {
      "manufacturer": "Gree",
      "normalizedManufacturer": "GREE",
      "seriesName": "GMV",
      "normalizedSeriesName": "GMV",
      "count": 14
    }
  ],
  "categories": [
    {
      "category": 0,
      "count": 6
    }
  ],
  "codes": [
    {
      "manufacturer": "Gree",
      "seriesName": "GMV",
      "category": 0,
      "code": "E1",
      "normalizedCode": "E1",
      "confidence": 1,
      "sourceType": "SeededEngineeringKnowledge",
      "evidenceLevel": "UnverifiedSeed",
      "count": 1
    }
  ],
  "sourceTypes": [
    "SeededEngineeringKnowledge"
  ],
  "evidenceLevels": [
    "UnverifiedSeed"
  ]
}
```

## Non-Goals

- No EF Core persistence or migrations.
- No Telegram bot integration.
- No AI, RAG, vector search, or semantic search.
- No calculation-physics changes.
- No protection-defeat or safeguard-deactivation instructions.

## Safety Notes

- Electrical, compressor, inverter, refrigerant, and chiller protection checks must be performed by qualified technicians.
- Diagnostics must keep safety switches, protection inputs, current protection, pressure protection, flow protection, and controller safeguards active during diagnosis.
- Seed entries are preliminary unless an exact manual reference exists in the repository. They must not claim `ManualVerified` confidence without explicit source evidence.
- ED-01 remains deterministic seeded knowledge only. It does not claim full manual verification, and it does not add Telegram, RAG/vector search, or persistence.
- ED-02 keeps seeded knowledge in a deterministic in-memory catalog. Entries remain low-confidence unless explicit source evidence is added later.
- ED-03 keeps seeded knowledge in deterministic embedded JSON files. It does not add persistence, Telegram, RAG/vector search, AI search, or full manual verification claims.
- ED-04 adds source/provenance metadata. Current entries remain deterministic seed knowledge, not manual verified.
- ED-05 expands Gree GMV seed coverage with a small batch of low-confidence entries. It does not add manual-backed claims.
- ED-06 adds deterministic catalog indexing for query/filter discovery. It does not add persistence, Telegram, RAG/vector search, AI search, or manual-backed claims.
- ED-07 adds manual-backed staging artifacts for review and promotion. It does not alter runtime catalog loading or public API routes.
- ED-08 adds staging candidate validation and promotion guards. It does not alter runtime catalog loading, public API routes, or calculation behavior.
- ED-09A expands Gree GMV outdoor, Gree Chiller, and Gree Indoor seed catalog coverage. It does not add manual-backed claims or runtime infrastructure.
- ED-09B improves deterministic search normalization, free-text query matching, and catalog QA tests. It does not add persistence, Telegram, RAG/vector search, AI search, or manual-backed claims.
- ED-09C adds deterministic operator-facing case response fields for summaries, next checks, confidence/source explanation, applicability, safety boundaries, and verification flags. It does not add public routes, persistence, Telegram, RAG/vector search, AI search, or manual-backed claims.
- ED-10A improves manual-ingestion staging examples, validation report summaries, and deterministic assistant/bot-readiness guidance. It does not add runtime persistence, Telegram integration, AI/RAG/vector search, new public endpoints, or manual-backed runtime claims.
- ED-10B adds an application-level deterministic operator guidance formatter over existing diagnostic case DTO fields. It does not add API routes, persistence, Telegram integration, AI/RAG/vector search, DTO contract changes, or manual-backed runtime claims.
- ED-10C adds deterministic contract examples and release-readiness guards for EquipmentDiagnostics API DTOs, formatter output, staging reports, and runtime/example separation. It does not add API routes, persistence, Telegram integration, AI/RAG/vector search, or manual-backed runtime claims.
- ED-10D adds final consistency, runtime-pollution, safety/provenance, formatter, and route-surface QA guards. It does not add API routes, persistence, Telegram integration, AI/RAG/vector search, or manual-backed runtime claims.
- ED-11A adds a deterministic C# manual-intake verification/report pipeline and thin script wrapper. It does not automatically promote entries or add API routes, persistence, Telegram integration, AI/RAG/vector search, or manual-backed claims without evidence.

## Future Stages

- ED-11: Telegram, assistant, or UI UX on top of the existing facade/API and deterministic formatter, without moving diagnostics into the Equipment catalog module.
- ED-12: database projection and admin/manual import through a dedicated Infrastructure adapter and migration.
- ED-13: RAG/manual evidence search only after deterministic source-backed data, safety/provenance rules, and manual evidence governance justify it.
- Add audit and confidence rules for manual-backed content before any `ManualVerified` claim is allowed.
