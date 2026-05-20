# ADR-0001: Security governance boundary and ownership backfill write-path disabled state

## Status

Accepted

## Date

2026-05-20

## Context

P5/P6/P7 introduced staged security governance, protected-route rollout, tenant-aware read integration, ownership metadata coverage, and ownership backfill governance tooling. This created strong no-write guardrails but also a broad documentation and governance chain.

## Decision

- P5/P6/P7 security work establishes governance, tenant-aware reads, route protection, ownership metadata, and ownership backfill evidence tooling.
- Real ownership backfill write-path remains intentionally disabled.
- Staging and production apply are not enabled.
- Global EF query filters and DB row-level security are not enabled.
- Full tenant isolation is not claimed.
- Any future write-path enablement requires a separate ADR and separate stage approval.

## Scope

- Security governance boundaries and claims.
- Ownership backfill apply boundary.
- Staging/production enablement boundaries.
- Documentation and guardrail expectations for future stages.

## Non-goals

- Enabling production apply.
- Enabling staging apply.
- Executing real ownership backfill.
- Introducing global EF query filters.
- Introducing DB row-level security.
- Claiming production security certification.

## Consequences

- Safer release boundary and lower accidental write risk.
- Higher governance and test maintenance overhead.
- Clearer separation between evidence/governance stages and runtime enablement stages.
- Future real apply still requires explicit staged decision and separate ADR updates.

## Alternatives considered

- Enable apply immediately.
- Add global EF query filters now.
- Add DB RLS now.
- Collapse governance docs into one file.
- Keep status duplicated across all docs.

## Accepted claims

- Governance-ready boundary with write-path intentionally disabled.
- Dry-run/evidence/sign-off/readiness/promotion governance capabilities exist.
- Route protection and tenant-aware read integration are staged and documented.

## Explicit non-claims

- No production security certification claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No certified/certification claim.

## Follow-up decisions

- ADR-0002 (future): staged write-path enablement architecture decision (staging-first gate design).
- ADR-0003 (future): persistence-layer isolation decision (global filters vs alternatives).
- ADR-0004 (future): row-level security decision and operational ownership model.

## Decision matrix relationship

ADR-0001 is the umbrella boundary decision. The detailed accepted/deferred/rejected decision mapping is maintained in:

- `docs/adr/security-architecture-decision-matrix.md`
- `docs/adr/future-security-adr-backlog.md`

Any future change to disabled write-path boundary flags requires a separate future ADR and cannot be inferred from governance documentation alone.
