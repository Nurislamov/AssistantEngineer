using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedExecutionEndpointRolloutGovernanceTests
{
    [Fact]
    public void ProtectedExecutionRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(ProtectedExecutionRolloutDocumentPath), $"Missing protected-execution rollout document: {ProtectedExecutionRolloutDocumentPath}");

        var content = File.ReadAllText(ProtectedExecutionRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected endpoint groups",
            "## Workflow execution policy",
            "## Calculation run policy",
            "## Resource scope resolution",
            "## Compatibility defaults",
            "## Tenant mismatch behavior",
            "## Rate limiting relationship",
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
    public void ProtectedExecutionRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProtectedExecutionRolloutDocumentPath);
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
    public void EndpointInventoryContainsP5_12WorkflowsExecuteEntry_WhenWorkflowControllerExists()
    {
        var workflowControllerPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "Calculations",
            "EngineeringWorkflowController.cs");

        if (!File.Exists(workflowControllerPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-12", StringComparison.Ordinal) &&
            string.Equals(endpoint.GetProperty("currentAuthStatus").GetString(), "AuthPilot", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("WorkflowsExecute", StringComparison.Ordinal));
    }

    [Fact]
    public void EndpointInventoryContainsP5_12CalculationEntry_WhenCalculationControllersExist()
    {
        var buildingControllerPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "Calculations",
            "BuildingLoadCalculationsController.cs");

        if (!File.Exists(buildingControllerPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-12", StringComparison.Ordinal) &&
            (endpoint.GetProperty("controller").GetString() ?? string.Empty).Contains("LoadCalculations", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("WorkflowsExecute", StringComparison.Ordinal));
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
    public void AuthorizationPolicyRolloutReferencesProtectedExecutionDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-execution-endpoints-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_12RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-12", roadmapItems);
    }

    [Fact]
    public void P5_12EntriesAreNotUnknownNeedsAudit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var p512Endpoints = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-12", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(p512Endpoints);
        Assert.All(p512Endpoints, endpoint =>
        {
            var status = endpoint.GetProperty("currentAuthStatus").GetString();
            Assert.False(string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void ReportAndArtifactEndpointGroupsAreNotMarkedAsP5_12()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var invalid = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-12", StringComparison.Ordinal) &&
                (
                    (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("Reports", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("controller").GetString() ?? string.Empty).Contains("Report", StringComparison.Ordinal) ||
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("artifacts", StringComparison.OrdinalIgnoreCase)
                ))
            .Select(endpoint => endpoint.GetProperty("routePattern").GetString() ?? string.Empty)
            .ToArray();

        Assert.True(invalid.Length == 0, $"Report/artifact endpoint groups were incorrectly marked as P5-12: {string.Join(", ", invalid)}");
    }

    private static string ProtectedExecutionRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-execution-endpoints-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
