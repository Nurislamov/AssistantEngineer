using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowPersistenceMigrationTests
{
    private const string InitialMigrationId = EngineeringWorkflowPersistenceDatabaseInitializer.InitialMigrationId;
    private const string ClaimLeaseMigrationId = "20260511000100_AddEngineeringJobClaimLeaseMetadata";
    private const string IdempotencyRecordsMigrationId = "20260511000200_AddEngineeringWorkflowIdempotencyRecords";

    [Fact]
    public void WorkflowPersistenceInitializerUsesMigrationsInsteadOfEnsureCreated()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Persistence",
            "Durable",
            "EngineeringWorkflowPersistenceDatabaseInitializer.cs");

        var content = File.ReadAllText(path);

        Assert.Contains(".Migrate()", content, StringComparison.Ordinal);
        Assert.DoesNotContain(".EnsureCreated()", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowPersistenceHasInitialMigrationCoveringDurableTables()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Persistence",
            "Durable",
            "Migrations",
            "20260510000100_InitialEngineeringWorkflowPersistence.cs");

        Assert.True(File.Exists(path), $"Initial workflow persistence migration is missing: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains($"[Migration(\"{InitialMigrationId}\")]", content, StringComparison.Ordinal);

        string[] requiredTables =
        [
            "engineering_workflow_projects",
            "engineering_workflow_states",
            "engineering_workflow_scenarios",
            "engineering_workflow_artifacts",
            "engineering_workflow_history_entries",
            "engineering_workflow_jobs",
            "engineering_workflow_job_events"
        ];

        foreach (var table in requiredTables)
        {
            Assert.Contains(table, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WorkflowPersistenceHasClaimLeaseMigrationForQueuedJobAtomicity()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Persistence",
            "Durable",
            "Migrations",
            "20260511000100_AddEngineeringJobClaimLeaseMetadata.cs");

        Assert.True(File.Exists(path), $"Claim/lease workflow persistence migration is missing: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains($"[Migration(\"{ClaimLeaseMigrationId}\")]", content, StringComparison.Ordinal);
        Assert.Contains("engineering_workflow_jobs", content, StringComparison.Ordinal);
        Assert.Contains("ClaimedByWorkerId", content, StringComparison.Ordinal);
        Assert.Contains("ClaimedAtUtc", content, StringComparison.Ordinal);
        Assert.Contains("LeaseExpiresAtUtc", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowPersistenceHasIdempotencyRecordsMigration()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Persistence",
            "Durable",
            "Migrations",
            "20260511000200_AddEngineeringWorkflowIdempotencyRecords.cs");

        Assert.True(File.Exists(path), $"Idempotency workflow persistence migration is missing: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains($"[Migration(\"{IdempotencyRecordsMigrationId}\")]", content, StringComparison.Ordinal);
        Assert.Contains("engineering_workflow_idempotency_records", content, StringComparison.Ordinal);
        Assert.Contains("Scope", content, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey", content, StringComparison.Ordinal);
        Assert.Contains("RequestFingerprint", content, StringComparison.Ordinal);
        Assert.Contains("IX_engineering_workflow_idempotency_records_Scope_IdempotencyKey", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SqliteProviderAppliesInitialMigrationOnStartup()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-workflow-migration-{Guid.NewGuid():N}.db");
        try
        {
            using var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:Provider"] = "SQLite",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:EnsureCreatedOnStartup"] = "true",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:SqliteConnectionString"] = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
            });

            using (var scope = provider.CreateScope())
            {
                _ = scope.ServiceProvider.GetRequiredService<IEngineeringWorkflowPersistenceService>();
            }

            var migrations = ReadMigrationHistory(dbPath);

            Assert.Contains(InitialMigrationId, migrations);
            Assert.Contains(ClaimLeaseMigrationId, migrations);
            Assert.Contains(IdempotencyRecordsMigrationId, migrations);
        }
        finally
        {
            TryDeleteSqliteFiles(dbPath);
        }
    }

    [Fact]
    public void SqliteProviderBaselinesExistingEnsureCreatedWorkflowDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-workflow-baseline-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate";
            var options = new DbContextOptionsBuilder<EngineeringWorkflowPersistenceDbContext>()
                .UseSqlite(connectionString)
                .Options;

            using (var dbContext = new EngineeringWorkflowPersistenceDbContext(options))
            {
                dbContext.Database.EnsureCreated();
            }

            using var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:Provider"] = "SQLite",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:EnsureCreatedOnStartup"] = "true",
                [$"{EngineeringWorkflowPersistenceOptions.SectionName}:SqliteConnectionString"] = connectionString
            });

            using (var scope = provider.CreateScope())
            {
                _ = scope.ServiceProvider.GetRequiredService<IEngineeringWorkflowPersistenceService>();
            }

            var migrations = ReadMigrationHistory(dbPath);

            Assert.Contains(InitialMigrationId, migrations);
            Assert.Contains(ClaimLeaseMigrationId, migrations);
            Assert.Contains(IdempotencyRecordsMigrationId, migrations);
        }
        finally
        {
            TryDeleteSqliteFiles(dbPath);
        }
    }

    private static IReadOnlyList<string> ReadMigrationHistory(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";

        using var reader = command.ExecuteReader();
        var migrations = new List<string>();
        while (reader.Read())
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations;
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApiPresentation();
        return services.BuildServiceProvider();
    }

    private static void TryDeleteSqliteFiles(string path)
    {
        TryDeleteFile(path);
        TryDeleteFile(path + "-shm");
        TryDeleteFile(path + "-wal");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // SQLite cleanup is best-effort in tests.
        }
    }
}
