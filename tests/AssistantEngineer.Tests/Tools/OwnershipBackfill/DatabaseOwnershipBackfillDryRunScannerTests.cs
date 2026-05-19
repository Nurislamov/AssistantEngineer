using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class DatabaseOwnershipBackfillDryRunScannerTests
{
    [Fact]
    public async Task EmptyDatabase_ProducesZeroMetrics()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            var scanner = new DatabaseOwnershipBackfillDryRunScanner();

            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);

            Assert.Equal(0, result.Summary.TotalRecordsScanned);
            Assert.Equal(0, result.Summary.TotalRecordsResolvable);
            Assert.Equal(0, result.Summary.TotalRecordsUnresolved);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Projects_WithAndWithoutOrganization_AreSplitBetweenResolvableAndUnresolved()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);

            await using (var appContext = CreateAppContext(connectionString))
            {
                await AddProjectAsync(appContext, "Scoped Project", organizationId: 42);
                await AddProjectAsync(appContext, "Legacy Project", organizationId: null);
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var projectMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Project");

            Assert.Equal(2, projectMetrics.TotalRecords);
            Assert.Equal(1, projectMetrics.ResolvableRecords);
            Assert.Equal(1, projectMetrics.UnresolvedRecords);
            Assert.Equal(0, projectMetrics.AmbiguousRecords);
            Assert.Equal(1, projectMetrics.UnresolvedByReason[OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing]);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Building_UnderScopedProject_IsResolvable()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);

            await using (var appContext = CreateAppContext(connectionString))
            {
                var projectId = await AddProjectAsync(appContext, "Scoped Project", organizationId: 11);
                await AddBuildingAsync(appContext, projectId, "Building A");
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var buildingMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Building");

            Assert.Equal(1, buildingMetrics.TotalRecords);
            Assert.Equal(1, buildingMetrics.ResolvableRecords);
            Assert.Equal(0, buildingMetrics.UnresolvedRecords);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Building_WithMissingProject_IsUnresolved()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);

            await using (var appContext = CreateAppContext(connectionString))
            {
                await appContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                await appContext.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"Buildings\" (\"Id\", \"Name\", \"ProjectId\", \"ClimateZoneId\") VALUES (999, 'Orphan Building', 555, NULL);");
                await appContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var buildingMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Building");

            Assert.Equal(1, buildingMetrics.UnresolvedRecords);
            Assert.Equal(1, buildingMetrics.UnresolvedByReason[OwnershipBackfillUnresolvedReasons.BuildingProjectMissing]);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task ScenarioAndJobOwnership_ResolveByProjectAndScenarioPath()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            int projectId;

            await using (var appContext = CreateAppContext(connectionString))
            {
                projectId = await AddProjectAsync(appContext, "Scoped Project", organizationId: 7);
            }

            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                await AddWorkflowProjectAsync(workflowContext, projectId);
                await AddScenarioAsync(workflowContext, "scenario-1", projectId, buildingId: null);
                await AddJobAsync(workflowContext, "job-1", projectId, "scenario-1");
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var scenarioMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Scenario");
            var jobMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Job");

            Assert.Equal(1, scenarioMetrics.ResolvableRecords);
            Assert.Equal(1, jobMetrics.ResolvableRecords);
            Assert.Equal(0, jobMetrics.UnresolvedRecords);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Job_WithMissingScenario_ResolvesThroughProjectFallback()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            int projectId;

            await using (var appContext = CreateAppContext(connectionString))
            {
                projectId = await AddProjectAsync(appContext, "Scoped Project", organizationId: 90);
            }

            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                await AddWorkflowProjectAsync(workflowContext, projectId);
                await workflowContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                await workflowContext.Database.ExecuteSqlRawAsync(@"
INSERT INTO ""engineering_workflow_jobs""
(""Id"", ""ProjectId"", ""ScenarioId"", ""Status"", ""ExecutionMode"", ""RequestJson"", ""ProgressPercent"", ""CurrentStep"", ""CreatedAtUtc"", ""UpdatedAtUtc"", ""RetryCount"", ""CancellationRequested"")
VALUES ('job-missing-scenario', {0}, 'missing-scenario', 'Queued', 'Manual', '{{}}', 0, 'Queued', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 0, 0);", projectId);
                await workflowContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var jobMetrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Job");

            Assert.Equal(1, jobMetrics.ResolvableRecords);
            Assert.Equal(0, jobMetrics.UnresolvedRecords);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task JobEvents_ResolveThroughJobOrScenario()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            int projectId;

            await using (var appContext = CreateAppContext(connectionString))
            {
                projectId = await AddProjectAsync(appContext, "Scoped Project", organizationId: 5);
            }

            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                await AddWorkflowProjectAsync(workflowContext, projectId);
                await AddScenarioAsync(workflowContext, "scenario-e", projectId, buildingId: null);
                await AddJobAsync(workflowContext, "job-e", projectId, "scenario-e");
                await AddJobEventAsync(workflowContext, "event-e", "job-e", "scenario-e", projectId);
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var metrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "JobEvent");

            Assert.Equal(1, metrics.ResolvableRecords);
            Assert.Equal(0, metrics.UnresolvedRecords);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Scenario_WithConflictingProjectAndBuildingOwnership_IsMarkedAmbiguous()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            int projectAId;
            int projectBId;
            int buildingId;

            await using (var appContext = CreateAppContext(connectionString))
            {
                projectAId = await AddProjectAsync(appContext, "Project A", organizationId: 1);
                projectBId = await AddProjectAsync(appContext, "Project B", organizationId: 2);
                buildingId = await AddBuildingAsync(appContext, projectBId, "B-2");
            }

            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                await AddWorkflowProjectAsync(workflowContext, projectAId);
                await AddScenarioAsync(workflowContext, "scenario-ambiguous", projectAId, buildingId);
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            var result = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);
            var metrics = result.Summary.RecordTypeMetrics.Single(metric => metric.RecordType == "Scenario");
            var unresolved = result.UnresolvedRecords.Single(record => record.RecordType == "Scenario");

            Assert.Equal(1, metrics.AmbiguousRecords);
            Assert.Equal(1, metrics.UnresolvedRecords);
            Assert.Equal(OwnershipBackfillUnresolvedReasons.ScenarioOwnershipAmbiguous, unresolved.Reason);
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task Scanner_DoesNotChangeRowCounts()
    {
        var connectionString = BuildSqliteConnectionString();

        try
        {
            await EnsureSchemasAsync(connectionString);
            int projectId;

            await using (var appContext = CreateAppContext(connectionString))
            {
                projectId = await AddProjectAsync(appContext, "Scoped Project", organizationId: 3);
            }

            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                await AddWorkflowProjectAsync(workflowContext, projectId);
                await AddScenarioAsync(workflowContext, "scenario-count", projectId, buildingId: null);
            }

            int projectsBefore;
            int scenariosBefore;
            await using (var appContext = CreateAppContext(connectionString))
            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                projectsBefore = await appContext.Projects.CountAsync();
                scenariosBefore = await workflowContext.Scenarios.CountAsync();
            }

            var scanner = new DatabaseOwnershipBackfillDryRunScanner();
            _ = await scanner.ScanAsync(CreateOptions(connectionString), CancellationToken.None);

            await using (var appContext = CreateAppContext(connectionString))
            await using (var workflowContext = CreateWorkflowContext(connectionString))
            {
                var projectsAfter = await appContext.Projects.CountAsync();
                var scenariosAfter = await workflowContext.Scenarios.CountAsync();

                Assert.Equal(projectsBefore, projectsAfter);
                Assert.Equal(scenariosBefore, scenariosAfter);
            }
        }
        finally
        {
            DeleteSqliteFile(connectionString);
        }
    }

    private static OwnershipBackfillOptions CreateOptions(string connectionString)
    {
        return new OwnershipBackfillOptions(
            BatchSize: 100,
            MaxUnresolvedRate: 0.05d,
            EvidenceOutputDirectory: "unused",
            ConnectionString: connectionString,
            DatabaseProvider: "SQLite",
            IncludeLegacyUnscoped: false,
            NoDataDryRun: false);
    }

    private static async Task EnsureSchemasAsync(string connectionString)
    {
        await using var appContext = CreateAppContext(connectionString);
        await appContext.Database.EnsureCreatedAsync();

        await using var workflowContext = CreateWorkflowContext(connectionString);
        await workflowContext.Database.MigrateAsync();
    }

    private static async Task<int> AddProjectAsync(AppDbContext context, string name, int? organizationId)
    {
        var project = Project.Create(name).Value;
        if (organizationId.HasValue)
            _ = project.AssignOrganization(organizationId.Value);

        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project.Id;
    }

    private static async Task<int> AddBuildingAsync(AppDbContext context, int projectId, string name)
    {
        var project = await context.Projects.SingleAsync(item => item.Id == projectId);
        var building = Building.Create(name, project).Value;
        _ = project.AddBuilding(building);

        await context.SaveChangesAsync();
        return building.Id;
    }

    private static async Task AddWorkflowProjectAsync(EngineeringWorkflowPersistenceDbContext context, int id)
    {
        context.Projects.Add(new EngineeringProjectEntity
        {
            Id = id,
            Name = $"Workflow Project {id}",
            Status = "Active",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static async Task AddScenarioAsync(
        EngineeringWorkflowPersistenceDbContext context,
        string scenarioId,
        int projectId,
        int? buildingId)
    {
        context.Scenarios.Add(new EngineeringCalculationScenarioEntity
        {
            Id = scenarioId,
            ProjectId = projectId,
            BuildingId = buildingId,
            ScenarioKind = "Baseline",
            ExecutionMode = "Manual",
            Status = "Created",
            RequestJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static async Task AddJobAsync(
        EngineeringWorkflowPersistenceDbContext context,
        string jobId,
        int projectId,
        string scenarioId)
    {
        context.Jobs.Add(new EngineeringCalculationJobEntity
        {
            Id = jobId,
            ProjectId = projectId,
            ScenarioId = scenarioId,
            Status = "Queued",
            ExecutionMode = "Manual",
            RequestJson = "{}",
            ProgressPercent = 0,
            CurrentStep = "Queued",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0,
            CancellationRequested = false
        });
        await context.SaveChangesAsync();
    }

    private static async Task AddJobEventAsync(
        EngineeringWorkflowPersistenceDbContext context,
        string eventId,
        string jobId,
        string scenarioId,
        int projectId)
    {
        context.JobEvents.Add(new EngineeringCalculationJobEventEntity
        {
            Id = eventId,
            JobId = jobId,
            ScenarioId = scenarioId,
            ProjectId = projectId,
            Status = "Queued",
            EventKind = "Queued",
            Message = "queued",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static AppDbContext CreateAppContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static EngineeringWorkflowPersistenceDbContext CreateWorkflowContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EngineeringWorkflowPersistenceDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new EngineeringWorkflowPersistenceDbContext(options);
    }

    private static string BuildSqliteConnectionString()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-ownership-backfill-{Guid.NewGuid():N}.db");
        return $"Data Source={path};Cache=Shared;Mode=ReadWriteCreate";
    }

    private static void DeleteSqliteFile(string connectionString)
    {
        var dataSourcePrefix = "Data Source=";
        if (!connectionString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
            return;

        var path = connectionString[dataSourcePrefix.Length..].Split(';', StringSplitOptions.RemoveEmptyEntries)[0];
        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // SQLite pooled handles can keep temp files locked briefly; tests should not fail on cleanup.
        }
    }
}

