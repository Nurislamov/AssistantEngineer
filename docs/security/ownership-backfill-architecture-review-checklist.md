# Ownership Backfill Architecture Review Checklist (Template)

## Review summary

- ReviewId:
- ReviewDateUtc:
- Scope:

## Reviewed commit/range

- Repository:
- Branch:
- Commit/Range:

## Reviewer

- Primary reviewer:
- Additional reviewers:

## Architecture invariants

- `ApplyDisabledInvariant`:
- `EnvironmentHardDenyInvariant`:
- `HashChainInvariant`:
- `EvidenceCompletenessInvariant`:
- `RollbackCompletenessInvariant`:
- `NoSecretsInvariant`:
- `NoPayloadInvariant`:
- `NoDestructiveSqlInvariant`:
- `NoGlobalFilterInvariant`:
- `NoProductionWiringInvariant`:

## Forbidden changes check

- CLI apply -> production executor wiring check:
- `SaveChanges`/`SaveChangesAsync` in non-test-only apply path:
- Raw destructive SQL check:
- Environment inference from connection string check:

## CLI apply disabled check

- Command used:
- Exit code:
- Disabled message present:
- Connection string redaction verified:

## Write-path wiring check

- Production executor wiring evidence:
- Staging executor no-write disabled evidence:
- Test-only executor boundary evidence:

## Secrets/logging check

- Connection string leakage check:
- Secret/token/password/API key leakage check:
- Payload logging check:

## Hash-chain check

- `ProductionPromotionHash`:
- `ApplyInputHash`:
- `PlanHash`:
- `StagingRunHash`:
- Consistency result:

## Rollback completeness check

- Previous-values completeness:
- Rollback owner assigned:
- Rollback evidence reference:

## Test evidence

- Build result:
- Test suite result:
- Release-ready script result:
- Governance tests result:

## Decision

- DecisionStatus (`NotReady|Go|NoGo|Expired|Superseded`):
- Decision rationale:
- Required follow-up:

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
