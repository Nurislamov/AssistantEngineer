using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringCalculationJobRepositoryTests
{
    [Fact]
    public async Task InMemoryAndSqliteJobRepositoriesPersistAndOrderDeterministically()
    {
        var now = new DateTimeOffset(2026, 05, 10, 14, 0, 0, TimeSpan.Zero);
        var jobA = CreateJob("job-a", "scenario-a", now);
        var jobB = CreateJob("job-b", "scenario-b", now.AddMinutes(1));
        var eventA = CreateEvent("event-a", "job-a", "scenario-a", now, EngineeringCalculationJobStatus.Queued, 5);
        var eventB = CreateEvent("event-b", "job-a", "scenario-a", now.AddSeconds(1), EngineeringCalculationJobStatus.Running, 25);

        var memoryStore = new EngineeringWorkflowMemoryStore();
        var memoryJobs = new InMemoryEngineeringCalculationJobRepository(memoryStore);
        var memoryEvents = new InMemoryEngineeringCalculationJobEventRepository(memoryStore);

        await memoryJobs.CreateAsync(jobA, CancellationToken.None);
        await memoryJobs.CreateAsync(jobB, CancellationToken.None);
        await memoryEvents.AppendAsync(eventA, CancellationToken.None);
        await memoryEvents.AppendAsync(eventB, CancellationToken.None);

        var memoryList = await memoryJobs.ListByProjectIdAsync(42, CancellationToken.None);
        var memoryEventList = await memoryEvents.ListByJobIdAsync("job-a", CancellationToken.None);

        await using var harness = await SqliteHarness.CreateAsync();
        var sqliteProjects = new EfEngineeringProjectRepository(harness.DbContext);
        await sqliteProjects.UpsertAsync(
            new EngineeringProjectRecordDto(
                ProjectId: 42,
                ProjectName: "Project 42",
                Description: null,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                Status: EngineeringProjectRecordStatus.Active,
                MetadataJson: null),
            CancellationToken.None);
        await harness.DbContext.Scenarios.AddAsync(new EngineeringCalculationScenarioEntity
        {
            Id = "scenario-a",
            ProjectId = 42,
            ScenarioKind = EngineeringCalculationScenarioKind.FullEngineeringCore.ToString(),
            ExecutionMode = EngineeringCalculationExecutionMode.ExecuteAvailableModules.ToString(),
            Status = EngineeringCalculationExecutionStatus.Prepared.ToString(),
            RequestJson = "{}",
            CreatedAtUtc = now
        });
        await harness.DbContext.Scenarios.AddAsync(new EngineeringCalculationScenarioEntity
        {
            Id = "scenario-b",
            ProjectId = 42,
            ScenarioKind = EngineeringCalculationScenarioKind.FullEngineeringCore.ToString(),
            ExecutionMode = EngineeringCalculationExecutionMode.ExecuteAvailableModules.ToString(),
            Status = EngineeringCalculationExecutionStatus.Prepared.ToString(),
            RequestJson = "{}",
            CreatedAtUtc = now
        });
        await harness.DbContext.SaveChangesAsync();

        var sqliteJobs = new EfEngineeringCalculationJobRepository(harness.DbContext);
        var sqliteEvents = new EfEngineeringCalculationJobEventRepository(harness.DbContext);

        await sqliteJobs.CreateAsync(jobA, CancellationToken.None);
        await sqliteJobs.CreateAsync(jobB, CancellationToken.None);
        await sqliteEvents.AppendAsync(eventA, CancellationToken.None);
        await sqliteEvents.AppendAsync(eventB, CancellationToken.None);

        var sqliteList = await sqliteJobs.ListByProjectIdAsync(42, CancellationToken.None);
        var sqliteEventList = await sqliteEvents.ListByJobIdAsync("job-a", CancellationToken.None);

        Assert.Equal(["job-b", "job-a"], memoryList.Select(item => item.JobId).ToArray());
        Assert.Equal(["job-b", "job-a"], sqliteList.Select(item => item.JobId).ToArray());
        Assert.Equal(["event-a", "event-b"], memoryEventList.Select(item => item.EventId).ToArray());
        Assert.Equal(["event-a", "event-b"], sqliteEventList.Select(item => item.EventId).ToArray());
    }

    private static EngineeringCalculationJobRecordDto CreateJob(string jobId, string scenarioId, DateTimeOffset createdAtUtc)
    {
        return new EngineeringCalculationJobRecordDto(
            JobId: jobId,
            ProjectId: 42,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Queued,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Queued,
            RequestJson: "{}",
            ResultSummaryJson: null,
            DiagnosticsJson: "[]",
            ProgressPercent: 5,
            CurrentStep: "Queued",
            CreatedAtUtc: createdAtUtc,
            QueuedAtUtc: createdAtUtc,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: createdAtUtc,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);
    }

    private static EngineeringCalculationJobEventRecordDto CreateEvent(
        string eventId,
        string jobId,
        string scenarioId,
        DateTimeOffset createdAtUtc,
        EngineeringCalculationJobStatus status,
        int progress)
    {
        return new EngineeringCalculationJobEventRecordDto(
            EventId: eventId,
            JobId: jobId,
            ScenarioId: scenarioId,
            ProjectId: 42,
            Status: status,
            EventKind: status.ToString(),
            Message: status.ToString(),
            DiagnosticsJson: "[]",
            ProgressPercent: progress,
            CreatedAtUtc: createdAtUtc);
    }

    private sealed class SqliteHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteHarness(SqliteConnection connection, EngineeringWorkflowPersistenceDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public EngineeringWorkflowPersistenceDbContext DbContext { get; }

        public static async Task<SqliteHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<EngineeringWorkflowPersistenceDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new EngineeringWorkflowPersistenceDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new SqliteHarness(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task JobRepositoryListsQueuedJobsInFifoOrder()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var repository = new InMemoryEngineeringCalculationJobRepository(store);
        var older = CreateJob("job-older", "scenario-older", DateTimeOffset.Parse("2026-05-10T00:00:00Z"));
        var newer = CreateJob("job-newer", "scenario-newer", DateTimeOffset.Parse("2026-05-10T00:01:00Z"));
        var running = CreateJob("job-running", "scenario-running", DateTimeOffset.Parse("2026-05-10T00:02:00Z")) with
        {
            Status = EngineeringCalculationJobStatus.Running,
            CurrentStep = "Running"
        };

        await repository.CreateAsync(newer, CancellationToken.None);
        await repository.CreateAsync(running, CancellationToken.None);
        await repository.CreateAsync(older, CancellationToken.None);

        var queued = await repository.ListQueuedAsync(10, CancellationToken.None);

        Assert.Equal(new[] { "job-older", "job-newer" }, queued.Select(item => item.JobId).ToArray());
    }

    [Fact]
    public async Task InMemoryRepositoryClaimsQueuedJobExactlyOnceAndWritesLeaseMetadata()
    {
        var repository = new InMemoryEngineeringCalculationJobRepository(new EngineeringWorkflowMemoryStore());
        var queued = CreateJob("job-claim-memory", "scenario-claim-memory", DateTimeOffset.Parse("2026-05-10T00:00:00Z"));
        await repository.CreateAsync(queued, CancellationToken.None);

        var firstClaim = await repository.TryClaimQueuedJobAsync(
            "job-claim-memory",
            "worker-a",
            TimeSpan.FromSeconds(120),
            CancellationToken.None);
        var secondClaim = await repository.TryClaimQueuedJobAsync(
            "job-claim-memory",
            "worker-b",
            TimeSpan.FromSeconds(120),
            CancellationToken.None);

        Assert.NotNull(firstClaim);
        Assert.Equal(EngineeringCalculationJobStatus.Running, firstClaim!.Status);
        Assert.Equal("worker-a", firstClaim.ClaimedByWorkerId);
        Assert.NotNull(firstClaim.ClaimedAtUtc);
        Assert.NotNull(firstClaim.LeaseExpiresAtUtc);
        Assert.True(firstClaim.LeaseExpiresAtUtc > firstClaim.ClaimedAtUtc);
        Assert.Null(secondClaim);
    }

    [Fact]
    public async Task InMemoryRepositoryDoesNotClaimNonQueuedJobs()
    {
        var repository = new InMemoryEngineeringCalculationJobRepository(new EngineeringWorkflowMemoryStore());
        var completed = CreateJob("job-complete-memory", "scenario-complete-memory", DateTimeOffset.Parse("2026-05-10T00:00:00Z")) with
        {
            Status = EngineeringCalculationJobStatus.Completed,
            CurrentStep = "Completed",
            ProgressPercent = 100
        };
        await repository.CreateAsync(completed, CancellationToken.None);

        var claim = await repository.TryClaimQueuedJobAsync(
            completed.JobId,
            "worker-a",
            TimeSpan.FromSeconds(120),
            CancellationToken.None);

        Assert.Null(claim);
    }

    [Fact]
    public async Task SqliteRepositoryClaimsQueuedJobExactlyOnceAndProtectsAgainstDoubleClaim()
    {
        var now = DateTimeOffset.Parse("2026-05-10T00:00:00Z");
        await using var harness = await SqliteHarness.CreateAsync();
        await SeedProjectAndScenarioAsync(harness.DbContext, now, "scenario-claim-sqlite");
        var repository = new EfEngineeringCalculationJobRepository(harness.DbContext);
        await repository.CreateAsync(CreateJob("job-claim-sqlite", "scenario-claim-sqlite", now), CancellationToken.None);

        var firstClaim = await repository.TryClaimQueuedJobAsync(
            "job-claim-sqlite",
            "worker-a",
            TimeSpan.FromSeconds(180),
            CancellationToken.None);
        var secondClaim = await repository.TryClaimQueuedJobAsync(
            "job-claim-sqlite",
            "worker-b",
            TimeSpan.FromSeconds(180),
            CancellationToken.None);

        Assert.NotNull(firstClaim);
        Assert.Equal(EngineeringCalculationJobStatus.Running, firstClaim!.Status);
        Assert.Equal("worker-a", firstClaim.ClaimedByWorkerId);
        Assert.NotNull(firstClaim.ClaimedAtUtc);
        Assert.NotNull(firstClaim.LeaseExpiresAtUtc);
        Assert.True(firstClaim.LeaseExpiresAtUtc > firstClaim.ClaimedAtUtc);
        Assert.Null(secondClaim);
    }

    [Fact]
    public async Task SqliteRepositoryDoesNotClaimNonQueuedJobs()
    {
        var now = DateTimeOffset.Parse("2026-05-10T00:00:00Z");
        await using var harness = await SqliteHarness.CreateAsync();
        await SeedProjectAndScenarioAsync(harness.DbContext, now, "scenario-running-sqlite");
        var repository = new EfEngineeringCalculationJobRepository(harness.DbContext);
        var running = CreateJob("job-running-sqlite", "scenario-running-sqlite", now) with
        {
            Status = EngineeringCalculationJobStatus.Running,
            CurrentStep = "Running",
            ProgressPercent = 25
        };
        await repository.CreateAsync(running, CancellationToken.None);

        var claim = await repository.TryClaimQueuedJobAsync(
            "job-running-sqlite",
            "worker-a",
            TimeSpan.FromSeconds(180),
            CancellationToken.None);

        Assert.Null(claim);
    }

    [Fact]
    public async Task InMemoryRepositoryConcurrentClaimAllowsExactlyOneWinner()
    {
        var repository = new InMemoryEngineeringCalculationJobRepository(new EngineeringWorkflowMemoryStore());
        var queued = CreateJob("job-claim-concurrent", "scenario-claim-concurrent", DateTimeOffset.Parse("2026-05-10T00:00:00Z"));
        await repository.CreateAsync(queued, CancellationToken.None);

        var first = repository.TryClaimQueuedJobAsync(
            queued.JobId,
            "worker-concurrent-a",
            TimeSpan.FromSeconds(120),
            CancellationToken.None);
        var second = repository.TryClaimQueuedJobAsync(
            queued.JobId,
            "worker-concurrent-b",
            TimeSpan.FromSeconds(120),
            CancellationToken.None);

        var claims = await Task.WhenAll(first, second);
        var successfulClaims = claims.Where(claim => claim is not null).ToArray();

        Assert.Single(successfulClaims);
        var winner = successfulClaims[0]!;
        Assert.Equal(EngineeringCalculationJobStatus.Running, winner.Status);
        Assert.Contains(winner.ClaimedByWorkerId, new[] { "worker-concurrent-a", "worker-concurrent-b" });

        var persisted = await repository.GetByIdAsync(queued.JobId, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(EngineeringCalculationJobStatus.Running, persisted!.Status);
        Assert.Equal(winner.ClaimedByWorkerId, persisted.ClaimedByWorkerId);
        Assert.Equal(winner.ClaimedAtUtc, persisted.ClaimedAtUtc);
        Assert.Equal(winner.LeaseExpiresAtUtc, persisted.LeaseExpiresAtUtc);
    }

    [Fact]
    public async Task InMemoryJobEventRepositoryConcurrentAppendPreservesAllEventsAndDeterministicOrdering()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var repository = new InMemoryEngineeringCalculationJobEventRepository(store);
        var createdAtUtc = DateTimeOffset.Parse("2026-05-10T00:00:00Z");

        var writes = Enumerable.Range(0, 64)
            .Select(index => repository.AppendAsync(
                CreateEvent(
                    $"event-concurrent-{index:D3}",
                    "job-concurrent",
                    "scenario-concurrent",
                    createdAtUtc.AddSeconds(index % 4),
                    EngineeringCalculationJobStatus.Running,
                    progress: index),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(writes);

        var listed = await repository.ListByJobIdAsync("job-concurrent", CancellationToken.None);
        Assert.Equal(64, listed.Count);
        Assert.Equal(
            listed
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.EventId, StringComparer.Ordinal)
                .Select(item => item.EventId)
                .ToArray(),
            listed.Select(item => item.EventId).ToArray());
    }

    private static async Task SeedProjectAndScenarioAsync(
        EngineeringWorkflowPersistenceDbContext context,
        DateTimeOffset createdAtUtc,
        string scenarioId)
    {
        var projects = new EfEngineeringProjectRepository(context);
        await projects.UpsertAsync(
            new EngineeringProjectRecordDto(
                ProjectId: 42,
                ProjectName: "Project 42",
                Description: null,
                CreatedAtUtc: createdAtUtc,
                UpdatedAtUtc: createdAtUtc,
                Status: EngineeringProjectRecordStatus.Active,
                MetadataJson: null),
            CancellationToken.None);

        await context.Scenarios.AddAsync(new EngineeringCalculationScenarioEntity
        {
            Id = scenarioId,
            ProjectId = 42,
            ScenarioKind = EngineeringCalculationScenarioKind.FullEngineeringCore.ToString(),
            ExecutionMode = EngineeringCalculationExecutionMode.ExecuteAvailableModules.ToString(),
            Status = EngineeringCalculationExecutionStatus.Prepared.ToString(),
            RequestJson = "{}",
            CreatedAtUtc = createdAtUtc
        });
        await context.SaveChangesAsync();
    }
}
