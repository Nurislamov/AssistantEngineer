# Ownership Backfill Change Request Template

## Change summary

- ChangeId:
- Change title:
- Scope summary:

## Environment

- Environment: `<staging|production>`
- Target database/provider:
- Scheduled window (UTC):

## Requested by

- Requester:
- Team:
- Date:

## Reviewers/approvers

- Engineering owner:
- Security/review owner:
- Database/release owner:
- Business/application owner:

## Evidence chain

- DryRunRunId:
- GateResultId:
- ReadinessId:
- Plan artifact path:
- Signoff artifact path:
- Previous-values artifact path:

## ApplyInputHash

- ApplyInputHash:
- RulesetVersion:

## PlanHash

- PlanHash:
- PlanId:

## SignoffId

- SignoffId:
- Signoff reviewer:
- Signoff ticket:
- Signoff TTL/expiry:

## ReadinessId

- ReadinessId:
- Readiness status:
- Readiness findings summary:

## Backup readiness

- BackupReference:
- Backup timestamp (UTC):
- Backup owner:
- Restore procedure evidence:
- Restore verification status:

## Rollback readiness

- Rollback owner:
- Previous-values completeness:
- Rollback rehearsal evidence:
- Rollback window estimate:

## Scheduled window

- Planned start (UTC):
- Planned end (UTC):
- Freeze/conflict notes:

## Go/no-go checklist

- [ ] Hash chain validated (`PlanHash`/signoff/`ApplyInputHash`)
- [ ] Signoff non-expired
- [ ] Unresolved threshold policy passed
- [ ] Ambiguous policy satisfied
- [ ] Previous-values completeness passed
- [ ] Backup completed
- [ ] Restore procedure verified
- [ ] Security regression tests passed
- [ ] Tenant isolation matrix tests passed
- [ ] Release window approved

## Approval signatures

- Engineering owner approval:
- Security/review approval:
- Database/release approval:
- Business/application approval:

## Post-run evidence

- This section is intentionally template-only for future stages.
- Do not include production secrets, tokens, or connection strings.
- Do not claim execution in P6-09.

## Non-claims

- No ownership backfill execution claim.
- No production apply enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.
