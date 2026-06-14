# Operations

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
