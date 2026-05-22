# Governance Test Brittleness Reduction (P8-08)

## Purpose

Reduce wording-coupled governance test fragility while preserving critical safety guardrails.

## Scope

This stage covers architecture/governance test assertions and supporting governance artifacts only.

## Non-claims

- No runtime behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No full tenant isolation claim.
- No production security certification claim.

## Brittleness sources

- Repeated markdown phrase assertions for JSON-backed governance artifacts.
- Repeated boolean-flag assertions duplicated across multiple P8 governance tests.
- Claims scans that depended on local per-test wording exceptions instead of shared allowed-context semantics.

## Refactoring approach

- Prefer semantic JSON/schema assertions for runtime/API/physics/no-write flags.
- Keep strict behavior-level tests for route signatures, status codes, CLI exit codes, and apply-disabled message.
- Centralize shared allowed-claim contexts and semantic assertion helpers in governance test helpers.

## Tests refactored

- `P8ScriptsToolsRationalizationTests`
- `P8RouteInventoryClassificationClosureTests`
- `P8EngineeringDomainArchitectureAuditTests`
- `P8TerminologyClaimsVocabularyTests`
- `P8TerminologyClaimsSurfaceCleanupTests`

## Guardrails preserved

- Forbidden positive-claim guardrails remain active.
- Route inventory coverage/consistency guardrails remain strict.
- Module-boundary matrix and dependency-direction guardrails remain strict.
- OwnershipBackfill apply-disabled, CLI redaction, and exit-code guardrails remain strict.
- Workflow controller and authorization gate characterization guardrails remain strict.

## Assertions converted to semantic checks

- Shared JSON false-flag checks via `GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse`.
- Shared non-claim concept checks via `GovernanceSemanticAssertions.AssertNonClaimsContainConcepts`.
- Shared artifact trio existence checks via `GovernanceSemanticAssertions.AssertDocumentArtifactsExist`.
- Shared allowed non-claim/forbidden-vocabulary contexts via `GovernanceClaimTestHelper.IsAllowedVocabularyOrNonClaimContext`.

## Assertions intentionally kept wording-based

- OwnershipBackfill apply disabled message exact text checks.
- Route template exact signature checks.
- Public DTO/property contract checks where exact names are part of the compatibility boundary.
- CLI command-name and exit-code consistency checks.

## Remaining brittle areas

- Large required-doc path lists in governance index tests still require periodic maintenance when new artifacts are added.
- Some section-header checks remain string-based by design for documentation structure guarantees.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`
- `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence .\artifacts\ownership-backfill\dry-run-local --gate-result .\artifacts\ownership-backfill\gate-local --output .\artifacts\ownership-backfill\apply-local --database-provider SQLite --connection-string "Data Source=fake.db;Password=super-secret"`

## Next steps

- Optional P8-09 final closure audit for remaining governance-test duplication and long index-list maintenance ergonomics.
