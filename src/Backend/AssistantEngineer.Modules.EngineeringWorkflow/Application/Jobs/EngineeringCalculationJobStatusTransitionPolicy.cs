using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobStatusTransitionPolicy
{
    public EngineeringCalculationJobRecordDto MoveToQueued(
        EngineeringCalculationJobRecordDto job,
        DateTimeOffset timestamp)
    {
        return job with
        {
            Status = EngineeringCalculationJobStatus.Queued,
            ProgressPercent = 5,
            CurrentStep = "Queued",
            QueuedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };
    }

    public EngineeringCalculationJobRecordDto MoveToRunning(
        EngineeringCalculationJobRecordDto job,
        DateTimeOffset startedAtUtc)
    {
        return job with
        {
            Status = EngineeringCalculationJobStatus.Running,
            ProgressPercent = 25,
            CurrentStep = "Running",
            StartedAtUtc = startedAtUtc,
            UpdatedAtUtc = startedAtUtc
        };
    }

    public EngineeringCalculationJobRecordDto MoveToCompleted(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationJobStatus finalStatus,
        string resultSummaryJson,
        string diagnosticsJson,
        DateTimeOffset completedAtUtc,
        double durationMilliseconds)
    {
        return job with
        {
            Status = finalStatus,
            ResultSummaryJson = resultSummaryJson,
            DiagnosticsJson = diagnosticsJson,
            ProgressPercent = 100,
            CurrentStep = "Completed",
            CompletedAtUtc = completedAtUtc,
            UpdatedAtUtc = completedAtUtc,
            DurationMilliseconds = durationMilliseconds
        };
    }

    public EngineeringCalculationJobRecordDto MoveToFailedExecution(
        EngineeringCalculationJobRecordDto job,
        string diagnosticsJson,
        DateTimeOffset failedAtUtc,
        double durationMilliseconds)
    {
        return job with
        {
            Status = EngineeringCalculationJobStatus.FailedExecution,
            DiagnosticsJson = diagnosticsJson,
            ProgressPercent = 100,
            CurrentStep = "Failed",
            CompletedAtUtc = failedAtUtc,
            UpdatedAtUtc = failedAtUtc,
            DurationMilliseconds = durationMilliseconds
        };
    }

    public EngineeringCalculationJobRecordDto MoveToCancelled(
        EngineeringCalculationJobRecordDto job,
        DateTimeOffset cancelledAtUtc)
    {
        return job with
        {
            Status = EngineeringCalculationJobStatus.Cancelled,
            ProgressPercent = Math.Max(job.ProgressPercent, 100),
            CurrentStep = "Cancelled",
            CompletedAtUtc = cancelledAtUtc,
            UpdatedAtUtc = cancelledAtUtc,
            CancellationRequested = true
        };
    }

    public EngineeringCalculationJobRecordDto MoveToCancelRequested(
        EngineeringCalculationJobRecordDto job,
        DateTimeOffset timestamp)
    {
        return job with
        {
            Status = EngineeringCalculationJobStatus.CancelRequested,
            CurrentStep = "CancelRequested",
            UpdatedAtUtc = timestamp,
            CancellationRequested = true
        };
    }

    public bool IsReadyForCancel(EngineeringCalculationJobStatus status) =>
        status is EngineeringCalculationJobStatus.Created
            or EngineeringCalculationJobStatus.Queued
            or EngineeringCalculationJobStatus.RetryScheduled;

    public bool IsRunning(EngineeringCalculationJobStatus status) =>
        status == EngineeringCalculationJobStatus.Running;

    public bool IsClaimedByWorker(EngineeringCalculationJobRecordDto job, string workerId) =>
        string.Equals(job.ClaimedByWorkerId, workerId, StringComparison.Ordinal);

    public EngineeringCalculationJobStatus MapScenarioStatus(EngineeringCalculationExecutionStatus status)
    {
        return status switch
        {
            EngineeringCalculationExecutionStatus.Completed => EngineeringCalculationJobStatus.Completed,
            EngineeringCalculationExecutionStatus.CompletedWithWarnings => EngineeringCalculationJobStatus.CompletedWithWarnings,
            EngineeringCalculationExecutionStatus.FailedValidation => EngineeringCalculationJobStatus.FailedValidation,
            EngineeringCalculationExecutionStatus.FailedExecution => EngineeringCalculationJobStatus.FailedExecution,
            EngineeringCalculationExecutionStatus.PartiallyExecuted => EngineeringCalculationJobStatus.CompletedWithWarnings,
            EngineeringCalculationExecutionStatus.Prepared => EngineeringCalculationJobStatus.CompletedWithWarnings,
            _ => EngineeringCalculationJobStatus.NotSupported
        };
    }
}
