using AssistantEngineer.Api.Contracts.Calculations;
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
}
