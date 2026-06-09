# Equipment Diagnostics Manual Intake Pipeline

## Manual codebook extraction

After a local manual is registered and its cover identity is checked, reviewed code occurrences may be recorded in `Knowledge/manual-codebook`. The codebook is non-runtime and preserves source/page/section plus classification and promotion readiness. Only occurrences with separately reviewed troubleshooting evidence may move into the staging-candidate workflow.

### Coverage gate

ED-14B adds a deterministic coverage gate between codebook extraction and staging review. It reports whether an occurrence is already covered, requires manual or troubleshooting evidence, must remain reference-only, or conflicts with another same-context source. Candidate readiness is only a recommendation; promotion remains a separate reviewed change.

### Evidence assessment and preview

ED-14C evaluates each occurrence against explicit source-usage and evidence-completeness rules. Ready assessments may appear in an ignored generated preview, never in production staging or runtime knowledge. Owner/controller/tool/technical-guide evidence remains supporting context, and unresolved conflicts or unsafe text block preview generation.

ED-11A provides a deterministic manual-intake, validation, and promotion-readiness pipeline for EquipmentDiagnostics.

The pipeline does not automatically modify the runtime catalog. A reviewed pull request remains required for every runtime catalog change.

## Boundaries

- Runtime knowledge source of truth: Git-reviewed production JSON under `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Knowledge/{manufacturer}/`.
- Staging candidates: non-runtime JSON under `Knowledge/staging/`.
- Contract examples: non-runtime JSON under `docs/equipment-diagnostics/examples/`.
- Verification logic: C# application report engine and dedicated CLI tool.
- PowerShell wrapper: thin command launcher only.

The pipeline does not add a database, EF migration, admin import endpoint, Telegram integration, AI/RAG/vector search, or public API endpoint.

## Add A Staging Candidate

1. Start from `Knowledge/staging/templates/gree-manual-entry.template.json`.
2. Create a candidate JSON outside `templates/` and `examples/` under `Knowledge/staging/`.
3. Record manufacturer, series, category, model applicability, code, source evidence, limitations, safe diagnostic steps, required measurements, and safety notes.
4. Keep incomplete findings at `Draft`, `NeedsManualCheck`, or `ReadyForReview`.
5. Do not use placeholder evidence in a real intake candidate.
6. Do not claim `ManualVerified` without `ManualPageVerified` or `CrossChecked` evidence.
7. Do not store long copyrighted manual quotes.

## Run Verification

Full verification:

```powershell
.\scripts\equipment-diagnostics\verify-equipment-diagnostics.ps1
```

Direct CLI commands:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- validate-staging
dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- validate-runtime-catalog
dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- validate-doc-examples
dotnet run --project .\tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- full-report
```

The CLI writes a deterministic JSON report to stdout. It does not create or modify catalog files.

## Read The Report

The report contains:

- runtime catalog total, seed count, manual-verified count, and duplicate normalized keys;
- staging candidate/example file counts;
- docs example file count;
- candidate keys and promotion readiness;
- errors, warnings, and informational findings;
- suggested generic next actions;
- `isReleaseReady` and `hasBlockingIssues`.

Promotion readiness states:

- `NotReady`: candidate remains draft, incomplete, rejected, or otherwise not ready for review.
- `ReadyForEngineeringReview`: candidate passed deterministic validation but still requires engineering evidence review.
- `ReadyForCatalogPromotion`: approved candidate has sufficient evidence and passes deterministic checks.
- `Blocked`: candidate has blocking validation issues.

Suggested actions are workflow guidance only. They are not diagnostic operating instructions.

## Promotion Blockers

Promotion is blocked by conditions including:

- invalid JSON;
- duplicate candidate or runtime keys;
- conflict with an existing production key;
- unsafe diagnostic wording;
- placeholder evidence in a real intake candidate;
- `ManualVerified` without verified evidence;
- `ManualPageVerified` without exact manual title and page;
- `CrossChecked` without evidence notes;
- `ApprovedForCatalog` without sufficient external evidence;
- long quote-like manual text;
- missing limitations, required measurements, diagnostic steps, or safety notes.

Templates and examples remain non-runtime. The intentionally invalid staging example is reported as blocked example evidence, but it does not make the current production runtime catalog unreleasable.

## Manual Promotion

A `ReadyForCatalogPromotion` result is not automatic approval and does not write runtime JSON.

To promote:

1. Review source evidence and installed equipment applicability.
2. Confirm the candidate passes the staging validator and full verification report.
3. Copy the reviewed entry into the appropriate production JSON catalog.
4. Run EquipmentDiagnostics and full solution verification.
5. Submit the runtime catalog change through a reviewed pull request.

## Future Integrations

A future database/admin import workflow may consume the deterministic report, but it must preserve the same validation and reviewed-promotion rules.

Future Telegram/UI clients remain downstream consumers of approved runtime DTOs and deterministic formatter output. They must not read staging candidates as runtime truth or generate their own diagnosis text.

RAG/manual evidence search remains future work and must not bypass provenance, safety, or reviewed promotion.

## Automated Branch Readiness

Run the complete local readiness gate with:

```powershell
.\scripts\dev\verify-branch-readiness.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics
```

This command discovers committed branch changes plus staged, unstaged, and untracked working-tree files. It
classifies scope, blocks forbidden paths and unsafe user-facing diagnostic wording, runs EquipmentDiagnostics
catalog/staging/docs validation, and executes solution restore/build/test checks.

Deterministic reports are written to:

- `artifacts/verification/branch-readiness/branch-readiness-report.json`
- `artifacts/verification/branch-readiness/branch-readiness-report.md`

The `/artifacts/` tree is ignored. Reports are local/CI evidence and must not be committed. A passing report does
not promote a staging candidate; runtime catalog changes still require a reviewed PR. Future admin import,
database persistence, Telegram, UI, or RAG consumers must remain downstream of the same validation boundary.

## PR Automation And CI Readiness

Run:

```powershell
.\scripts\dev\verify-and-prepare-pr.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics
```

The combined wrapper first runs full branch readiness and stops on failure. On PASS it generates the deterministic
ignored PR body at `artifacts/verification/branch-readiness/pr-body.md`. The readiness report remains the source
of truth; the PR body is a bounded summary and checklist derived from it.

The focused GitHub Actions workflow repeats EquipmentDiagnostics tests and branch readiness for relevant pull
requests, then uploads the readiness report and PR body as workflow artifacts. Review uses the GitHub diff and
checks rather than manually copied local diff/log output. Staging/manual candidates remain non-runtime until a
reviewed promotion PR.

## ED-13A GMV6 Outdoor Manual Intake

The first real manual-backed staging pack uses the locally supplied GMV6 service manual:

- Manual ID: `gree-gmv6-service-manual-gc202001-i`
- Title: `Gree GMV6 DC Inverter VRF Units Service Manual`
- Document code: `GC202001-I`
- Usage: primary GMV6 outdoor troubleshooting evidence

The source registry is committed as metadata under `docs/equipment-diagnostics/manual-sources/`; PDF source files
remain ignored local artifacts. The owner manual is registered as a secondary safety/indication source. The indoor
service manual is registered only for a future indoor pack and is not referenced by GMV6 outdoor candidates.

Candidates remain `ReadyForReview` in staging. Exact page/section anchors support engineering review, but no
candidate is promoted automatically and no runtime entry becomes `ManualVerified` in ED-13A.
