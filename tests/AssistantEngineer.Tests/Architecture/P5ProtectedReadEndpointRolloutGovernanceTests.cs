using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedReadEndpointRolloutGovernanceTests
{
    [Fact]
    public void ProtectedReadRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(ProtectedReadRolloutDocumentPath), $"Missing protected-read rollout document: {ProtectedReadRolloutDocumentPath}");

        var content = File.ReadAllText(ProtectedReadRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected endpoint groups",
            "## Protection behavior",
            "## Compatibility defaults",
            "## Project read policy",
            "## Building read policy",
            "## Tenant mismatch behavior",
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
    public void ProtectedReadRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProtectedReadRolloutDocumentPath);
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
    public void EndpointInventoryContainsP5_10ProjectsReadEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-10", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("ProjectsRead", StringComparison.Ordinal));
    }

    [Fact]
    public void EndpointInventoryContainsP5_10BuildingsReadEntry()
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
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-10", StringComparison.Ordinal) &&
            (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("BuildingsRead", StringComparison.Ordinal));
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
    public void AuthorizationPolicyRolloutReferencesProtectedReadDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-read-endpoints-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_10RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-10", roadmapItems);
    }

    [Fact]
    public void P5_10EntriesAreNotUnknownNeedsAudit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var p510Endpoints = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-10", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(p510Endpoints);
        Assert.All(p510Endpoints, endpoint =>
        {
            var status = endpoint.GetProperty("currentAuthStatus").GetString();
            Assert.False(string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal));
        });
    }

    private static string ProtectedReadRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-read-endpoints-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
