# Goal Run Report Validator

## Purpose

The Goal Run Report Validator checks the machine-readable evidence report for a large AssistantEngineer engineering task. It validates required structure, statuses, phase evidence, final-audit evidence, blockers, warnings, generated-artifact paths, and unsupported claims.

It is a deterministic verification/reporting layer. It has no runtime AI agent, no RAG/vector search, no Telegram command execution, and no production/public release claim. It does not execute product workflows, change runtime behavior, or decide a merge without human review.

## Expected JSON Structure

The committed minimal schema is `docs/engineering-workflow/goal-run-report.schema.json`. Required top-level fields are:

- `goalId`, `title`, `sourceBranch`, and `targetBranch`;
- `scope`, `outOfScope`, and `constraints`;
- `preflight.commands`;
- `phases`;
- `finalAudit`;
- `warnings`, `blockers`, and `generatedArtifacts`.

Statuses are `pass`, `fail`, or `not_run`. A failed preflight command, failed phase, failed final audit, explicit blocker, missing required evidence, unsafe artifact path, or forbidden claim produces a blocker. A `not_run` status produces a warning unless another rule makes it a blocker.

## Generated Artifacts Policy

Generated goal-run reports must live under ignored `artifacts/verification/` or `artifacts/planning/`. Generated artifacts are not committed and must not be placed under `docs/`, contain secret-like paths, or reference log dumps, PDFs, or manual files. Committed schemas and templates under `docs/engineering-workflow/` are maintained documentation, not generated runtime evidence.

## Command

```powershell
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- `
  goal-run-report `
  --input artifacts/verification/goal-run-report.json
```

The command prints `PASS` or `FAIL`, blocker count, and warning count. A missing file, invalid JSON, or validation blocker returns a non-zero exit code.

## Safety Boundary

The validator does not replace phase commands or final human review. It preserves the Goal Protocol statement that `scripts/engineering-core/verify-engineering-core-v1.ps1` is protected and unchanged by goal-protocol work.
