# Security Release Boundary

## Purpose

This document defines the canonical P5/P6/P7 security release boundary so stage documents can reference one source of truth for enabled and intentionally disabled capabilities.

## Scope

This boundary covers:

- route protection posture;
- tenant-aware read isolation posture;
- ownership-backfill governance/tooling posture;
- apply/write-path boundary;
- claim/non-claim policy;
- verification boundary for release assertions.

## Current boundary

- Route protection is options-controlled and staged by documented rollout flags.
- Tenant-aware read paths exist for selected protected Project/Building/Workflow read endpoints.
- Ownership backfill toolchain is governance, dry-run, validation-gate, plan, sign-off, readiness, and promotion-governance capable.
- Production apply is disabled.
- Staging apply is disabled.
- Real ownership backfill execution has not run.
- DB write-path for ownership backfill is not enabled.
- Global EF query filters are not enabled.
- Database row-level security is not enabled.
- No full multi-tenant isolation claim is made.

## Enabled capabilities

- Staged route protection governance and regression guardrails.
- Tenant-isolation matrix and tenant-aware read integration for selected protected paths.
- Ownership-backfill dry-run tooling and read-only DB scan.
- Evidence validation, plan generation, sign-off, readiness, staging acceptance, and production promotion governance commands.
- Test-only apply rehearsal in controlled test context.

## Intentionally disabled capabilities

- Production ownership-backfill apply execution.
- Staging ownership-backfill apply execution.
- Real ownership-backfill DB write-path.
- Global EF query-filter isolation rollout.
- DB row-level security rollout.
- Claim of completed tenant isolation.
- Production security certification claim.

## Allowed claims

- P6/P7 governance-ready boundary with write-path intentionally disabled.
- Dry-run, evidence-validation, planning, sign-off, and readiness governance capability.
- Test-only rehearsal capability.
- Staging and production apply remain disabled.

## Forbidden claims

- Production apply enabled.
- Staging apply execution enabled.
- Ownership backfill executed in real environment.
- DB write-path enabled for ownership backfill.
- Global EF query filters enabled for tenant isolation.
- Database row-level security enabled.
- Claiming tenant-isolation completion.
- Claiming production security certification completion.

## Runtime behavior boundary

- No runtime controller behavior is changed by governance-only stages.
- No public API route or DTO shape changes are implied by boundary documentation.
- CLI apply remains disabled and non-zero.

## Backfill/write-path boundary

- Ownership-backfill tool commands remain no-write for production/staging apply.
- Apply command remains intentionally disabled.
- No backfill execution evidence is asserted.

## Tenant isolation boundary

- Tenant-aware read controls exist for selected paths only.
- Full persistence-layer isolation is not claimed.
- Global EF query filters and DB RLS remain disabled.

## Release verification boundary

Release boundary assertions rely on:

- governance tests in `tests/AssistantEngineer.Tests/Architecture`;
- security guardrails registry (`docs/security/security-regression-guardrails.md/.json`);
- production SaaS readiness inventory (`docs/security/production-saas-readiness-inventory.md/.json`);
- route inventory and classification governance (`docs/security/api-endpoint-protection-inventory.json`, `docs/security/api-endpoint-classification-model.md`);
- post-P6 audit artifacts (`docs/security/post-p6-governance-audit.md/.json`).
- security docs map (`docs/security/security-docs-map.md/.json`).
- ADR records (`docs/adr/ADR-0001-security-governance-boundary.md`, `docs/adr/adr-index.md/.json`).
- ADR decision matrix and backlog (`docs/adr/security-architecture-decision-matrix.md/.json`, `docs/adr/future-security-adr-backlog.md/.json`).
- terminology and claims vocabulary (`docs/architecture/terminology-and-claims-vocabulary.md/.json`).

## Relationship to P5/P6/P7 docs

- This document is canonical for enabled/disabled capability claims.
- Stage documents keep stage-specific evidence and non-claims.
- Stage documents should reference this boundary instead of duplicating long status disclaimers where safe.
- Future write-path enablement cannot be approved by this document alone and requires a separate ADR stage decision.
- Changes to disabled boundary flags require explicit future ADR updates to ADR-0001 and the security architecture decision matrix.

## Non-claims

- No production apply enabled claim.
- No staging apply execution claim.
- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.
