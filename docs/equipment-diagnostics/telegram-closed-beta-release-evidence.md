# Telegram Closed Beta Release Evidence

## Purpose

ED-22A provides one deterministic local runner for collecting a Telegram EquipmentDiagnostics closed-beta evidence pack before any separately reviewed activation. It validates repository/build/test evidence, existing beta readiness evidence, and the generated ED-21B-compatible goal-run report.

This is closed beta evidence only. It is not Telegram activation, a real deployment, production or public release, a network check, or a substitute for human review.

## Command

Run the focused local path:

```powershell
.\scripts\equipment-diagnostics\prepare-telegram-closed-beta-release-evidence.ps1 `
  -BaseRef origin/master `
  -SkipFrontend
```

Explicit skip switches are recorded as warnings. The default runner executes restore, build, focused Goal Protocol and Telegram deterministic tests, frontend tests, existing ED-20A beta readiness generation, and ED-21B goal-run-report validation. It never runs Verify Engineering Core V1.

## Expected Outputs

Generated local evidence is written only under ignored `artifacts/verification/equipment-diagnostics/telegram-closed-beta/`:

- `release-evidence-summary.md`
- `release-evidence-report.json`
- `telegram-closed-beta-goal-run-report.json`

Generated artifacts are not committed. They are local review evidence, not runtime evidence.

## Manual Review Points

- Review `telegram-closed-beta-release-candidate.md`, `telegram-closed-beta-operator-limitation-card.md`, and `telegram-closed-beta-smoke-matrix.md`.
- Generate and review the ED-22C deployment activation dry-run before any separately approved activation.
- Review every blocker, warning, skipped command, branch, base reference, and head SHA.
- Confirm no generated artifact, raw log, PDF, manual file, chat ID, credential, or real domain is staged.
- Confirm Telegram transport and chat ID discovery remain disabled by default.
- Confirm runtime catalog remains the only final-answer source.
- Confirm manual-codebook, staging, and preview remain non-runtime review inputs and are not final diagnosis.
- Review ED-20A beta readiness warnings and validate the ED-21B goal-run report before any activation decision.

## Explicit Limitations

- Closed beta only; not production or public release.
- No real secrets in Git.
- Telegram is disabled by default.
- Chat ID discovery is disabled by default.
- No long polling.
- No database or audit persistence.
- No external monitoring.
- Runtime catalog is the only final-answer source.
- Manual-codebook, staging, and preview are not final diagnosis.
- Vendor manual coverage remains partial; no completeness claim is made.
- No runtime AI agent, RAG/vector search, or Telegram command execution is added by ED-22A.
