using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5TenantAwareReadControllerIntegrationGovernanceTests
{
    [Fact]
    public void TenantAwareReadControllerIntegrationDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(DocumentPath), $"Missing P5-16C document: {DocumentPath}");

        var content = File.ReadAllText(DocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Integrated endpoint groups",
            "## Project read integration",
            "## Building read integration",
            "## Compatibility defaults",
            "## Legacy unscoped behavior",
            "## Anti-enumeration behavior",
            "## Relationship to authorization gates",
            "## Relationship to tenant-aware query services",
            "## What remains staged",
            "## Next steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TenantAwareReadControllerIntegrationDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(DocumentPath);
        foreach (var phrase in RequiredNonClaims)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TenantAwareQueryIsolationServicesDocumentReferencesP5_16CDocument()
    {
        var content = File.ReadAllText(TenantAwareQueryIsolationServicesPath);
        Assert.Contains("tenant-aware-read-controller-integration.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TenantIsolationMatrixMarksProjectsAndBuildingsReadWithP5_16CControllerIntegration()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(TenantIsolationMatrixJsonPath));
        var endpointGroups = document.RootElement.GetProperty("endpointGroups").EnumerateArray().ToArray();

        AssertControllerIntegration(endpointGroups, "ProjectsRead");
        AssertControllerIntegration(endpointGroups, "BuildingsRead");
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_16C()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-16C", roadmapItems);
    }

    [Fact]
    public void DocumentsDoNotClaimGlobalEfFiltersOrFullTenantIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(DocumentPath),
            File.ReadAllText(TenantAwareQueryIsolationServicesPath),
            File.ReadAllText(TenantIsolationMatrixMarkdownPath),
            File.ReadAllText(ProductionInventoryMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void WorkflowControllerIntegrationReferencesP5_16DExtensionDocument()
    {
        var content = File.ReadAllText(DocumentPath);
        Assert.Contains("workflow-tenant-aware-read-integration.md", content, StringComparison.Ordinal);
    }

    private static void AssertControllerIntegration(
        JsonElement[] endpointGroups,
        string groupName)
    {
        var group = Assert.Single(endpointGroups, candidate =>
            string.Equals(candidate.GetProperty("group").GetString(), groupName, StringComparison.Ordinal));

        Assert.True(group.TryGetProperty("controllerIntegration", out var controllerIntegration), $"Missing controllerIntegration for {groupName}.");
        Assert.Equal("P5-16C", controllerIntegration.GetString());

        Assert.True(group.TryGetProperty("tenantAwareQueryService", out var tenantAwareQueryService), $"Missing tenantAwareQueryService for {groupName}.");
        Assert.True(tenantAwareQueryService.GetBoolean());

        var coveredByTests = group.GetProperty("coveredByTests")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ProtectedReadControllersTenantScopedQueryIntegrationTests", coveredByTests);
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
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-aware-read-controller-integration.md");

    private static string TenantAwareQueryIsolationServicesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-aware-query-isolation-services.md");

    private static string TenantIsolationMatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.json");

    private static string TenantIsolationMatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");
}
