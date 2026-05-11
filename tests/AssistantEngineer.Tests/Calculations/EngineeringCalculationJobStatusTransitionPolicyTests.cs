using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationJobStatusTransitionPolicyTests
{
    [Fact]
    public void MoveToQueued_AppliesExpectedLifecycleFields()
    {
        var policy = new EngineeringCalculationJobStatusTransitionPolicy();
        var timestamp = DateTimeOffset.Parse("2026-05-11T00:00:00Z");
        var job = CreateJob(EngineeringCalculationJobStatus.Created);

        var queued = policy.MoveToQueued(job, timestamp);

        Assert.Equal(EngineeringCalculationJobStatus.Queued, queued.Status);
        Assert.Equal("Queued", queued.CurrentStep);
        Assert.Equal(5, queued.ProgressPercent);
        Assert.Equal(timestamp, queued.QueuedAtUtc);
        Assert.Equal(timestamp, queued.UpdatedAtUtc);
    }

    [Fact]
    public void MoveToRunning_AppliesExpectedLifecycleFields()
    {
        var policy = new EngineeringCalculationJobStatusTransitionPolicy();
        var startedAt = DateTimeOffset.Parse("2026-05-11T00:01:00Z");
        var job = CreateJob(EngineeringCalculationJobStatus.Queued);

        var running = policy.MoveToRunning(job, startedAt);

        Assert.Equal(EngineeringCalculationJobStatus.Running, running.Status);
        Assert.Equal("Running", running.CurrentStep);
        Assert.Equal(25, running.ProgressPercent);
        Assert.Equal(startedAt, running.StartedAtUtc);
        Assert.Equal(startedAt, running.UpdatedAtUtc);
    }

    [Fact]
    public void MapScenarioStatus_PreservesExpectedJobStatusMapping()
    {
        var policy = new EngineeringCalculationJobStatusTransitionPolicy();

        Assert.Equal(EngineeringCalculationJobStatus.Completed, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.Completed));
        Assert.Equal(EngineeringCalculationJobStatus.CompletedWithWarnings, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.CompletedWithWarnings));
        Assert.Equal(EngineeringCalculationJobStatus.FailedValidation, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.FailedValidation));
        Assert.Equal(EngineeringCalculationJobStatus.FailedExecution, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.FailedExecution));
        Assert.Equal(EngineeringCalculationJobStatus.CompletedWithWarnings, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.PartiallyExecuted));
        Assert.Equal(EngineeringCalculationJobStatus.CompletedWithWarnings, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.Prepared));
        Assert.Equal(EngineeringCalculationJobStatus.NotSupported, policy.MapScenarioStatus(EngineeringCalculationExecutionStatus.NotSupported));
    }

    private static EngineeringCalculationJobRecordDto CreateJob(EngineeringCalculationJobStatus status)
    {
        var timestamp = DateTimeOffset.Parse("2026-05-11T00:00:00Z");
        return new EngineeringCalculationJobRecordDto(
            JobId: "job-policy",
            ProjectId: 1,
            ScenarioId: "scenario-policy",
            Status: status,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Queued,
            RequestJson: "{}",
            ResultSummaryJson: null,
            DiagnosticsJson: null,
            ProgressPercent: 0,
            CurrentStep: "Created",
            CreatedAtUtc: timestamp,
            QueuedAtUtc: null,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: timestamp,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);
    }
}
