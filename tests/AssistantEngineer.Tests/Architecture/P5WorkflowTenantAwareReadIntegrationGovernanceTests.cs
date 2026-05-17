using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5WorkflowTenantAwareReadIntegrationGovernanceTests
{
    [Fact]
    public void WorkflowTenantAwareReadIntegrationDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(DocumentPath), $"Missing P5-16D document: {DocumentPath}");

        var content = File.ReadAllText(DocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Integrated endpoint groups",
            "## Workflow state read integration",
            "## Scenario read/list integration",
            "## Job read/events/list integration",
            "## Metadata ownership resolution",
            "## Anti-enumeration behavior",
            "## Compatibility defaults",
            "## Relationship to protected workflow read rollout",
            "## Relationship to tenant-aware query services",
            "## Staged limitations",
            "## Next steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WorkflowTenantAwareReadIntegrationDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(DocumentPath);
        foreach (var phrase in RequiredNonClaims)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TenantAwareQueryIsolationServicesDocumentReferencesP5_16DDocument()
    {
        var content = File.ReadAllText(TenantAwareQueryIsolationServicesPath);
        Assert.Contains("workflow-tenant-aware-read-integration.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProtectedWorkflowReadHistoryRolloutReferencesP5_16DDocument()
    {
        var content = File.ReadAllText(ProtectedWorkflowReadHistoryRolloutPath);
        Assert.Contains("workflow-tenant-aware-read-integration.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TenantIsolationMatrixMarksWorkflowReadGroupsWithP5_16DControllerIntegration()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(TenantIsolationMatrixJsonPath));
        var endpointGroups = document.RootElement.GetProperty("endpointGroups").EnumerateArray().ToArray();

        AssertControllerIntegration(endpointGroups, "WorkflowsRead");
        AssertControllerIntegration(endpointGroups, "WorkflowScenarioRead");
        AssertControllerIntegration(endpointGroups, "WorkflowJobRead");
        AssertControllerIntegration(endpointGroups, "WorkflowJobEventsRead");
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_16D()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-16D", roadmapItems);
    }

    [Fact]
    public void DocumentsDoNotClaimGlobalFiltersDbRlsOrFullTenantIsolation()
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
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void WorkflowTenantScopedReadServiceIsRegisteredInDi()
    {
        var registration = File.ReadAllText(ApiAuthenticationRegistrationPath);
        Assert.Contains("AddScoped<IWorkflowTenantScopedReadService, WorkflowTenantScopedReadService>()", registration, StringComparison.Ordinal);
    }

    private static void AssertControllerIntegration(
        JsonElement[] endpointGroups,
        string groupName)
    {
        var group = Assert.Single(endpointGroups, candidate =>
            string.Equals(candidate.GetProperty("group").GetString(), groupName, StringComparison.Ordinal));

        Assert.True(group.TryGetProperty("controllerIntegration", out var controllerIntegration), $"Missing controllerIntegration for {groupName}.");
        Assert.Equal("P5-16D", controllerIntegration.GetString());

        Assert.True(group.TryGetProperty("tenantAwareQueryService", out var tenantAwareQueryService), $"Missing tenantAwareQueryService for {groupName}.");
        Assert.True(tenantAwareQueryService.GetBoolean());

        var coveredByTests = group.GetProperty("coveredByTests")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ProtectedWorkflowReadControllersTenantScopedQueryIntegrationTests", coveredByTests);
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
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-tenant-aware-read-integration.md");

    private static string TenantAwareQueryIsolationServicesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-aware-query-isolation-services.md");

    private static string ProtectedWorkflowReadHistoryRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-workflow-read-history-rollout.md");

    private static string TenantIsolationMatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.json");

    private static string TenantIsolationMatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string ApiAuthenticationRegistrationPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Configuration", "ApiAuthenticationRegistration.cs");
}
