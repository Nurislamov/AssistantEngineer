# Security Docs Map

## Purpose

This map provides a single navigation layer for P5/P6/P7 security governance artifacts, their ownership roles, and decision boundaries.

## Scope

- canonical release-boundary and governance index artifacts;
- stage evidence artifacts for route protection, tenant isolation, ownership metadata, and ownership backfill governance;
- templates and machine-readable registries;
- guardrail test linkage and ADR linkage.

## Non-claims

- No production security certification claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No certified/certification claim.

## How to read this map

- Use canonical artifacts first for capability/status interpretation.
- Use stage evidence artifacts for implementation history and constraints.
- Use templates for manual/governance packets only.
- Use registries/JSON artifacts for automation and tests.

## Canonical documents

- `docs/security/security-release-boundary.md`
- `docs/security/security-governance-index.md`
- `docs/security/security-governance-status-vocabulary.md`
- `docs/security/security-docs-map.md`
- `docs/adr/ADR-0001-security-governance-boundary.md`
- `docs/adr/security-architecture-decision-matrix.md`

## Stage evidence documents

- P5 staged route-protection rollout documents.
- P5 tenant-isolation integration and tenant-aware query/read integration documents.
- P6 ownership-backfill strategy, dry-run, gate, sign-off, readiness, staging, production, and manual decision documents.
- P7 audit and normalization documents (`post-p6-governance-audit`, release boundary/index normalization, route-inventory consistency).

## Templates

- `docs/security/ownership-backfill-architecture-review-checklist.md`
- `docs/security/ownership-backfill-change-request-template.md`
- `docs/security/ownership-backfill-staging-acceptance-checklist.md`
- `docs/security/ownership-backfill-manual-decision-log-template.md`

## Inventories and machine-readable registries

- `docs/security/production-saas-readiness-inventory.json`
- `docs/security/security-regression-guardrails.json`
- `docs/security/security-governance-index.json`
- `docs/security/security-governance-status-vocabulary.json`
- `docs/security/api-endpoint-protection-inventory.json`
- `docs/security/tenant-isolation-integration-matrix.json`
- `docs/adr/adr-index.json`

## Guardrails and tests

- Guardrail registry source: `docs/security/security-regression-guardrails.md/.json`.
- Route inventory and claims consistency automation: `P7RouteInventoryCoverageTests`, `P7RouteClaimsConsistencyTests`, `P7RouteOperationalCategoryConsistencyTests`, `P7RouteTenantScopeConsistencyTests`, `P7ProtectionStageConsistencyTests`.
- CI/release-ready observability guardrails: `P7CiGithubChecksVisibilityAuditTests`, `P7CiWorkflowInventoryTests`, `ReleaseReadyScriptObservabilityTests`.
- Apply disabled/no-write boundary guardrails remain enforced by P6/P7 architecture tests.

## Route protection documentation map

- Policy baseline: `docs/security/authorization-policy-rollout.md`.
- Stage evidence: `protected-endpoint-pilot`, `protected-read`, `protected-write`, `protected-execution`, `protected-report-artifact`, `protected-workflow-read-history`.
- Inventory/model: `api-endpoint-protection-inventory.md/.json` + `api-endpoint-classification-model.md`.

## Tenant isolation documentation map

- Scope model: `project-tenant-scoping-model.md`.
- Matrix and integration: `tenant-isolation-integration-matrix.md/.json`, `tenant-aware-query-isolation-services.md`, `tenant-aware-read-controller-integration.md`, `workflow-tenant-aware-read-integration.md`.
- Ownership metadata linkage: `workflow-ownership-metadata-coverage.md/.json`.

## Ownership backfill documentation map

- Strategy/evidence: `ownership-backfill-strategy.md`, `ownership-backfill-evidence-model.md`.
- Toolchain stages: dry-run, database scan, validate-evidence, plan-apply, signoff-plan, validate-apply-readiness.
- Governance progression: staging design, production promotion readiness, manual write-path decision, architecture review.

## Apply governance documentation map

- Canonical disabled boundary claim source: `security-release-boundary.md`.
- Apply mode and no-write controls: `ownership-backfill-apply-mode-design.md` plus P6 governance chain docs.
- Explicit boundary: staging/prod apply remain disabled; apply command remains disabled.

## CI/release-ready documentation map

- Release-ready observability: `release-ready-observability-audit.md/.json`.
- CI visibility contract and runbook: `ci-github-checks-visibility-audit.md/.json`, `ci-github-checks-visibility-runbook.md`.

## Decision records

- `docs/adr/ADR-0001-security-governance-boundary.md` records accepted governance boundary decisions and explicit non-goals.
- `docs/adr/adr-index.md/.json` tracks ADR discoverability and stage linkage.
- `docs/adr/security-architecture-decision-matrix.md/.json` consolidates accepted/deferred/rejected P5/P6/P7 architecture decisions.
- `docs/adr/future-security-adr-backlog.md/.json` tracks deferred future ADR items and trigger conditions.

## Known documentation limitations

- Some stage docs intentionally repeat short safety/non-claim statements for local clarity.
- Route inventory contains deferred/unknown classifications with explicit limitations and ignore-list governance.
- P6 governance chain remains broad; P7-08 is planned to consolidate ADR-level architecture decisions.

## Next steps

- Keep ADR-0001, decision matrix, and future ADR backlog synchronized when security boundary decisions change.
- Keep `security-docs-map` and ADR index updated when new governance stages are added.
