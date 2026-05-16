using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class DurablePersistenceMigrationGovernanceTests
{
    private const string NewMigrationId = "20260515000100_AddEngineeringWorkflowQueuedJobAndArtifactLookupIndexes";

    [Fact]
    public void TargetedP4_08BMigrationFileExists()
    {
        var migrationPath = Path.Combine(
            WorkflowMigrationsDirectory,
            $"{NewMigrationId}.cs");

        Assert.True(File.Exists(migrationPath), $"Expected targeted migration file is missing: {migrationPath}");
    }

    [Fact]
    public void InventoryListsTargetedP4_08BMigration()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        var migrationIds = inventory.RootElement.GetProperty("migrations")
            .EnumerateArray()
            .Select(item => item.GetProperty("migrationIdOrName").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(NewMigrationId, migrationIds);
    }

    [Fact]
    public void InventoryListsTargetedLookupIndexes()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        var indexNames = inventory.RootElement.GetProperty("indexesAndConstraints")
            .EnumerateArray()
            .Select(item => item.GetProperty("indexOrConstraint").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(
            "IX_engineering_workflow_jobs_Status_CancellationRequested_QueuedAtUtc_CreatedAtUtc_Id",
            indexNames);
        Assert.Contains(
            "IX_engineering_workflow_artifacts_ScenarioId_ArtifactKind_CreatedAtUtc_Id",
            indexNames);
    }

    [Fact]
    public void HardeningDocumentRetainsMigrationAndIndexPolicyAndP4_08BSection()
    {
        var content = File.ReadAllText(HardeningDocPath);

        Assert.Contains("## Migration policy", content, StringComparison.Ordinal);
        Assert.Contains("## Index/constraint policy", content, StringComparison.Ordinal);
        Assert.Contains("## P4-08B Targeted Hardening Result", content, StringComparison.Ordinal);
        Assert.Contains(NewMigrationId, content, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoricalWorkflowMigrationFilesRemainPresent()
    {
        var expectedHistoricalFiles = new[]
        {
            "20260510000100_InitialEngineeringWorkflowPersistence.cs",
            "20260511000100_AddEngineeringJobClaimLeaseMetadata.cs",
            "20260511000200_AddEngineeringWorkflowIdempotencyRecords.cs"
        };

        foreach (var fileName in expectedHistoricalFiles)
        {
            var path = Path.Combine(WorkflowMigrationsDirectory, fileName);
            Assert.True(File.Exists(path), $"Historical migration file is missing: {path}");
        }
    }

    [Fact]
    public void HardeningDocumentDoesNotAddOverclaimPhrases()
    {
        var content = File.ReadAllText(HardeningDocPath);
        var ashraeValidatedPhrase = "ASHRAE 140 " + "validated";
        var energyPlusParityPhrase = "EnergyPlus " + "parity";

        Assert.DoesNotContain("exactly-once guarantee", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("production certified", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ashraeValidatedPhrase, content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(energyPlusParityPhrase, content, StringComparison.OrdinalIgnoreCase);
    }

    private static string HardeningDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-hardening.md");

    private static string InventoryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-inventory.json");

    private static string WorkflowMigrationsDirectory =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Persistence",
            "Durable",
            "Migrations");
}
