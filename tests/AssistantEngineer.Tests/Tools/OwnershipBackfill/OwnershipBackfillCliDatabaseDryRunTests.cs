using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliDatabaseDryRunTests
{
    [Fact]
    public async Task SqliteDryRun_WithTempDatabase_ExitsZeroAndWritesEvidence()
    {
        var connectionString = BuildSqliteConnectionString();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ae-backfill-cli-db-" + Guid.NewGuid().ToString("N"));

        try
        {
            await EnsureSchemasAsync(connectionString);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
                [
                    "dry-run",
                    "--output", outputDirectory,
                    "--database-provider", "SQLite",
                    "--connection-string", connectionString
                ],
                stdout,
                stderr,
                CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.True(Directory.GetFiles(outputDirectory, "ownership-backfill-dry-run-summary-*.json").Length > 0);
            Assert.True(Directory.GetFiles(outputDirectory, "ownership-backfill-unresolved-records-*.json").Length > 0);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);

            DeleteSqliteFile(connectionString);
        }
    }

    [Fact]
    public async Task MissingConnectionStringForSqlite_Fails()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["dry-run", "--output", "tmp", "--database-provider", "SQLite"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("--connection-string", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownProvider_Fails()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["dry-run", "--output", "tmp", "--database-provider", "MySql", "--connection-string", "x"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("--database-provider", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectionString_IsNotWrittenToOutputOrErrors()
    {
        const string fakeSecret = "Host=127.0.0.1;Password=TOP-SECRET-DB;";

        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["dry-run", "--output", "tmp", "--database-provider", "SQLite", "--connection-string", fakeSecret, "--unknown-arg"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        var allOutput = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, allOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_IsStillRejected()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["apply", "--output", "tmp"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static OwnershipBackfillCli CreateCli()
    {
        return new OwnershipBackfillCli(
            new OwnershipBackfillCommandLineParser(),
            new OwnershipBackfillEvidenceWriter(),
            new NoDataOwnershipBackfillDryRunScanner(),
            new DatabaseOwnershipBackfillDryRunScanner(),
            new OwnershipBackfillEvidenceLoader(),
            new OwnershipBackfillEvidenceGateEvaluator(),
            new OwnershipBackfillGateResultWriter(),
            new OwnershipBackfillApplyPreconditionValidator(),
            new OwnershipBackfillApplyPlanGenerator(),
            new OwnershipBackfillApplyPlanWriter(),
            new OwnershipBackfillPlanSignoffValidator(),
            new OwnershipBackfillPlanSignoffWriter());
    }

    private static async Task EnsureSchemasAsync(string connectionString)
    {
        var appOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using var appContext = new AppDbContext(appOptions);
        await appContext.Database.EnsureCreatedAsync();

        var workflowOptions = new DbContextOptionsBuilder<EngineeringWorkflowPersistenceDbContext>()
            .UseSqlite(connectionString)
            .Options;

        await using var workflowContext = new EngineeringWorkflowPersistenceDbContext(workflowOptions);
        await workflowContext.Database.MigrateAsync();
    }

    private static string BuildSqliteConnectionString()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-ownership-backfill-cli-{Guid.NewGuid():N}.db");
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


