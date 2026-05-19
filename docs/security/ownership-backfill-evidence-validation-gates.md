# Ownership Backfill Evidence Validation Gates

## Purpose

This document defines P6-03 evidence-validation gates for ownership backfill dry-run outputs before any future apply-mode design is considered.

## Scope

The gate validates dry-run evidence structure, unresolved thresholds, ambiguous ownership findings, and required record-type metrics.

## Non-claims

- No ownership backfill execution claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Gate command usage

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-evidence --input <evidenceDir> --output <gateOutputDir>
```

Optional threshold overrides:

- `--summary <path>`
- `--max-total-unresolved-rate <0..1>`
- `--max-project-unresolved-rate <0..1>`
- `--max-scenario-unresolved-rate <0..1>`
- `--max-job-unresolved-rate <0..1>`
- `--max-ambiguous-records <int>`
- `--fail-on-missing-record-type-metrics <true|false>`
- `--fail-on-ambiguous-records <true|false>`
- `--fail-on-schema-mismatch <true|false>`

## Gate inputs

Required:

- dry-run summary JSON (`ownership-backfill-dry-run-summary-{runId}.json`);
- evidence input directory via `--input`.

Optional:

- unresolved records JSON (`ownership-backfill-unresolved-records-{runId}.json`);
- previous values JSON (`ownership-backfill-previous-values-{runId}.json`).

## Gate outputs

The gate writer emits run-scoped artifacts in `--output`:

- `ownership-backfill-evidence-gate-result-{runId}.json`
- `ownership-backfill-evidence-gate-result-{runId}.md`

## Default thresholds

- `MaxTotalUnresolvedRate = 0.05`
- `MaxProjectUnresolvedRate = 0.0`
- `MaxScenarioUnresolvedRate = 0.05`
- `MaxJobUnresolvedRate = 0.10`
- `MaxAmbiguousRecords = 0`
- `FailOnMissingRecordTypeMetrics = true`
- `FailOnAmbiguousRecords = true`
- `FailOnSchemaMismatch = true`

## Required metrics

Required record-type metrics:

- `Project`
- `Building`
- `WorkflowState`
- `Scenario`
- `Job`
- `JobEvent`
- `ScenarioHistory`

Summary consistency checks include:

- `Mode == DryRun`;
- non-negative totals;
- `TotalRecordsResolvable + TotalRecordsUnresolved <= TotalRecordsScanned`.

## Ambiguous record policy

Ambiguous ownership is blocking by default:

- metric-level ambiguous count must be `<= MaxAmbiguousRecords`;
- unresolved reason entries with ambiguous ownership fail when `FailOnAmbiguousRecords=true`.

## Failure codes/findings

Representative finding codes:

- `EVIDENCE_MODE_INVALID`
- `SUMMARY_TOTAL_MISMATCH`
- `REQUIRED_RECORD_TYPE_METRIC_MISSING`
- `TOTAL_UNRESOLVED_RATE_EXCEEDED`
- `PROJECT_UNRESOLVED_RATE_EXCEEDED`
- `SCENARIO_UNRESOLVED_RATE_EXCEEDED`
- `JOB_UNRESOLVED_RATE_EXCEEDED`
- `AMBIGUOUS_COUNT_EXCEEDED`
- `AMBIGUOUS_REASON_PRESENT`
- `NON_CLAIMS_MISSING`
- `UNRESOLVED_RECORD_FORBIDDEN_FIELDS`

## Exit codes

- `0`: gate passed.
- `2`: gate failed (blocking findings).
- `1`: invalid command/input or execution error.

## Relationship to future apply mode

P6-03 does not enable apply mode. Evidence gates are a prerequisite for future apply-mode design and governance in later P6 steps, and future apply mode must require a passed gate result before any write path is considered. P6-05 `plan-apply` also requires a passed gate result and exits with code `2` when gate status is failed. P6-06 `signoff-plan` makes passed gate evidence necessary but not sufficient; signed plan governance is also required.

## Known limitations

- Gate validates evidence quality, not full business correctness of every ownership decision.
- Apply mode remains disabled.
- No DB writes are performed by gate validation.
- Plan generation is allowed only for passed gate evidence; apply execution remains disabled.

## Next steps

- P6-04: define apply-mode design (still disabled) with evidence-gate prerequisites.
- P6-05: add CI automation to enforce evidence-gate policies for staged datasets.
