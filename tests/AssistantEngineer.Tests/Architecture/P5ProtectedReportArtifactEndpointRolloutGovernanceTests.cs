using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProtectedReportArtifactEndpointRolloutGovernanceTests
{
    [Fact]
    public void ProtectedReportArtifactRolloutDocumentExistsAndContainsRequiredSections()
    {
        Assert.True(File.Exists(ProtectedReportArtifactRolloutDocumentPath), $"Missing protected-report-artifact rollout document: {ProtectedReportArtifactRolloutDocumentPath}");

        var content = File.ReadAllText(ProtectedReportArtifactRolloutDocumentPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Selected endpoint groups",
            "## Report read policy",
            "## Report generate/export policy",
            "## Artifact read/write policy",
            "## Resource scope resolution",
            "## Compatibility defaults",
            "## Tenant mismatch behavior",
            "## Rate limiting relationship",
            "## Audit/observability behavior",
            "## Test matrix",
            "## Public artifact endpoint status",
            "## What remains unprotected",
            "## Next rollout candidates"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProtectedReportArtifactRolloutDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProtectedReportArtifactRolloutDocumentPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No claim that all API endpoints are protected yet",
            "No claim that artifact ownership is fully enforced unless descriptor scope exists"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EndpointInventoryContainsP5_13ReportEntries_WhenReportControllersExist()
    {
        var reportControllerPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "Reports",
            "BuildingCoolingReportsController.cs");

        if (!File.Exists(reportControllerPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint =>
            string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-13", StringComparison.Ordinal) &&
            (
                (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("ReportsRead", StringComparison.Ordinal) ||
                (endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty).Contains("ReportsWrite", StringComparison.Ordinal)
            ));
    }

    [Fact]
    public void ArtifactEndpointStatusIsDocumented()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var hasArtifactEntries = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Any(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-13", StringComparison.Ordinal) &&
                (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("artifacts", StringComparison.OrdinalIgnoreCase));

        var doc = File.ReadAllText(ProtectedReportArtifactRolloutDocumentPath);
        var deferredStatementPresent =
            doc.Contains("write/delete endpoints are not currently exposed", StringComparison.OrdinalIgnoreCase) ||
            doc.Contains("deferred", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasArtifactEntries || deferredStatementPresent, "Artifact endpoint status must be documented either via inventory entries or explicit deferred statement.");
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
    public void AuthorizationPolicyRolloutReferencesProtectedReportArtifactDocument()
    {
        var content = File.ReadAllText(AuthorizationPolicyRolloutPath);
        Assert.Contains("protected-report-artifact-endpoints-rollout.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSaasInventoryContainsP5_13RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P5-13", roadmapItems);
    }

    [Fact]
    public void P5_13EntriesAreNotUnknownNeedsAudit()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var p513Endpoints = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint => string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-13", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(p513Endpoints);
        Assert.All(p513Endpoints, endpoint =>
        {
            var status = endpoint.GetProperty("currentAuthStatus").GetString();
            Assert.False(string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void WorkflowHistoryEndpointsAreNotIncorrectlyMarkedAsP5_13()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(EndpointInventoryJsonPath));
        var invalid = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Where(endpoint =>
                string.Equals(endpoint.GetProperty("rolloutStage").GetString(), "P5-13", StringComparison.Ordinal) &&
                (endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("/jobs", StringComparison.OrdinalIgnoreCase) &&
                !(endpoint.GetProperty("routePattern").GetString() ?? string.Empty).Contains("artifacts", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(invalid.Length == 0, "Workflow history/job routes should not be marked as P5-13 unless explicitly selected.");
    }

    private static string ProtectedReportArtifactRolloutDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "protected-report-artifact-endpoints-rollout.md");

    private static string EndpointInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string EndpointInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
