using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationJobWorkerTests
{
    [Fact]
    public async Task WorkerProcessesQueuedJobsThroughJobService()
    {
        var services = new ServiceCollection();
        var store = new EngineeringWorkflowMemoryStore();
        var jobRepository = new InMemoryEngineeringCalculationJobRepository(store);
        await jobRepository.CreateAsync(CreateQueuedJob("job-worker-test", "scenario-worker-test"), CancellationToken.None);

        services.AddSingleton<IEngineeringCalculationJobRepository>(jobRepository);
        services.AddSingleton<IEngineeringCalculationJobService>(new JobServiceStub());

        await using var provider = services.BuildServiceProvider();
        var worker = new EngineeringCalculationJobWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EngineeringCalculationJobWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 1,
                BatchSize = 5
            }),
            NullLogger<EngineeringCalculationJobWorker>.Instance);

        var processed = await worker.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var service = Assert.IsType<JobServiceStub>(provider.GetRequiredService<IEngineeringCalculationJobService>());
        Assert.Equal(new[] { "job-worker-test" }, service.ExecutedJobIds);
    }

    private static EngineeringCalculationJobRecordDto CreateQueuedJob(string jobId, string scenarioId)
    {
        var timestamp = DateTimeOffset.Parse("2026-05-10T00:00:00Z");
        return new EngineeringCalculationJobRecordDto(
            JobId: jobId,
            ProjectId: 10,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Queued,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Queued,
            RequestJson: "{}",
            ResultSummaryJson: null,
            DiagnosticsJson: null,
            ProgressPercent: 5,
            CurrentStep: "Queued",
            CreatedAtUtc: timestamp,
            QueuedAtUtc: timestamp,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: timestamp,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);
    }

    private sealed class JobServiceStub : IEngineeringCalculationJobService
    {
        public List<string> ExecutedJobIds { get; } = [];

        public Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
            EngineeringCalculationJobRequestDto request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(
            string jobId,
            CancellationToken cancellationToken)
        {
            ExecutedJobIds.Add(jobId);
            return Task.FromResult<EngineeringCalculationJobResultDto?>(new EngineeringCalculationJobResultDto(
                JobId: jobId,
                ProjectId: 10,
                ScenarioId: "scenario-worker-test",
                Status: EngineeringCalculationJobStatus.Completed,
                ProgressPercent: 100,
                CurrentStep: "Completed",
                QueuedAtUtc: DateTimeOffset.Parse("2026-05-10T00:00:00Z"),
                StartedAtUtc: DateTimeOffset.Parse("2026-05-10T00:00:01Z"),
                CompletedAtUtc: DateTimeOffset.Parse("2026-05-10T00:00:02Z"),
                DurationMilliseconds: 1000,
                ScenarioResultSummary: null,
                Diagnostics: [],
                Assumptions: [],
                Warnings: [],
                PersistedArtifactReferences: [],
                HistoryEvents: [],
                Metadata: new Dictionary<string, string>()));
        }

        public Task<EngineeringCalculationJobResultDto?> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult<EngineeringCalculationJobResultDto?>(null);

        public Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(int projectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EngineeringCalculationJobResultDto>>([]);

        public Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventDto>>([]);

        public Task<EngineeringCalculationJobResultDto?> CancelJobAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult<EngineeringCalculationJobResultDto?>(null);
    }
}