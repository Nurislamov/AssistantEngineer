# Engineering Calculation Jobs API

## Purpose

Engineering Calculation Jobs API adds persisted run lifecycle on top of scenario runner and workflow persistence foundations.

It coordinates job orchestration only and does not introduce engineering physics.

## Endpoints

- `POST /api/v1/engineering-workflow/jobs`
- `GET /api/v1/engineering-workflow/jobs/{jobId}`
- `GET /api/v1/engineering-workflow/jobs/{jobId}/events`
- `POST /api/v1/engineering-workflow/jobs/{jobId}/cancel`
- `GET /api/v1/engineering-workflow/{projectId}/jobs`

## Status lifecycle

- `Created -> Queued -> Running -> Completed`
- `Created -> Queued -> Running -> CompletedWithWarnings`
- `Created -> Queued -> Running -> FailedValidation`
- `Created -> Queued -> Running -> FailedExecution`
- `Created/Queued -> Cancelled`
- `Running -> CancelRequested` when immediate running cancellation is not supported in current foundation mode.

## Execution behavior

- `Synchronous`: runs scenario runner in request scope and persists lifecycle/events/results/artifacts.
- `Queued`: persists queued lifecycle state and executes through background worker when enabled.
- `DryRun`: maps to scenario `DryRun`.
- `ValidateOnly`: maps to scenario `ValidateOnly`.

## Atomic queue claim foundation (P3-01)

- Worker picks queued candidates via bounded read and then tries an atomic claim transition (`Queued/RetryScheduled -> Running`).
- Claim writes lease metadata (`ClaimedByWorkerId`, `ClaimedAtUtc`, `LeaseExpiresAtUtc`) and execution proceeds only for successfully claimed jobs.
- Competing worker instances that lose the claim skip the job without raising execution errors.
- The durable SQLite repository uses a conditional update and validates `rows affected == 1` to prevent double-start.

## Persistence behavior

- Job records and job events are persisted through Stage 12 provider model (`InMemory` / `SQLite`).
- Scenario records and artifacts are persisted through existing workflow persistence service.
- Job diagnostics and events are sorted/deduplicated deterministically.
- Job submission idempotency (`Idempotency-Key`) uses durable SQLite-backed store when workflow persistence provider is SQLite.

## Known limitations

- foundation job queue is not distributed.
- no production-grade background worker orchestration guarantees in this stage.
- no distributed stale-lease recovery scheduler in this stage.
- no multi-node coordination primitives beyond atomic queued-to-running claim.
- no advanced retry/backoff policy in this stage.
- no dead-letter queue in this stage.
- no full audit/security model in this stage.
- job lifecycle does not prove engineering correctness.
- not a compliance certificate.
- no external validation evidence.
- no full standard compliance claim.
