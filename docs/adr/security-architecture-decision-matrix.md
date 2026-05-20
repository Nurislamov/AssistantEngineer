# Security Architecture Decision Matrix

## Purpose

This matrix consolidates P5/P6/P7 security architecture decisions so boundary-critical choices are tracked in one place instead of being scattered across stage documents.

## Scope

- route protection decisions;
- tenant isolation decisions;
- ownership metadata decisions;
- ownership backfill tooling/governance decisions;
- apply/write-path governance decisions;
- staging/production separation decisions;
- release-ready/CI observability decisions;
- release-boundary claim decisions;
- future enforcement decisions that require separate ADR stages.

## Non-claims

- No production security certification claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production apply enabled claim.
- No staging apply execution claim.
- No ownership backfill execution claim.
- No certified/certification claim.

## Decision categories

- Route protection
- Tenant isolation
- Ownership metadata
- Ownership backfill tooling
- Apply/write-path governance
- Staging/production separation
- Release-ready/CI observability
- Claims/release boundary
- Future enforcement

## Accepted decisions

- Route protection remains options-controlled and is governed through staged rollout evidence.
- Selected read/write/execution/report/workflow endpoints are tracked in staged protection docs and inventory metadata.
- Project ownership fields exist as transition-safe metadata while legacy unscoped compatibility remains documented.
- Tenant-aware read services are integrated for selected protected read paths.
- Ownership backfill toolchain supports dry-run/evidence/gate/plan/signoff/readiness/promotion governance artifacts.
- Real ownership backfill apply/write-path remains intentionally disabled.
- Test-only apply rehearsal is not production apply and is not execution evidence.
- Global EF query filters are deferred.
- Database row-level security is deferred.
- Full tenant isolation is not claimed.

## Deferred decisions

- Real staging apply enablement.
- Real production apply enablement.
- Real ownership backfill execution.
- Global EF query filter rollout.
- Database row-level security rollout.
- External identity provider integration.
- Production-grade durable audit persistence.
- Post-backfill strict tenant enforcement rollout.

## Rejected alternatives

- Enable apply immediately after dry-run evidence only.
- Use only manual prose docs without machine-readable guardrails/tests.
- Enable global EF query filters before ownership/backfill evidence chain is complete.
- Claim full tenant isolation before backfill/write-path enforcement exists.
- Treat test-only apply rehearsal as production readiness.

## Future ADR-required decisions

- ADR-FUTURE-001: Staging ownership backfill apply enablement.
- ADR-FUTURE-002: Production ownership backfill apply enablement.
- ADR-FUTURE-003: Global EF query filter enablement.
- ADR-FUTURE-004: Database row-level security enablement.
- ADR-FUTURE-005: External identity provider integration.
- ADR-FUTURE-006: Production audit log persistence.
- ADR-FUTURE-007: Full tenant isolation claim eligibility.
- ADR-FUTURE-008: Post-backfill strict tenant enforcement.

## Cross-links to security docs

- `docs/security/security-release-boundary.md`
- `docs/security/security-docs-map.md`
- `docs/security/security-governance-index.md`
- `docs/security/route-inventory-claims-consistency-audit.md`
- `docs/security/ownership-backfill-apply-enablement-architecture-review.md`
- `docs/security/production-saas-readiness-inventory.md`

## Release boundary relationship

This matrix is subordinate to the canonical release boundary and ADR-0001 umbrella decision. Any attempt to change disabled boundary flags requires a dedicated future ADR and separate staged approval.

## Known limitations

- This matrix is governance evidence, not runtime enforcement.
- Deferred items remain intentionally unresolved until prerequisite evidence and explicit ADR decisions exist.
- Matrix entries summarize decisions and cross-link to full stage evidence; they are not replacements for stage detail docs.

## Next steps

- Keep matrix entries aligned with new ADRs and P7/P8 stage outcomes.
- Promote deferred items only through explicit future ADR records with evidence-trigger conditions.