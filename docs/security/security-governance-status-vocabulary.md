# Security Governance Status Vocabulary

## Purpose

This document defines canonical status terms for P5/P6/P7 security governance artifacts to reduce naming drift.

## Statuses

- `Implemented`
  - meaning: stage deliverable is implemented in current repository scope.
  - allowed usage: completed tooling, tests, or governance artifacts.
  - not meaning: production write-path or full security completion.
- `AuditOnly`
  - meaning: audit/review artifact with findings and boundaries.
  - allowed usage: post-stage audit reports and audit checkpoints.
  - not meaning: feature enablement.
- `DesignOnly`
  - meaning: design/contract artifact without execution enablement.
  - allowed usage: staged runbook/executor/preflight design docs.
  - not meaning: runtime behavior change.
- `StrategyOnly`
  - meaning: strategy/policy definition before implementation.
  - allowed usage: strategy and planning baselines.
  - not meaning: execution complete.
- `GovernanceOnly`
  - meaning: policy/proposal/decision framework without runtime activation.
  - allowed usage: approval policy, proposal, manual decision governance, architecture review governance.
  - not meaning: write-path enabled.
- `ToolingOnly`
  - meaning: tooling support exists without runtime feature enablement.
  - allowed usage: dry-run/readiness/validator commands and supporting artifacts.
  - not meaning: production apply execution.
- `TestOnly`
  - meaning: behavior available only in controlled test contexts.
  - allowed usage: test-only rehearsal executors.
  - not meaning: staging/production execution enablement.
- `DisabledBoundary`
  - meaning: explicit disabled state boundary for risky capability.
  - allowed usage: apply/write-path disabled references.
  - not meaning: temporary warning that can be ignored.
- `Reference`
  - meaning: supporting reference artifact used by canonical docs/tests.
  - allowed usage: index entries, schemas, and cross-linked references.
  - not meaning: superseded/obsolete by default.
- `Template`
  - meaning: reusable manual/governance template artifact.
  - allowed usage: change request/checklist/log templates.
  - not meaning: actual executed run evidence.
- `NeedsCleanup`
  - meaning: known cleanup/normalization needed.
  - allowed usage: backlog and transitional documentation signals.
  - not meaning: invalid artifact.
- `Superseded`
  - meaning: artifact is replaced by a newer canonical artifact.
  - allowed usage: legacy references that must remain traceable.
  - not meaning: deleted history.

## Normalization notes

- `ProposalOnly`, `ManualDecisionOnly`, and `ArchitectureReviewOnly` are normalized to `GovernanceOnly`.
- `Active` in security governance index entries is normalized to stage-specific canonical statuses from this vocabulary.
