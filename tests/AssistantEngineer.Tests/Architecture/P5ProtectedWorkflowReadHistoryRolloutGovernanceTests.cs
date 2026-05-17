using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedWorkflowReadHistoryRolloutGovernanceTests
{
    [Fact]
    public void ProtectedWorkflowReadHistoryRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(ProtectedWorkflowReadHistoryRolloutDocumentPath), $"Missing protected-workflow-read-history rollout document: {ProtectedWorkflowReadHistoryRolloutDocumentPath}");

        var content = File.ReadAllText(ProtectedWorkflowReadHistoryRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected endpoint groups",
            "## Workflow read policy",
            "## Scenario read policy",
            "## Job status/read policy",
            "## Resource scope resolution",
            "## Anti-enumeration behavior",
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
    public void ProtectedWorkflowReadHistoryRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProtectedWorkflowReadHistoryRolloutDocumentPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No claim that all API endpoints are protected yet",
            "No claim that workflow tenant isolation is complete unless resolver scope is proven"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EndpointInventoryContainsP5_14WorkflowsReadEntries_WhenWorkflowControllerExists()
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
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-14", StringComparison.Ordinal) &&
            string.Equals(endpoint.GetProperty("currentAuthStatus").GetString(), "AuthPilot", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("WorkflowsRead", StringComparison.Ordinal));
    }

    [Fact]
    public void P5_14EntriesAreNotUnknownNeedsAudit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var p514Endpoints = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-14", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(p514Endpoints);
        Assert.All(p514Endpoints, endpoint =>
        {
            var status = endpoint.GetProperty("currentAuthStatus").GetString();
            Assert.False(string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal));
        });
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
    public void AuthorizationPolicyRolloutReferencesProtectedWorkflowReadHistoryDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-workflow-read-history-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_14RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-14", roadmapItems);
    }

    [Fact]
    public void ExecutionEndpointsAreNotIncorrectlyDowngradedToWorkflowsReadInP5_14()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var invalid = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-14", StringComparison.Ordinal) &&
                (
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("prepare-calculation", StringComparison.OrdinalIgnoreCase) ||
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("run-calculation", StringComparison.OrdinalIgnoreCase) ||
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("/jobs [post]", StringComparison.OrdinalIgnoreCase) ||
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("/jobs/{jobId}/cancel", StringComparison.OrdinalIgnoreCase)
                ) &&
                !(endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("WorkflowsExecute", StringComparison.Ordinal))
            .Select(endpoint => endpoint.GetProperty("routePattern").GetString() ?? string.Empty)
            .ToArray();

        Assert.True(invalid.Length == 0, $"Execution endpoints must not be downgraded to WorkflowsRead in P5-14: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void ReportArtifactEndpointsAreNotIncorrectlyMarkedAsP5_14()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var invalid = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-14", StringComparison.Ordinal) &&
                (
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("/report", StringComparison.OrdinalIgnoreCase) ||
                    (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("artifacts", StringComparison.OrdinalIgnoreCase)
                ))
            .Select(endpoint => endpoint.GetProperty("routePattern").GetString() ?? string.Empty)
            .ToArray();

        Assert.True(invalid.Length == 0, $"Report/artifact routes should remain in dedicated rollout stage, not P5-14: {string.Join(", ", invalid)}");
    }

    private static string ProtectedWorkflowReadHistoryRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-workflow-read-history-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
