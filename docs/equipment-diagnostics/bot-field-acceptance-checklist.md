# Equipment Diagnostics Bot Field Acceptance Checklist

Passing this checklist contributes to ED-20A closed-beta readiness, but does not establish production readiness or full vendor manual coverage.
Before inviting Telegram beta operators, also complete `telegram-closed-beta-smoke-matrix.md`.

## ED-16B Purpose

ED-16B defines deterministic field scenarios before any Telegram adapter work. The pack verifies the same operator request across the application facade, HTTP endpoint, and internal frontend panel without adding diagnostic knowledge or changing runtime behavior.

## Scenario Expectations

| Scenario | Current expected behavior |
| --- | --- |
| Gree GMV H5 | `Answer`, seed verification/provenance/safety visible |
| Gree C5 | `Answer` for the current single Indoor runtime context |
| Gree GMV F5 | `NotFound`; non-runtime review sources remain excluded |
| Gree A0 / n6 / db | `ReferenceOnly`, never a final diagnostic answer |
| Gree ZZ99 | `NotFound` with equipment/display/manual verification fallback |
| Gree E1 | `ClarificationRequired` with deterministic context options |

The canonical definitions are under `docs/equipment-diagnostics/bot-scenarios/`.

## Acceptance Layers

- Backend scenario tests load and validate every scenario, call the public module facade twice, and assert deterministic status, safety, provenance, content, and non-runtime boundaries.
- API integration tests run the core H5, C5, A0, and unknown scenarios through the existing POST endpoint and require HTTP 200 with the expected bot status.
- Frontend tests render equivalent field scenarios without a real backend and verify answer, reference-only, not-found, verification, and safety states.

## Local Scenario Smoke

Start the API separately, then run:

```powershell
.\scripts\equipment-diagnostics\run-bot-scenario-smoke.ps1 -BaseUrl http://localhost:5000
```

The script reads the source-controlled scenarios, sends them to the existing endpoint, prints per-scenario PASS/FAIL, and exits non-zero on mismatch.

## Ready For A Future Telegram Transport

- deterministic request and response contracts;
- runtime-only final-answer boundary;
- field scenarios accepted through facade, API, and frontend;
- clarification and reference-only behavior explicitly covered;
- safety, provenance, verification, and internal-artifact guards retained.
- ED-17A parser/formatter/adapter skeleton delegates only to the existing bot facade.

## Remaining Limitations

- no production Telegram transport, webhook, or long polling;
- no production auth/roles claim beyond the current API setup;
- no audit log or operator feedback loop;
- no database/admin review UI;
- limited manual-backed runtime coverage.
- webhook transport is disabled by default and production deployment still requires HTTPS and secret-store review.
- deny lists are reviewed and deny wins over allow;
- `EnableChatIdDiscovery=false` after initial chat ID setup;
