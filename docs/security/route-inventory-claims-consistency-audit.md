# Route Inventory and Claims Consistency Audit

## Purpose

This audit formalizes route-inventory and claim-consistency automation for P7-06 so endpoint governance drift is detected early without changing runtime behavior.

## Scope

- API endpoint protection inventory structure and field completeness;
- endpoint-classification consistency for route/protection/auth/rate-limit/audit/tenant metadata;
- cross-document alignment with rollout docs, release boundary, and tenant isolation matrix;
- claims and non-claims consistency for route-protection posture.

## Non-claims

- No production security certification claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No certified/certification claim.

## Current route inventory sources

- `docs/security/api-endpoint-protection-inventory.json`
- `docs/security/api-endpoint-protection-inventory.md`
- Controller route attributes in `src/Backend/AssistantEngineer.Api/Controllers/**`
- Staged rollout references in P5 route-protection documents

## Endpoint classification model

- Canonical model: `docs/security/api-endpoint-classification-model.md`
- Required metadata fields include route/method/group/stage/permission/tenant scope/rate-limit/audit/claim context.

## Protection stage mapping

- Explicit mapping in inventory metadata uses: `P5-09`, `P5-10`, `P5-11`, `P5-12`, `P5-13`, `P5-14`, `P5-15`, `P5-16C`, `P5-16D`, `Deferred`, `Compatibility`, `Public`, `UnknownNeedsClassification`.

## Claims consistency rules

- Inventory and route-governance docs must not claim:
  - completed tenant-isolation coverage;
  - completed production security certification;
  - active DB row-level security enforcement;
  - active global EF query-filter enforcement;
  - completed ownership backfill execution;
  - active production apply enablement.
- Deferred and unknown classifications must include explicit known limitations.

## Release boundary relationship

- Canonical boundary remains `docs/security/security-release-boundary.md`.
- Route inventory metadata is evidence for staged protection posture, not proof of completed tenant isolation or production certification.

## Automation gaps

- Route discovery remains text-level and can miss edge cases in highly dynamic attribute patterns.
- Some inventory entries intentionally aggregate multiple methods (`MULTI`) and require future split refinement.
- Workflow/report/artifact scopes still include staged/deferred limitations by design.

## Implemented automation

- Route discovery helper and coverage test against inventory + explicit ignore list.
- Claims-consistency test for route docs/inventory posture.
- Rate-limit/audit category consistency checks against docs and resolver constants.
- Tenant-scope vocabulary and matrix alignment checks.
- Protection-stage vocabulary and rollout-stage alignment checks.

## Remaining limitations

- Text-level route discovery is conservative and not a full ASP.NET runtime endpoint graph.
- Inventory still contains staged `Deferred`/`UnknownNeedsClassification` entries that require future refinement.
- Automation prevents silent drift but does not claim complete runtime authorization coverage.

## Next steps

- P7-07 security docs map and decision record.
- Continue reducing `MULTI`/deferred inventory entries with explicit staged rollout updates.
