using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedWriteEndpointRolloutGovernanceTests
{
    [Fact]
    public void ProtectedWriteRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(ProtectedWriteRolloutDocumentPath), $"Missing protected-write rollout document: {ProtectedWriteRolloutDocumentPath}");

        var content = File.ReadAllText(ProtectedWriteRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected endpoint groups",
            "## Protection behavior",
            "## Compatibility defaults",
            "## Project write policy",
            "## Building write policy",
            "## Tenant mismatch behavior",
            "## Audit/observability behavior",
            "## Test matrix",
            "## What remains unprotected",
            "## Next rollout candidates"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProtectedWriteRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProtectedWriteRolloutDocumentPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No claim that all API endpoints are protected yet"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EndpointInventoryContainsP5_11ProjectsWriteEntry_WhenProjectControllerExists()
    {
        var projectsControllerPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "Projects",
            "ProjectsController.cs");

        if (!File.Exists(projectsControllerPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-11", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("ProjectsWrite", StringComparison.Ordinal));
    }

    [Fact]
    public void EndpointInventoryContainsP5_11BuildingsWriteEntry_WhenBuildingControllerExists()
    {
        var buildingsControllerPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "Buildings",
            "BuildingsController.cs");

        if (!File.Exists(buildingsControllerPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-11", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("BuildingsWrite", StringComparison.Ordinal));
    }

    [Fact]
    public void EndpointInventoryDoesNotClaimAllEndpointsAreProtected()
    {
        var markdown = File.ReadAllText(EndpointInventoryMarkdownPath);
        var json = File.ReadAllText(EndpointInventoryJsonPath);

        Assert.DoesNotContain("all endpoints protected", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("all endpoints protected", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthorizationPolicyRolloutReferencesProtectedWriteDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-write-endpoints-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_11RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-11", roadmapItems);
    }

    [Fact]
    public void P5_11EntriesAreNotUnknownNeedsAudit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var p511Endpoints = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-11", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(p511Endpoints);
        Assert.All(p511Endpoints, endpoint =>
        {
            var status = endpoint.GetProperty("currentAuthStatus").GetString();
            Assert.False(string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void WorkflowCalculationAndReportEndpointsAreNotMarkedAsP5_11()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var invalid = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-11", StringComparison.Ordinal) &&
                (
                    (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("Workflows", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("Reports", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("controller").GetString() ?? string.Empty).Contains("Calculations", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("controller").GetString() ?? string.Empty).Contains("Workflow", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("controller").GetString() ?? string.Empty).Contains("Report", StringComparison.Ordinal)
                ))
            .Select(endpoint => endpoint.GetProperty("controller").GetString() ?? string.Empty)
            .ToArray();

        Assert.True(invalid.Length == 0, $"Non-target controller groups were marked as P5-11: {string.Join(", ", invalid)}");
    }

    private static string ProtectedWriteRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-write-endpoints-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
