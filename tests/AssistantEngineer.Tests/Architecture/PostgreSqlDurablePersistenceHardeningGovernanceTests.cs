using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class PostgreSqlDurablePersistenceHardeningGovernanceTests
{
    [Fact]
    public void DurablePersistenceHardeningDocumentExists()
    {
        Assert.True(
            File.Exists(HardeningDocPath),
            $"PostgreSQL durable persistence hardening document is missing: {HardeningDocPath}");
    }

    [Fact]
    public void DurablePersistenceHardeningDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(HardeningDocPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Provider model",
            "## Current inventory",
            "## Transaction boundary policy",
            "## Migration policy",
            "## Index/constraint policy",
            "## Payload/artifact policy",
            "## Smoke test strategy",
            "## Future work"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DurablePersistenceHardeningDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(HardeningDocPath);

        Assert.Contains("No globally exactly-once distributed execution claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No production certification claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No claim that in-memory provider is durable", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No claim that SQLite provider represents multi-node production PostgreSQL behavior", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No external compliance/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryJsonAndSchemaExistAndParse()
    {
        Assert.True(File.Exists(InventoryPath), $"Inventory file is missing: {InventoryPath}");
        Assert.True(File.Exists(InventorySchemaPath), $"Inventory schema file is missing: {InventorySchemaPath}");

        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        using var schema = JsonDocument.Parse(File.ReadAllText(InventorySchemaPath));

        Assert.Equal(JsonValueKind.Object, inventory.RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Object, schema.RootElement.ValueKind);
    }

    [Fact]
    public void InventorySchemaContainsRequiredTopLevelFields()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(InventorySchemaPath));
        var required = schema.RootElement
            .GetProperty("requiredTopLevelFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("version", required);
        Assert.Contains("lastReviewedDate", required);
        Assert.Contains("dbContexts", required);
        Assert.Contains("repositories", required);
        Assert.Contains("migrations", required);
        Assert.Contains("indexesAndConstraints", required);
        Assert.Contains("knownRisks", required);
    }

    [Fact]
    public void InventoryJsonHasUniqueDbContextNamesAndMigrationsPerContext()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        var root = inventory.RootElement;

        var dbContextNames = root.GetProperty("dbContexts")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();

        Assert.Equal(dbContextNames.Length, dbContextNames.Distinct(StringComparer.Ordinal).Count());

        var migrationPairs = root.GetProperty("migrations")
            .EnumerateArray()
            .Select(item =>
            {
                var dbContext = item.GetProperty("dbContext").GetString() ?? string.Empty;
                var migration = item.GetProperty("migrationIdOrName").GetString() ?? string.Empty;
                return $"{dbContext}|{migration}";
            })
            .ToArray();

        Assert.Equal(migrationPairs.Length, migrationPairs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void InventoryJsonContainsKnownRisksArray()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        var knownRisks = inventory.RootElement.GetProperty("knownRisks");

        Assert.Equal(JsonValueKind.Array, knownRisks.ValueKind);
    }

    [Fact]
    public void InMemoryDefaultInProductionConfigMustBeExplicitlyTrackedAsRisk()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllText(InventoryPath));
        var knownRiskIds = inventory.RootElement.GetProperty("knownRisks")
            .EnumerateArray()
            .Select(item => item.GetProperty("riskId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var appSettingsContent = File.ReadAllText(AppSettingsPath);
        using var appSettings = JsonDocument.Parse(appSettingsContent);

        var provider = appSettings.RootElement
            .GetProperty("EngineeringWorkflowPersistence")
            .GetProperty("Provider")
            .GetString();

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains(
                "RISK-WF-PROVIDER-INMEMORY-DEFAULT",
                knownRiskIds);
        }
    }

    [Fact]
    public void HardeningDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                HardeningDocPath,
                InventoryPath,
                InventorySchemaPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string HardeningDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-hardening.md");

    private static string InventoryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-inventory.schema.json");

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "appsettings.json");
}
