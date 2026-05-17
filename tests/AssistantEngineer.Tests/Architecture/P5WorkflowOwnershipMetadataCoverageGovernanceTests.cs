using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5WorkflowOwnershipMetadataCoverageGovernanceTests
{
    [Fact]
    public void WorkflowOwnershipMetadataCoverageDocumentsExist()
    {
        Assert.True(File.Exists(CoverageDocumentPath), $"Missing P5-17 metadata coverage document: {CoverageDocumentPath}");
        Assert.True(File.Exists(CoverageJsonPath), $"Missing P5-17 metadata coverage JSON: {CoverageJsonPath}");
        Assert.True(File.Exists(CoverageSchemaPath), $"Missing P5-17 metadata coverage schema: {CoverageSchemaPath}");
    }

    [Fact]
    public void WorkflowOwnershipMetadataCoverageDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(CoverageDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Current metadata inventory",
            "## Workflow metadata coverage",
            "## Scenario metadata coverage",
            "## Job metadata coverage",
            "## Resolver behavior",
            "## Staged fallback cases",
            "## Safe metadata additions",
            "## Migration status",
            "## Known limitations",
            "## Next steps"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WorkflowOwnershipMetadataCoverageDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(CoverageDocumentPath);

        foreach (var phrase in RequiredNonClaims)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CoverageJsonIncludesWorkflowScenarioAndJobEntries()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CoverageJsonPath));
        var records = document.RootElement.GetProperty("metadataRecords").EnumerateArray().ToArray();

        Assert.Contains(records, record => string.Equals(record.GetProperty("recordType").GetString(), "WorkflowState", StringComparison.Ordinal));
        Assert.Contains(records, record => string.Equals(record.GetProperty("recordType").GetString(), "ScenarioRecord", StringComparison.Ordinal));
        Assert.Contains(records, record => string.Equals(record.GetProperty("recordType").GetString(), "JobRecord", StringComparison.Ordinal));
    }

    [Fact]
    public void CoverageJsonDoesNotClaimEverythingComplete()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CoverageJsonPath));

        var coverages = document.RootElement
            .GetProperty("metadataRecords")
            .EnumerateArray()
            .Select(record => record.GetProperty("tenantScopeCoverage").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("Partial", coverages);
    }

    [Fact]
    public void TenantAwareQueryIsolationServicesReferencesP5_17CoverageDocument()
    {
        var content = File.ReadAllText(TenantAwareQueryIsolationServicesPath);
        Assert.Contains("workflow-ownership-metadata-coverage.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_17()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-17", roadmapItems);
    }

    [Fact]
    public void TenantIsolationMatrixIncludesWorkflowMetadataCoverageFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(TenantIsolationMatrixJsonPath));
        var groups = document.RootElement.GetProperty("endpointGroups").EnumerateArray().ToArray();

        AssertWorkflowGroupMetadataCoverage(groups, "WorkflowsRead");
        AssertWorkflowGroupMetadataCoverage(groups, "WorkflowScenarioRead");
        AssertWorkflowGroupMetadataCoverage(groups, "WorkflowJobRead");
        AssertWorkflowGroupMetadataCoverage(groups, "WorkflowJobEventsRead");
    }

    [Fact]
    public void DocumentsDoNotClaimBackfillCompleteGlobalFiltersDbRlsOrFullIsolation()
    {
        var docs = new[]
        {
            File.ReadAllText(CoverageDocumentPath),
            File.ReadAllText(WorkflowTenantAwareReadIntegrationPath),
            File.ReadAllText(ProductionInventoryMarkdownPath),
            File.ReadAllText(TenantIsolationMatrixMarkdownPath)
        };

        foreach (var content in docs)
        {
            Assert.DoesNotContain("ownership backfill has been completed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ownership backfill is fully completed", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global ef query filters are enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database row-level security is enabled", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("full tenant isolation is implemented", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertWorkflowGroupMetadataCoverage(
        JsonElement[] groups,
        string groupName)
    {
        var group = Assert.Single(groups, candidate =>
            string.Equals(candidate.GetProperty("group").GetString(), groupName, StringComparison.Ordinal));

        Assert.True(group.TryGetProperty("metadataCoverage", out var metadataCoverage), $"Missing metadataCoverage for {groupName}.");
        var value = metadataCoverage.GetString();
        Assert.True(
            string.Equals(value, "Complete", StringComparison.Ordinal) ||
            string.Equals(value, "Partial", StringComparison.Ordinal) ||
            string.Equals(value, "Missing", StringComparison.Ordinal) ||
            string.Equals(value, "UnknownNeedsAudit", StringComparison.Ordinal),
            $"Unexpected metadataCoverage value for {groupName}: {value}");
    }

    private static readonly string[] RequiredNonClaims =
    [
        "No production security certification claim",
        "No SOC 2 / ISO 27001 compliance claim",
        "No full multi-tenant isolation claim yet",
        "No database row-level security claim",
        "No global EF query filter claim",
        "No ownership backfill claim",
        "No external identity provider integration claim",
        "No certified/certification claim"
    ];

    private static string CoverageDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-ownership-metadata-coverage.md");

    private static string CoverageJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-ownership-metadata-coverage.json");

    private static string CoverageSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-ownership-metadata-coverage.schema.json");

    private static string TenantAwareQueryIsolationServicesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-aware-query-isolation-services.md");

    private static string WorkflowTenantAwareReadIntegrationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "workflow-tenant-aware-read-integration.md");

    private static string TenantIsolationMatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.json");

    private static string TenantIsolationMatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");
}
