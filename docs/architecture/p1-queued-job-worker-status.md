# P1 Queued Job Worker Status

## Status

P1 queued job honesty is now implemented as a real single-node background worker foundation.

## What changed

- `EngineeringCalculationJobService` no longer emits `CALCULATION_JOB_WORKER_NOT_ENABLED` for queued jobs.
- `Queued` jobs are persisted as queued work and can be executed through `ExecuteQueuedJobAsync`.
- `EngineeringCalculationJobWorker` is registered as an ASP.NET Core hosted service.
- The worker polls a bounded `ListQueuedAsync` repository method, then performs atomic claim (`TryClaimQueuedJobAsync`) before execution.
- Claimed jobs carry worker/lease metadata (`ClaimedByWorkerId`, `ClaimedAtUtc`, `LeaseExpiresAtUtc`) to avoid multi-instance double execution of the same queued record.
- Testing disables the hosted loop by configuration to keep integration tests deterministic.

## Configuration

```json
{
  "EngineeringCalculationJobs": {
    "Worker": {
      "Enabled": true,
      "PollIntervalSeconds": 5,
      "BatchSize": 3,
      "LeaseDurationSeconds": 300,
      "StaleRunningJobRecoveryEnabled": false
    }
  }
}
```

## Intentional limitations

This is a P1 single-node worker foundation, not a distributed queue.

Current limitations:

- no distributed stale-lease recovery scheduler;
- no retry backoff policy beyond the existing `RetryScheduled` status shape;
- running-job cancellation remains cooperative/future work.

The important correction is that queued mode is no longer a fake/stale mode. It now has a concrete execution path.
