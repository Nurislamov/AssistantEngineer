# Operations

## Goal Protocol

Large engineering tasks and release-readiness work use the internal [AssistantEngineer Goal Protocol](../engineering-workflow/goal-protocol.md). It defines deterministic recon, roadmap, phase evidence, recovery, and final-audit practices without creating runtime automation.
Generated goal-run evidence can be checked with the [Goal Run Report Validator](../engineering-workflow/goal-run-report-validator.md).
Before a reviewed Telegram closed-beta activation, generate and manually review the [ED-22A release evidence pack](../equipment-diagnostics/telegram-closed-beta-release-evidence.md).
Then review the [ED-22B release candidate](../equipment-diagnostics/telegram-closed-beta-release-candidate.md), [operator limitation card](../equipment-diagnostics/telegram-closed-beta-operator-limitation-card.md), and [smoke matrix](../equipment-diagnostics/telegram-closed-beta-smoke-matrix.md).
Before any separately approved activation, generate and review the [ED-22C deployment activation dry-run](../equipment-diagnostics/telegram-closed-beta-deployment-dry-run.md).

For EquipmentDiagnostics ED-20A, operations evidence supports a controlled closed beta only. Sanitize logs before review; no production monitoring or audit persistence is claimed.

This section documents the current provider-neutral operational foundation:

- [runtime observability](runtime-observability.md);
- [operational diagnostics](operational-diagnostics.md);
- [request correlation and logging](request-correlation-and-logging.md);
- [incident runbooks](incidents/);
- sanitized log collection with `scripts/operations/collect-sanitized-logs.ps1`.

The current stack has no external monitoring, alerting, log sink, audit persistence, or provider-specific
operations integration. Generated incident artifacts belong under ignored `artifacts/operations/` and must never
be committed.

Sanitize an existing local file without Docker:

```powershell
.\scripts\operations\collect-sanitized-logs.ps1 `
  -RedactOnlyInputPath <local-input> `
  -OutputPath artifacts/operations/sanitized-logs/manual/sanitized-log.txt
```

The input file is never modified. Review sanitized output before sharing it.
