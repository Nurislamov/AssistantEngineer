# P2 Hardening Status

## Scope

This document records the P2 production-hardening baseline for AssistantEngineer API/frontend workflow foundations.

It is an internal engineering implementation status note. It is not a compliance certificate and does not claim external validation parity.

## P2-01

- API hardening baseline:
- rate limiting middleware with heavy endpoint policy.
- explicit CORS policy with default deny-by-default origin list.
- anonymous operational health endpoints:
- `GET /health`
- `GET /ready`
- hardening docs and architecture guards.

## P2-02

- Workflow persistence payload limits:
- JSON/artifact size gates for request/state/result/diagnostics/artifacts.
- deterministic truncation marker policy.
- warning diagnostics when truncation occurs.
- persistence guard tests and documentation.

## P2-03

- Frontend test baseline:
- Vitest + React Testing Library foundation.
- deterministic workflow client and diagnostics panel tests.
- frontend keeps orchestration and DTO handling only; no calculation physics in browser.

## P2-04

- Final hardening sweep:
- pagination baseline for workflow list endpoints (`jobs`, `scenarios`) with default `page=1`, `pageSize=50`, max `pageSize=200`.
- idempotency key foundation for heavy calculation submissions:
- `run-calculation`
- `jobs`
- conflict handling (`409`) for same key + different payload fingerprint.
- structured logging baseline for scenario/job/idempotency lifecycle events with safe context fields.

## Remaining Work

- durable/distributed idempotency store for multi-node deployment.
- object/blob storage for large report/trace artifacts.
- tenant/user authorization isolation beyond API key boundary.
- OpenAPI contract generation and governance automation.
- frontend browser E2E coverage on top of current unit/component baseline.
- deeper repository-level batching for workflow input snapshot and large list traversals.
- deployment-grade structured logging provider/enrichment strategy.
- expanded external validation suites.

## Known Limitations

- current idempotency baseline is local in-memory (not distributed, not restart-durable).
- persistence and job lifecycle foundations do not prove engineering correctness.
- workflow/report/trace outputs summarize current internal engineering calculations only.
- no full standard compliance claim.
- not a legal compliance certificate.
- no external validation evidence.
