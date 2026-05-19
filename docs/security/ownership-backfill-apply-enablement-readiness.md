# Ownership Backfill Apply Enablement Readiness

## Purpose

This document defines the P6-08 readiness gate for future real apply enablement. It validates artifact consistency and rollback-readiness without enabling production apply.

## Scope

P6-08 scope includes:

- dry-run, gate, plan, signoff, and previous-values artifact chain validation;
- deterministic ApplyInputHash generation;
- PlanHash/signoff hash consistency checks;
- signoff TTL policy checks;
- previous-values completeness and rollback-readiness checks;
- readiness result artifact generation.

## Non-claims

- No ownership backfill execution claim.
- No production apply enabled claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production security certification claim.
- No certified/certification claim.

## Command usage

```powershell
dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-apply-readiness \
  --dry-run <dry-run-summary-json> \
  --gate-result <gate-result-json> \
  --plan <apply-plan-json> \
  --signoff <plan-signoff-json> \
  --previous-values <previous-values-json> \
  --output <readiness-output-dir>
```

Optional:

- `--max-signoff-age-hours 24`
- `--require-rollback-readiness true`
- `--expected-plan-hash <hash>`
- `--ruleset-version P6-08`

## Required inputs

The readiness gate requires parseable artifacts for:

- dry-run summary;
- evidence gate result;
- apply plan;
- plan signoff;
- previous-values snapshot list.

## Hash chain model

Readiness validates:

- `Signoff.PlanHash == Plan.PlanHash`;
- `--expected-plan-hash == Plan.PlanHash` when provided;
- deterministic `ApplyInputHash` over full normalized artifact chain.

Hash mismatch fails readiness.

## ApplyInputHash model

`ApplyInputHash` uses SHA256 over canonical normalized content:

- dry-run summary;
- gate result;
- plan;
- signoff artifact (excluding local execution context side effects);
- previous-values snapshots;
- ruleset version and stage token.

It does not include output paths, machine-local environment values, or current runtime timestamps.

## Sign-off TTL policy

Readiness validates signoff freshness using:

- signoff expiration (`ExpiresAtUtc`) when present;
- max signoff age (`--max-signoff-age-hours`, default `24`).

Expired or stale signoff fails readiness.

## Previous-values completeness

Readiness verifies previous-values coverage for planned records:

- planned record identity must exist in previous-values snapshot set;
- completeness is emitted as a metric;
- missing snapshots become blocking findings when rollback-readiness is required.

## Rollback-readiness checks

When `--require-rollback-readiness=true`:

- previous-values completeness must be 100%;
- no missing planned-record snapshots are allowed;
- no unresolved/ambiguous planned records are allowed;
- no current-value conflict readiness blockers are allowed.

## Failure codes/findings

`validate-apply-readiness` exits with:

- `0` when readiness passes;
- `2` when readiness checks fail;
- `1` on invalid command/input or parse errors.

Findings include severity and expected/actual values where relevant.

## Generated readiness artifacts

Readiness output files:

- `ownership-backfill-apply-readiness-result-{readinessId}.json`
- `ownership-backfill-apply-readiness-result-{readinessId}.md`

Artifacts include:

- pass/fail state;
- PlanHash and ApplyInputHash;
- signoff metrics;
- previous-values completeness metrics;
- findings and non-claims.

## Safety guarantees

- no DB connection is required;
- no DB writes are executed;
- apply command remains disabled and non-zero;
- no secret/token/payload-like fields are allowed in validated artifacts.

## Relationship to future apply enablement

P6-08 readiness is required governance evidence but does not enable apply by itself. Future apply enablement must require:

- passed evidence gate;
- passed plan signoff gate;
- passed apply-readiness result;
- matching PlanHash and ApplyInputHash across inputs;
- accepted production/staging enablement proposal and change-management policy.
- accepted staging acceptance checklist evidence as the next governance gate.

Staging policy note:

- readiness gate is required for future staging apply consideration.

Readiness output handling requirement:

- `ApplyInputHash` from readiness output must be copied into the change-management request for any future enablement decision.
- readiness pass alone does not authorize staging or production apply.

## Known limitations

- readiness validation does not execute ownership writes;
- readiness validation does not replace production change-management approval;
- readiness outcome is artifact-based and depends on evidence quality.

## Next steps

- P6-09: production apply enablement proposal (still disabled) with explicit policy and staged rollout controls.
