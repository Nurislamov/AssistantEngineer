using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
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
                BatchSize = 5,
                LeaseDurationSeconds = 300,
                WorkerId = "worker-test"
            }),
            NullLogger<EngineeringCalculationJobWorker>.Instance);

        var processed = await worker.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var service = Assert.IsType<JobServiceStub>(provider.GetRequiredService<IEngineeringCalculationJobService>());
        Assert.Equal(new[] { "job-worker-test" }, service.ExecutedJobIds);
    }

    [Fact]
    public async Task WorkerSkipsJobsWhenClaimFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEngineeringCalculationJobRepository>(new ClaimFailingRepository());
        services.AddSingleton<IEngineeringCalculationJobService>(new JobServiceStub());

        await using var provider = services.BuildServiceProvider();
        var worker = new EngineeringCalculationJobWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EngineeringCalculationJobWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 1,
                BatchSize = 5,
                LeaseDurationSeconds = 300,
                WorkerId = "worker-test"
            }),
            NullLogger<EngineeringCalculationJobWorker>.Instance);

        var processed = await worker.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(0, processed);
        var service = Assert.IsType<JobServiceStub>(provider.GetRequiredService<IEngineeringCalculationJobService>());
        Assert.Empty(service.ExecutedJobIds);
    }

    [Fact]
    public async Task WorkerDoesNotExecuteSameQueuedJobTwiceAcrossBatches()
    {
        var services = new ServiceCollection();
        var store = new EngineeringWorkflowMemoryStore();
        var jobRepository = new InMemoryEngineeringCalculationJobRepository(store);
        await jobRepository.CreateAsync(CreateQueuedJob("job-once", "scenario-once"), CancellationToken.None);

        services.AddSingleton<IEngineeringCalculationJobRepository>(jobRepository);
        services.AddSingleton<IEngineeringCalculationJobService>(new JobServiceStub());

        await using var provider = services.BuildServiceProvider();
        var worker = new EngineeringCalculationJobWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EngineeringCalculationJobWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 1,
                BatchSize = 5,
                LeaseDurationSeconds = 300,
                WorkerId = "worker-test"
            }),
            NullLogger<EngineeringCalculationJobWorker>.Instance);

        var firstProcessed = await worker.ProcessBatchAsync(CancellationToken.None);
        var secondProcessed = await worker.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, firstProcessed);
        Assert.Equal(0, secondProcessed);

        var service = Assert.IsType<JobServiceStub>(provider.GetRequiredService<IEngineeringCalculationJobService>());
        Assert.Equal(new[] { "job-once" }, service.ExecutedJobIds);
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
            throw new NotSupportedException();
        }

        public Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(
            string jobId,
            string workerId,
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

    private sealed class ClaimFailingRepository : IEngineeringCalculationJobRepository
    {
        public Task<EngineeringCalculationJobRecordDto> CreateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationJobRecordDto> UpdateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(int maxCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>([CreateQueuedJob("job-claim-fail", "scenario-claim-fail")]);

        public Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(string jobId, string workerId, TimeSpan leaseDuration, CancellationToken cancellationToken) =>
            Task.FromResult<EngineeringCalculationJobRecordDto?>(null);

        public Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult<EngineeringCalculationJobRecordDto?>(null);

        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EngineeringCalculationJobRecordDto>>([]);
    }
}
