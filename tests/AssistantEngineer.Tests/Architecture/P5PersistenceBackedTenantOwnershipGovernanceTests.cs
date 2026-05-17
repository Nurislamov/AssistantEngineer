using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5PersistenceBackedTenantOwnershipGovernanceTests
{
    [Fact]
    public void PersistenceBackedTenantOwnershipDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(DocumentPath), $"Missing P5-16A document: {DocumentPath}");

        var content = File.ReadAllText(DocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Ownership fields",
            "## Project ownership model",
            "## Building scope derivation",
            "## Workflow/scenario/job scope derivation",
            "## Legacy unscoped project behavior",
            "## Migration details",
            "## Resolver behavior",
            "## Known limitations",
            "## Next steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PersistenceBackedTenantOwnershipDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(DocumentPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No database row-level security claim",
            "No external identity provider integration claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SecurityDocsReferencePersistenceBackedTenantOwnershipDocument()
    {
        Assert.Contains("persistence-backed-tenant-ownership-fields.md", File.ReadAllText(ProjectTenantScopingPath), StringComparison.Ordinal);
        Assert.Contains("persistence-backed-tenant-ownership-fields.md", File.ReadAllText(TenantIsolationMatrixPath), StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_16A()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-16A", roadmapItems);
    }

    [Fact]
    public void DocumentsDoNotClaimFinishedTenantIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(DocumentPath),
            File.ReadAllText(ProjectTenantScopingPath),
            File.ReadAllText(TenantIsolationMatrixPath),
            File.ReadAllText(ProductionInventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("tenant isolation is complete", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("all queries tenant-filtered", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProjectTenantOwnershipMigrationExistsAndIsAppendOnly()
    {
        var migrationPath = Directory.EnumerateFiles(MigrationsDirectory, "*AddProjectTenantOwnershipFields.cs")
            .Single(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var migration = File.ReadAllText(migrationPath);

        Assert.Contains("AddColumn<int>", migration, StringComparison.Ordinal);
        Assert.Contains("OrganizationId", migration, StringComparison.Ordinal);
        Assert.Contains("OwnerUserId", migration, StringComparison.Ordinal);
        Assert.Contains("IX_Projects_OrganizationId", migration, StringComparison.Ordinal);
        Assert.Contains("IX_Projects_OwnerUserId", migration, StringComparison.Ordinal);
        Assert.Contains("IX_Projects_OrganizationId_Id", migration, StringComparison.Ordinal);

        var forbiddenOperations = new[]
        {
            "DropTable(",
            "DropColumn(",
            "RenameColumn(",
            "DELETE ",
            "TRUNCATE "
        };

        foreach (var forbiddenOperation in forbiddenOperations)
        {
            Assert.DoesNotContain(forbiddenOperation, migration, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ModelSnapshotContainsProjectOwnershipFieldsAndIndexes()
    {
        var snapshot = File.ReadAllText(ModelSnapshotPath);

        Assert.Contains("OrganizationId", snapshot, StringComparison.Ordinal);
        Assert.Contains("OwnerUserId", snapshot, StringComparison.Ordinal);
        Assert.Contains("HasIndex(\"OrganizationId\")", snapshot, StringComparison.Ordinal);
        Assert.Contains("HasIndex(\"OwnerUserId\")", snapshot, StringComparison.Ordinal);
        Assert.Contains("HasIndex(\"OrganizationId\", \"Id\")", snapshot, StringComparison.Ordinal);
    }

    private static string DocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "persistence-backed-tenant-ownership-fields.md");

    private static string ProjectTenantScopingPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "project-tenant-scoping-model.md");

    private static string TenantIsolationMatrixPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string MigrationsDirectory =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Infrastructure", "Persistence", "Migrations");

    private static string ModelSnapshotPath =>
        Path.Combine(MigrationsDirectory, "AppDbContextModelSnapshot.cs");
}
