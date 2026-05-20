# Ownership Backfill Apply Enablement Architecture Review

## Purpose

This document defines architecture review requirements that must be accepted before any future apply code enablement, while write-path remains disabled.

Canonical release-boundary reference: `docs/security/security-release-boundary.md`.

## Scope

This review framework covers:

- CLI boundary and apply disabled boundary;
- staging/production environment separation;
- test-only executor boundary;
- future staging executor boundary;
- future production executor boundary;
- evidence/hash-chain invariants;
- rollback invariants;
- logging and secret-handling invariants;
- DB write-path invariants;
- governance test requirements.

## Non-claims

- No production apply enabled claim.
- No staging apply execution claim.
- No production ownership backfill execution claim.
- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Current status

- CLI `apply` remains disabled.
- Test-only executor exists but is not CLI production apply.
- Staging executor remains disabled.
- Production executor is not enabled.
- No DB write path is wired.
- Manual decision log is governance-only and not executable.

## Architecture enablement invariants

1. `ApplyDisabledInvariant`:
- CLI apply must remain disabled until an explicit future enablement stage.

2. `EnvironmentHardDenyInvariant`:
- Production must be hard-denied by default.
- Unknown environment must be hard-denied.
- Staging must be explicit.

3. `HashChainInvariant`:
- `ProductionPromotionHash`, `ApplyInputHash`, `PlanHash`, `StagingRunHash` must match decision packet references.

4. `EvidenceCompletenessInvariant`:
- dry-run, gate, plan, signoff, readiness, previous-values, staging acceptance, and production promotion decision artifacts must exist before future enablement consideration.

5. `RollbackCompletenessInvariant`:
- each planned update must have previous-values snapshot coverage and explicit rollback owner.

6. `NoSecretsInvariant`:
- no connection strings, tokens, passwords, API keys, or secrets in logs/artifacts.

7. `NoPayloadInvariant`:
- no workflow/report/artifact raw payload logging in ownership backfill artifacts.

8. `NoDestructiveSqlInvariant`:
- no destructive raw SQL (`DELETE`, `TRUNCATE`, destructive `UPDATE`) in ownership backfill tooling.

9. `NoGlobalFilterInvariant`:
- no silent global EF query filter rollout as a side-effect of ownership backfill enablement work.

10. `NoProductionWiringInvariant`:
- production write executor must not be wired to CLI apply before explicit future stage approval.

## Forbidden architecture changes

Forbidden before explicit future code enablement:

- wiring CLI apply to production executor;
- using production connection strings in test-only executor paths;
- enabling `SaveChanges`/`SaveChangesAsync` in CLI apply path;
- adding raw `UPDATE`/`DELETE`/`TRUNCATE` SQL for ownership backfill;
- inferring environment from connection string content;
- logging connection strings/secrets/payloads;
- adding global query filters as backfill side-effects;
- adding DB RLS as backfill side-effects;
- bypassing signoff/readiness/promotion/manual decision gates.

## Required architecture review checklist

- CLI disabled regression test passes.
- No production executor wiring exists.
- No `SaveChanges` outside explicitly test-only scopes.
- No destructive SQL patterns.
- No connection string logging.
- Hash-chain validator coverage exists.
- Manual decision docs/templates exist.
- Production promotion validator exists.
- Staging acceptance validator exists.
- Rollback completeness checks exist.
- Generated artifacts are ignored.
- Release-ready verification passes.

## Future code enablement criteria

Before any future code enablement:

- P6-15 architecture review checklist is accepted;
- explicit code enablement proposal is reviewed;
- write-path diff scope is explicitly reviewed;
- staging-only enablement is evaluated before any production enablement;
- production enablement is reviewed as a separate stage;
- rollback command design is reviewed;
- audit logging design is reviewed;
- operator runbook updates are reviewed.

## Relationship to post-P6 audit

- P7-00 (`post-p6-governance-audit.md`) validates that these architecture invariants remain intact after P6 completion.
- P7-00 does not enable any write path and does not modify CLI apply disabled behavior.
- P7-01 (`security-release-boundary.md`) is the canonical source for enabled/disabled capability claims referenced by this review.
- P7-02 (`governance-test-consolidation-report.md`) consolidates repeated governance test assertions that enforce these invariants.
