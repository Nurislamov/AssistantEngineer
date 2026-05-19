# Ownership Backfill Evidence Model

## Purpose

This document defines the evidence model for ownership backfill dry-run and future apply mode so readiness decisions are auditable and reproducible.

## Scope

The evidence model covers:

- dry-run summaries;
- apply summaries;
- unresolved record exports;
- previous-value snapshots for rollback preparation;
- storage, retention, redaction, and review expectations.

## Non-claims

- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Evidence artifact types

Required artifact types:

- `ownership-backfill-dry-run-summary.json`
- `ownership-backfill-dry-run-summary.md`
- `ownership-backfill-unresolved-records.json`
- `ownership-backfill-apply-summary.json`
- `ownership-backfill-previous-values.json`
- `ownership-backfill-evidence-gate-result.json`
- `ownership-backfill-apply-plan.json`
- `ownership-backfill-apply-summary-draft.json`
- `ownership-backfill-planned-records.json`
- `ownership-backfill-plan-signoff.json`

Run-scoped naming in tooling:

- P6-01 dry-run tool writes run-scoped variants such as `ownership-backfill-dry-run-summary-{runId}.json`.
- P6-03 evidence gate writes run-scoped variants such as `ownership-backfill-evidence-gate-result-{runId}.json`.
- P6-05 plan generator writes plan-scoped variants such as `ownership-backfill-apply-plan-{planId}.json`.
- P6-06 signoff gate writes signoff-scoped variants such as `ownership-backfill-plan-signoff-{signoffId}.json`.

P6-01 dry-run tool reference:

- `docs/security/ownership-backfill-dry-run-tool.md`
- `docs/security/ownership-backfill-database-dry-run-scanner.md`
- `docs/security/ownership-backfill-evidence-validation-gates.md`
- `docs/security/ownership-backfill-apply-plan-generator.md`
- `docs/security/ownership-backfill-plan-signoff-gate.md`

## Dry-run summary schema

Dry-run summary must include:

- run metadata (`runId`, timestamps, mode);
- total scanned/resolvable/unresolved counts;
- unresolved-by-reason map;
- per-record-type metrics;
- safety-threshold evaluation snapshot.

## Apply summary schema

Apply summary must include:

- run metadata (`runId`, timestamps, mode);
- target evidence reference used for apply authorization;
- total records evaluated/updated/skipped/failed;
- unresolved rate and threshold result;
- failure reasons summary;
- commit/rollback preparation pointers.
- Note: in P6-04 this is still design-only because apply mode remains disabled.

## Unresolved records schema

Unresolved records export must include:

- `recordType`;
- `recordId`;
- unresolved `reason`;
- optional ownership candidates:
  - `candidateProjectId`
  - `candidateBuildingId`
  - `candidateOrganizationId`

## Previous-values snapshot schema

Previous-values snapshot must include:

- `recordType`;
- `recordId`;
- `previousProjectId`;
- `previousBuildingId`;
- `previousOrganizationId`;
- `previousOwnerUserId`.

## Storage/retention guidance

- Store evidence in controlled artifact storage outside production payload stores.
- Keep immutable copies per run identifier.
- Retain dry-run evidence until the corresponding apply decision and governance review are complete.
- Retention tuning should be documented per environment and compliance requirements.

## Redaction policy

- No engineering payloads.
- No secrets/API keys/tokens.
- No raw request or result blobs.
- Identifiers only when required for traceability and rollback safety.

## Review checklist

- Dry-run artifacts exist and parse.
- Unresolved reasons are categorized and reviewed.
- Threshold policy outcome is documented.
- Apply prerequisites are satisfied before any write mode.
- No forbidden fields (payloads/secrets/tokens) are present.

## Future automation

- P6-01: generate dry-run evidence from tool skeleton.
- P6-02: generate read-only database dry-run evidence with unresolved/ambiguous coverage metrics.
- P6-03: validate dry-run evidence with threshold and ambiguity gates.
- P6-04: attach apply summary to governance gates (apply mode still disabled).
- P6-05: add deterministic apply-plan generator artifacts (`PlanHash`, summary draft, planned records) with no-write behavior.
- P6-06: add deterministic plan sign-off artifacts with reviewer/ticket and expiration policy metadata (no-write behavior).
