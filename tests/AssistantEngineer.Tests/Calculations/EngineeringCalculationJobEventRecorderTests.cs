using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Jobs;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationJobEventRecorderTests
{
    [Fact]
    public async Task AppendAsync_PersistsDeterministicEventRecord()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var repository = new InMemoryEngineeringCalculationJobEventRepository(store);
        var recorder = new EngineeringCalculationJobEventRecorder(
            repository,
            new EngineeringCalculationJobPayloadCodec());

        var timestamp = DateTimeOffset.Parse("2026-05-11T00:00:00Z");
        var job = CreateJob();
        var diagnostics = new[]
        {
            new EngineeringWorkflowDiagnosticDto("warning", "WARN_A", "A", "Validation"),
            new EngineeringWorkflowDiagnosticDto("warning", "WARN_A", "A", "Validation")
        };

        await recorder.AppendAsync(
            job,
            EngineeringCalculationJobStatus.Queued,
            "Calculation job queued.",
            null,
            5,
            diagnostics,
            timestamp,
            CancellationToken.None);

        var events = await repository.ListByJobIdAsync(job.JobId, CancellationToken.None);
        Assert.Single(events);
        Assert.Equal($"{job.JobId}:{EngineeringCalculationJobStatus.Queued}:{timestamp.ToUnixTimeMilliseconds()}", events[0].EventId);
        Assert.Equal(5, events[0].ProgressPercent);
        Assert.Contains("WARN_A", events[0].DiagnosticsJson, StringComparison.Ordinal);
    }

    private static EngineeringCalculationJobRecordDto CreateJob()
    {
        var timestamp = DateTimeOffset.Parse("2026-05-11T00:00:00Z");
        return new EngineeringCalculationJobRecordDto(
            JobId: "job-event",
            ProjectId: 10,
            ScenarioId: "scenario-event",
            Status: EngineeringCalculationJobStatus.Created,
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
