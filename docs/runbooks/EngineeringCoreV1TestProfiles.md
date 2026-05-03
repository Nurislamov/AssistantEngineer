# Engineering Core V1 Test Profiles

## Purpose

Engineering Core V1 now has many guard layers: formulas, diagnostics, API contracts, frontend visibility, report disclosure, export policy, validation registry, release evidence and traceability.

Running the full suite after every small edit is expensive.

This runbook defines practical test profiles.

## Profile 1: Smoke

Use this after small code/docs/frontend edits while iterating.

    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1

Without frontend build:

    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1 -SkipFrontend

Smoke checks:

- frontend build unless skipped;
- FormulaAudit;
- EngineeringCoreStatus;
- EngineeringCoreReportDisclosureTests;
- EngineeringCore diagnostics catalog API tests;
- frontend visibility guards;
- annual 8760 weather smoke;
- hourly heat-balance closure smoke.

## Profile 2: Contracts

Use this after changing docs, manifests, snapshots, reports, API contracts or generated artifacts.

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1

Without frontend build:

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1 -SkipFrontend

Without regenerating artifacts:

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1 -SkipRegenerate

Contracts checks:

- regenerates Engineering Core V1 artifacts;
- API contract snapshots;
- OpenAPI contract;
- report contract snapshots;
- report/export disclosure policy;
- diagnostics catalog;
- release manifest;
- release evidence package;
- traceability matrix;
- validation registry;
- documentation guards;
- CI/contribution guards.

## Profile 3: Fast full engineering-core verification

Use this before a normal engineering-core commit when full repository tests are not necessary yet.

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Fast mode runs all engineering-core guard layers but skips the final full backend test suite.

## Profile 4: Full verification

Use this before final commit, push, merge or release handoff.

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Full verification includes:

- frontend build;
- all engineering-core guard layers;
- generated artifact checks;
- full backend test suite.

## Artifact regeneration only

Use this when generated markdown/json snapshots may be stale:

    .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1

This runs:

- release evidence generator;
- API contract snapshot generator;
- report contract snapshot generator;
- export disclosure checklist generator;
- validation readiness generator;
- traceability matrix generator.

## Recommended workflow

During iteration:

    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1 -SkipFrontend

Before committing generated docs/contracts:

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1 -SkipFrontend

Before commit:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Before push/merge/release:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

## Do not remove guards to make tests faster

If tests become slow, split profiles.

Do not weaken:

- FormulaAuditMatrix guards;
- diagnostics Error/Warning/Info rules;
- annual 8760 requirements;
- calculationDisclosure visibility;
- frontend non-claim visibility;
- report/export disclosure guards;
- non-parity claims;
- traceability matrix.

## Profile 5: Validation

Use this when changing validation fixtures, registry, tolerances, comparison outputs or validation scripts.

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1

Regenerate only validation artifacts:

    .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1

Strict future mode requiring real EnergyPlus references:

    .\scripts\engineering-core\verify-engineering-core-v1-validation.ps1 -RequireRealReferences
