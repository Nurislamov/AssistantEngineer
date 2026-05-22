# Security Governance Index

## Purpose

This index is the canonical navigation map for P5/P6/P7/P8 governance artifacts.

## Canonical release boundary

- `docs/security/security-release-boundary.md`
  - stage: `P7-01`
  - category: `Canonical release boundary`
  - status: `Implemented`
  - canonicalRole: `Canonical`
  - pointsToCanonicalBoundary: `true`

## Route protection

- `docs/security/authorization-policy-rollout.md`
- `docs/security/api-endpoint-classification-model.md`
- `docs/security/api-endpoint-protection-inventory.md`
- `docs/security/protected-endpoint-pilot-rollout.md`
- `docs/security/protected-read-endpoints-rollout.md`
- `docs/security/protected-write-endpoints-rollout.md`
- `docs/security/protected-execution-endpoints-rollout.md`
- `docs/security/protected-report-artifact-endpoints-rollout.md`
- `docs/security/protected-workflow-read-history-rollout.md`

## Tenant isolation

- `docs/security/project-tenant-scoping-model.md`
- `docs/security/tenant-isolation-integration-matrix.md`
- `docs/security/tenant-aware-query-isolation-services.md`
- `docs/security/tenant-aware-read-controller-integration.md`
- `docs/security/workflow-tenant-aware-read-integration.md`

## Ownership metadata

- `docs/security/persistence-backed-tenant-ownership-fields.md`
- `docs/security/workflow-ownership-metadata-coverage.md`

## Ownership backfill toolchain

- `docs/security/ownership-backfill-strategy.md`
- `docs/security/ownership-backfill-evidence-model.md`
- `docs/security/ownership-backfill-dry-run-tool.md`
- `docs/security/ownership-backfill-database-dry-run-scanner.md`
- `docs/security/ownership-backfill-cli-command-inventory.md`

## Apply governance

- `docs/security/ownership-backfill-evidence-validation-gates.md`
- `docs/security/ownership-backfill-apply-mode-design.md`
- `docs/security/ownership-backfill-apply-plan-generator.md`
- `docs/security/ownership-backfill-plan-signoff-gate.md`
- `docs/security/ownership-backfill-test-only-apply-rehearsal.md`
- `docs/security/ownership-backfill-apply-enablement-readiness.md`

## Staging governance

- `docs/security/ownership-backfill-staging-apply-runbook.md`
- `docs/security/ownership-backfill-staging-apply-executor-design.md`
- `docs/security/ownership-backfill-staging-post-run-evidence.md`

## Production governance

- `docs/security/ownership-backfill-production-apply-enablement-proposal.md`
- `docs/security/ownership-backfill-production-promotion-readiness.md`
- `docs/security/ownership-backfill-manual-write-path-enablement-decision.md`

## Architecture review

- `docs/security/ownership-backfill-apply-enablement-architecture-review.md`
- `docs/security/ownership-backfill-architecture-review-checklist.md`
- `docs/adr/ADR-0001-security-governance-boundary.md`
- `docs/adr/security-architecture-decision-matrix.md`
- `docs/adr/security-architecture-decision-matrix.json`
- `docs/adr/future-security-adr-backlog.md`
- `docs/adr/future-security-adr-backlog.json`
- `docs/adr/adr-index.md`
- `docs/adr/adr-index.json`
- `docs/architecture/engineering-domain-architecture-audit.md`
- `docs/architecture/assistantengineer-architecture-map.md`
- `docs/architecture/legacy-and-dead-code-inventory.md`
- `docs/architecture/scripts-tools-inventory.md`
- `docs/architecture/scripts-tools-rationalization.md`
- `docs/architecture/terminology-and-claims-vocabulary.md`
- `docs/architecture/terminology-claims-surface-cleanup.md`
- `docs/architecture/governance-test-brittleness-reduction.md`
- `docs/architecture/p8-engineering-domain-hardening-closure.md`

## Audit/release readiness

- `docs/security/production-saas-readiness-inventory.md`
- `docs/security/security-regression-guardrails.md`
- `docs/security/post-p6-governance-audit.md`
- `docs/security/security-governance-status-vocabulary.md`
- `docs/security/security-docs-map.md`
- `docs/security/security-docs-map.json`
- `docs/security/governance-test-consolidation-report.md`
- `docs/security/governance-test-consolidation-report.json`
- `docs/security/ownership-backfill-cli-command-inventory.json`
- `docs/security/release-ready-observability-audit.md`
- `docs/security/release-ready-observability-audit.json`
- `docs/security/ci-github-checks-visibility-audit.md`
- `docs/security/ci-github-checks-visibility-audit.json`
- `docs/security/ci-github-checks-visibility-runbook.md`
- `docs/security/route-inventory-claims-consistency-audit.md`
- `docs/security/route-inventory-claims-consistency-audit.json`
- `docs/architecture/engineering-domain-architecture-audit.json`
- `docs/architecture/assistantengineer-architecture-map.json`
- `docs/architecture/legacy-and-dead-code-inventory.json`
- `docs/architecture/scripts-tools-inventory.json`
- `docs/architecture/scripts-tools-rationalization.json`
- `docs/architecture/terminology-and-claims-vocabulary.json`
- `docs/architecture/terminology-claims-surface-cleanup.json`
- `docs/architecture/governance-test-brittleness-reduction.json`
- `docs/architecture/p8-engineering-domain-hardening-closure.json`

## Notes

- Stage documents remain as evidence/reference and are not deleted by normalization.
- Use `docs/security/security-release-boundary.md` for canonical enabled/disabled capability claims.
- Use `docs/security/security-governance-status-vocabulary.md` for normalized status values.
- Use `docs/architecture/terminology-and-claims-vocabulary.md` for canonical allowed/forbidden terminology and claim wording.
- Use `docs/architecture/governance-test-brittleness-reduction.md` for P8-08 semantic assertion migration boundaries and preserved strict guardrail list.
- Use `docs/architecture/p8-engineering-domain-hardening-closure.md` as the final P8 closure decision and deferred P9 backlog boundary.
