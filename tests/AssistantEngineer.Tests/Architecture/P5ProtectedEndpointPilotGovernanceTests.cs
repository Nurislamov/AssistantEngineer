using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedEndpointPilotGovernanceTests
{
    [Fact]
    public void PilotRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(PilotRolloutDocumentPath), $"Missing pilot rollout document: {PilotRolloutDocumentPath}");

        var content = File.ReadAllText(PilotRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected pilot endpoint",
            "## Protection behavior",
            "## Compatibility defaults",
            "## Environment gate preservation",
            "## Test matrix",
            "## What is intentionally not protected yet",
            "## Next rollout candidates"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PilotRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(PilotRolloutDocumentPath);
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
    public void EndpointInventoryContainsP5_09AuthPilotEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("currentAuthStatus").GetString(), "AuthPilot", StringComparison.Ordinal) &&
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-09", StringComparison.Ordinal));
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
    public void AuthorizationPolicyRolloutReferencesPilotDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-endpoint-pilot-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_09RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-09", roadmapItems);
    }

    [Fact]
    public void SecurityRegressionGuardrailsReferenceEndpointInventory()
    {
        var content = File.ReadAllText(SecurityRegressionGuardrailsPath);
        Assert.Contains("api-endpoint-protection-inventory.json", content, StringComparison.Ordinal);
    }

    private static string PilotRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-endpoint-pilot-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string SecurityRegressionGuardrailsPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");
}
