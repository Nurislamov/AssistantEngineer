using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowDurableRepositoryTests
{
    [Fact]
    public async Task SqliteRepositoriesPersistAndReadDeterministically()
    {
        await using var harness = await SqliteHarness.CreateAsync();
        var projectRepository = new EfEngineeringProjectRepository(harness.DbContext);
        var workflowRepository = new EfEngineeringWorkflowStateRepository(harness.DbContext);
        var scenarioRepository = new EfEngineeringCalculationScenarioRepository(harness.DbContext);
        var artifactRepository = new EfEngineeringCalculationArtifactRepository(harness.DbContext);
        var historyRepository = new EfEngineeringScenarioHistoryRepository(harness.DbContext);
        var timestamp = new DateTimeOffset(2026, 05, 10, 10, 0, 0, TimeSpan.Zero);

        var project = await projectRepository.UpsertAsync(
            new EngineeringProjectRecordDto(
                ProjectId: 40,
                ProjectName: "Durable project",
                Description: "SQLite foundation",
                CreatedAtUtc: timestamp,
                UpdatedAtUtc: timestamp,
                Status: EngineeringProjectRecordStatus.Active,
                MetadataJson: new Dictionary<string, string> { ["k"] = "v" }),
            CancellationToken.None);

        Assert.Equal(40, project.ProjectId);

        await workflowRepository.SaveAsync(
            new EngineeringWorkflowStateRecordDto(
                WorkflowStateId: "wf-40-0001",
                ProjectId: 40,
                BuildingId: 400,
                Version: 1,
                CurrentStep: "Project",
                WorkflowStateJson: "{\"projectId\":40}",
                ValidationDiagnosticsJson: "[]",
                CreatedAtUtc: timestamp,
                UpdatedAtUtc: timestamp),
            CancellationToken.None);

        await workflowRepository.SaveAsync(
            new EngineeringWorkflowStateRecordDto(
                WorkflowStateId: "wf-40-0002",
                ProjectId: 40,
                BuildingId: 400,
                Version: 2,
                CurrentStep: "Review",
                WorkflowStateJson: "{\"projectId\":40,\"currentStep\":\"Review\"}",
                ValidationDiagnosticsJson: "[]",
                CreatedAtUtc: timestamp.AddMinutes(1),
                UpdatedAtUtc: timestamp.AddMinutes(1)),
            CancellationToken.None);

        var latestState = await workflowRepository.GetLatestByProjectIdAsync(40, CancellationToken.None);
        Assert.NotNull(latestState);
        Assert.Equal(2, latestState.Version);

        await scenarioRepository.CreateAsync(
            new EngineeringCalculationScenarioRecordDto(
                ScenarioId: "scenario-40",
                ProjectId: 40,
                BuildingId: 400,
                ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
                Status: EngineeringCalculationExecutionStatus.Prepared,
                RequestJson: "{}",
                ResultSummaryJson: "{}",
                CreatedAtUtc: timestamp,
                StartedAtUtc: timestamp,
                CompletedAtUtc: null,
                DurationMilliseconds: null,
                DiagnosticsJson: "[]"),
            CancellationToken.None);

        var updatedScenario = await scenarioRepository.UpdateAsync(
            new EngineeringCalculationScenarioRecordDto(
                ScenarioId: "scenario-40",
                ProjectId: 40,
                BuildingId: 400,
                ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
                Status: EngineeringCalculationExecutionStatus.CompletedWithWarnings,
                RequestJson: "{}",
                ResultSummaryJson: "{\"status\":\"CompletedWithWarnings\"}",
                CreatedAtUtc: timestamp,
                StartedAtUtc: timestamp,
                CompletedAtUtc: timestamp.AddMinutes(2),
                DurationMilliseconds: 1200,
                DiagnosticsJson: "[]"),
            CancellationToken.None);

        Assert.Equal(EngineeringCalculationExecutionStatus.CompletedWithWarnings, updatedScenario.Status);

        await artifactRepository.SaveAsync(
            new EngineeringCalculationArtifactRecordDto(
                ArtifactId: "scenario-40:ScenarioResultJson",
                ScenarioId: "scenario-40",
                ArtifactKind: EngineeringCalculationArtifactKind.ScenarioResultJson,
                ContentType: "application/json",
                Content: "{\"scenarioId\":\"scenario-40\"}",
                CreatedAtUtc: timestamp.AddMinutes(2),
                SizeBytes: 26,
                ChecksumSha256: "x"),
            CancellationToken.None);

        await historyRepository.AppendAsync(
            new EngineeringScenarioHistoryEntryDto(
                EventId: "scenario-40:Completed:1",
                ScenarioId: "scenario-40",
                ProjectId: 40,
                EventKind: EngineeringScenarioHistoryEventKind.Completed,
                Message: "Completed.",
                DiagnosticsJson: "[]",
                CreatedAtUtc: timestamp.AddMinutes(2)),
            CancellationToken.None);

        var scenarios = await scenarioRepository.ListByProjectIdAsync(40, CancellationToken.None);
        Assert.Single(scenarios);
        Assert.Equal("scenario-40", scenarios[0].ScenarioId);

        var artifact = await artifactRepository.GetByScenarioAndKindAsync(
            "scenario-40",
            EngineeringCalculationArtifactKind.ScenarioResultJson,
            CancellationToken.None);
        Assert.NotNull(artifact);

        var history = await historyRepository.ListByScenarioIdAsync("scenario-40", CancellationToken.None);
        Assert.Single(history);
        Assert.Equal(EngineeringScenarioHistoryEventKind.Completed, history[0].EventKind);
    }

    [Fact]
    public async Task InMemoryAndSqliteRepositoriesExposeCompatibleListOrdering()
    {
        var now = new DateTimeOffset(2026, 05, 10, 12, 0, 0, TimeSpan.Zero);
        var expectedScenarioIds = new[] { "scenario-new", "scenario-old" };

        var memoryStore = new EngineeringWorkflowMemoryStore();
        var memoryRepo = new InMemoryEngineeringCalculationScenarioRepository(memoryStore);
        await SeedScenariosAsync(memoryRepo, now);
        var memoryScenarios = await memoryRepo.ListByProjectIdAsync(50, CancellationToken.None);

        await using var harness = await SqliteHarness.CreateAsync();
        var sqliteRepo = new EfEngineeringCalculationScenarioRepository(harness.DbContext);
        var sqliteProjectRepository = new EfEngineeringProjectRepository(harness.DbContext);
        await sqliteProjectRepository.UpsertAsync(
            new EngineeringProjectRecordDto(
                ProjectId: 50,
                ProjectName: "Parity project",
                Description: null,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                Status: EngineeringProjectRecordStatus.Active,
                MetadataJson: null),
            CancellationToken.None);
        await SeedScenariosAsync(sqliteRepo, now);
        var sqliteScenarios = await sqliteRepo.ListByProjectIdAsync(50, CancellationToken.None);

        Assert.Equal(expectedScenarioIds, memoryScenarios.Select(item => item.ScenarioId).ToArray());
        Assert.Equal(expectedScenarioIds, sqliteScenarios.Select(item => item.ScenarioId).ToArray());
    }

    private static async Task SeedScenariosAsync(
        IEngineeringCalculationScenarioRepository repository,
        DateTimeOffset now)
    {
        await repository.CreateAsync(
            new EngineeringCalculationScenarioRecordDto(
                ScenarioId: "scenario-old",
                ProjectId: 50,
                BuildingId: null,
                ScenarioKind: EngineeringCalculationScenarioKind.ValidationOnly,
                ExecutionMode: EngineeringCalculationExecutionMode.ValidateOnly,
                Status: EngineeringCalculationExecutionStatus.Prepared,
                RequestJson: "{}",
                ResultSummaryJson: "{}",
                CreatedAtUtc: now,
                StartedAtUtc: null,
                CompletedAtUtc: null,
                DurationMilliseconds: null,
                DiagnosticsJson: "[]"),
            CancellationToken.None);

        await repository.CreateAsync(
            new EngineeringCalculationScenarioRecordDto(
                ScenarioId: "scenario-new",
                ProjectId: 50,
                BuildingId: null,
                ScenarioKind: EngineeringCalculationScenarioKind.ValidationOnly,
                ExecutionMode: EngineeringCalculationExecutionMode.ValidateOnly,
                Status: EngineeringCalculationExecutionStatus.Prepared,
                RequestJson: "{}",
                ResultSummaryJson: "{}",
                CreatedAtUtc: now.AddMinutes(1),
                StartedAtUtc: null,
                CompletedAtUtc: null,
                DurationMilliseconds: null,
                DiagnosticsJson: "[]"),
            CancellationToken.None);
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
