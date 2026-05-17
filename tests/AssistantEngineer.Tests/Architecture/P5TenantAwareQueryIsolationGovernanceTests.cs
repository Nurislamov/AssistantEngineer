using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5TenantAwareQueryIsolationGovernanceTests
{
    [Fact]
    public void TenantAwareQueryIsolationDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(DocumentPath), $"Missing P5-16B document: {DocumentPath}");

        var content = File.ReadAllText(DocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Query isolation model",
            "## TenantQueryContext",
            "## TenantQueryIsolationPolicy",
            "## Project tenant-scoped reads",
            "## Building tenant-scoped reads",
            "## Workflow tenant-scoped reads",
            "## Legacy unscoped resources",
            "## Compatibility defaults",
            "## Relationship to authorization gates",
            "## Relationship to persisted ownership fields",
            "## Why global EF query filters are not enabled yet",
            "## Known limitations",
            "## Next steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TenantAwareQueryIsolationDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(DocumentPath);
        foreach (var phrase in RequiredNonClaims)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RelatedSecurityDocsReferenceTenantAwareQueryIsolation()
    {
        Assert.Contains("tenant-aware-query-isolation-services.md", File.ReadAllText(PersistenceOwnershipPath), StringComparison.Ordinal);
        Assert.Contains("tenant-aware-query-isolation-services.md", File.ReadAllText(TenantIsolationMatrixPath), StringComparison.Ordinal);
        Assert.Contains("tenant-aware-query-isolation-services.md", File.ReadAllText(ProjectTenantScopingPath), StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_16B()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-16B", roadmapItems);
    }

    [Fact]
    public void DocumentsDoNotClaimGlobalFiltersOrFullTenantIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(DocumentPath),
            File.ReadAllText(PersistenceOwnershipPath),
            File.ReadAllText(TenantIsolationMatrixPath),
            File.ReadAllText(ProductionInventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("global EF query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("all queries are tenant-filtered", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TenantAwareQueryIsolationServicesExist()
    {
        Assert.True(File.Exists(TenantQueryPolicyInterfacePath), $"Missing tenant query policy interface: {TenantQueryPolicyInterfacePath}");
        Assert.True(File.Exists(TenantQueryPolicyImplementationPath), $"Missing tenant query policy implementation: {TenantQueryPolicyImplementationPath}");
        Assert.True(File.Exists(ProjectScopedReadServiceInterfacePath), $"Missing project tenant-scoped read service interface: {ProjectScopedReadServiceInterfacePath}");
        Assert.True(File.Exists(BuildingScopedReadServiceInterfacePath), $"Missing building tenant-scoped read service interface: {BuildingScopedReadServiceInterfacePath}");
    }

    private static readonly string[] RequiredNonClaims =
    [
        "No production security certification claim",
        "No SOC 2 / ISO 27001 compliance claim",
        "No full multi-tenant isolation claim yet",
        "No database row-level security claim",
        "No global EF query filter claim",
        "No external identity provider integration claim",
        "No certified/certification claim"
    ];

    private static string DocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-aware-query-isolation-services.md");

    private static string PersistenceOwnershipPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "persistence-backed-tenant-ownership-fields.md");

    private static string TenantIsolationMatrixPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string ProjectTenantScopingPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "project-tenant-scoping-model.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string TenantQueryPolicyInterfacePath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.Identity", "Application", "Contracts", "Access", "ITenantQueryIsolationPolicy.cs");

    private static string TenantQueryPolicyImplementationPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.Identity", "Application", "Services", "Access", "TenantQueryIsolationPolicy.cs");

    private static string ProjectScopedReadServiceInterfacePath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Security", "TenantIsolation", "IProjectTenantScopedReadService.cs");

    private static string BuildingScopedReadServiceInterfacePath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Security", "TenantIsolation", "IBuildingTenantScopedReadService.cs");
}
