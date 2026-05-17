using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5TenantIsolationIntegrationMatrixGovernanceTests
{
    [Fact]
    public void TenantIsolationMatrixDocumentsExist()
    {
        Assert.True(File.Exists(MatrixMarkdownPath), $"Missing tenant isolation matrix document: {MatrixMarkdownPath}");
        Assert.True(File.Exists(MatrixJsonPath), $"Missing tenant isolation matrix registry: {MatrixJsonPath}");
        Assert.True(File.Exists(MatrixSchemaPath), $"Missing tenant isolation matrix schema: {MatrixSchemaPath}");
    }

    [Fact]
    public void TenantIsolationMatrixDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(MatrixMarkdownPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Tenant test actors",
            "## Resource matrix",
            "## Expected behavior matrix",
            "## Endpoint groups",
            "## Known limitations"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TenantIsolationMatrixDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(MatrixMarkdownPath);
        var requiredPhrases = RequiredNonClaims;

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TenantIsolationMatrixJsonContainsRequiredEndpointGroups()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(MatrixJsonPath));
        var groups = document.RootElement.GetProperty("endpointGroups").EnumerateArray().ToArray();
        var groupNames = groups
            .Select(group => group.GetProperty("group").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var requiredGroups = new[]
        {
            "ProjectsRead",
            "ProjectsWrite",
            "BuildingsRead",
            "BuildingsWrite",
            "WorkflowsRead",
            "WorkflowsExecute",
            "CalculationRun",
            "ReportsRead",
            "ReportsWrite",
            "ArtifactRead",
            "WorkflowScenarioRead",
            "WorkflowJobRead",
            "WorkflowJobEventsRead",
            "ProjectTenantScopedReadService",
            "BuildingTenantScopedReadService",
            "WorkflowTenantScopedReadService"
        };

        foreach (var requiredGroup in requiredGroups)
        {
            Assert.Contains(requiredGroup, groupNames);
        }
    }

    [Fact]
    public void TenantIsolationMatrixJsonHasExpectedFieldsForEveryEndpointGroup()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(MatrixJsonPath));
        foreach (var group in document.RootElement.GetProperty("endpointGroups").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(group.GetProperty("group").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(group.GetProperty("permission").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(group.GetProperty("rolloutStage").GetString()));
            var sameTenantExpected = group.GetProperty("sameTenantExpected").GetString();
            var missingPermissionExpected = group.GetProperty("missingPermissionExpected").GetString();
            var crossTenantExpected = group.GetProperty("crossTenantExpected").GetString();
            var anonymousExpected = group.GetProperty("anonymousExpected").GetString();
            var documentedOnly = string.Equals(sameTenantExpected, "DocumentedOnly", StringComparison.Ordinal);

            Assert.False(string.IsNullOrWhiteSpace(sameTenantExpected));
            Assert.True(
                documentedOnly || string.Equals("Forbidden", missingPermissionExpected, StringComparison.Ordinal),
                $"Unexpected missing-permission expectation for {group.GetProperty("group").GetString()}.");
            Assert.True(
                documentedOnly ||
                string.Equals("ForbiddenOrNotFoundByOption", crossTenantExpected, StringComparison.Ordinal) ||
                string.Equals("FailureOrNotFoundByOption", crossTenantExpected, StringComparison.Ordinal),
                $"Unexpected cross-tenant expectation for {group.GetProperty("group").GetString()}.");
            Assert.True(
                documentedOnly || string.Equals("Unauthorized", anonymousExpected, StringComparison.Ordinal),
                $"Unexpected anonymous expectation for {group.GetProperty("group").GetString()}.");
            Assert.NotEmpty(group.GetProperty("coveredByTests").EnumerateArray());
            Assert.Equal(JsonValueKind.Array, group.GetProperty("knownLimitations").ValueKind);
        }
    }

    [Fact]
    public void TenantIsolationMatrixSchemaContainsRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(MatrixSchemaPath));
        var required = document.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var field in new[] { "version", "lastReviewedDate", "actors", "resourceFamilies", "endpointGroups", "nonClaims" })
        {
            Assert.Contains(field, required);
        }

        var endpointGroupRequired = document.RootElement
            .GetProperty("properties")
            .GetProperty("endpointGroups")
            .GetProperty("items")
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var field in new[]
        {
            "group",
            "permission",
            "rolloutStage",
            "sameTenantExpected",
            "missingPermissionExpected",
            "crossTenantExpected",
            "anonymousExpected",
            "coveredByTests",
            "knownLimitations"
        })
        {
            Assert.Contains(field, endpointGroupRequired);
        }
    }

    [Fact]
    public void TenantIsolationMatrixDoesNotClaimFullTenantIsolation()
    {
        var markdown = File.ReadAllText(MatrixMarkdownPath);
        var json = File.ReadAllText(MatrixJsonPath);

        Assert.DoesNotContain("full tenant isolation is implemented", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full tenant isolation is implemented", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No full multi-tenant isolation claim yet", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_15RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-15", roadmapItems);
    }

    [Fact]
    public void SecurityAndTenantDocsReferenceTenantIsolationMatrix()
    {
        Assert.Contains("tenant-isolation-integration-matrix.md", File.ReadAllText(SecurityGuardrailsPath), StringComparison.Ordinal);
        Assert.Contains("tenant-isolation-integration-matrix.md", File.ReadAllText(ProjectTenantScopingPath), StringComparison.Ordinal);
    }

    private static readonly string[] RequiredNonClaims =
    [
        "No production security certification claim",
        "No SOC 2 / ISO 27001 compliance claim",
        "No full multi-tenant isolation claim yet",
        "No database row-level security claim",
        "No external identity provider integration claim",
        "No certified/certification claim"
    ];

    private static string MatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.md");

    private static string MatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.json");

    private static string MatrixSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "tenant-isolation-integration-matrix.schema.json");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string SecurityGuardrailsPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string ProjectTenantScopingPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "project-tenant-scoping-model.md");
}
