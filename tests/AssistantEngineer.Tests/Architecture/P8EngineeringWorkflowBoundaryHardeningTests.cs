using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8EngineeringWorkflowBoundaryHardeningTests
{
    [Fact]
    public void BoundaryHardeningArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            HardeningMarkdownPath,
            HardeningJsonPath,
            HardeningSchemaPath);
    }

    [Fact]
    public void BoundaryHardeningJsonParsesAndFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(HardeningJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());

        var nonClaims = root.GetProperty("nonClaims").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, value => value.Contains("No calculation physics change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No public API route change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DTO shape change claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No ownership backfill execution claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No global EF query filter claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No DB RLS claim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, value => value.Contains("No production security certification claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EngineeringDomainAuditMarksP8_00NamespaceLeakAsAddressed()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F01", StringComparison.Ordinal));

        var resolutionStatus = finding.GetProperty("resolutionStatus").GetString();
        var resolutionStage = finding.GetProperty("resolutionStage").GetString();

        Assert.Contains(resolutionStatus, new[] { "Addressed", "PartiallyAddressed" });
        Assert.Equal("P8-01", resolutionStage);
    }

    [Fact]
    public void BoundaryHardeningDocsDoNotContainFalseParityOrCertificationClaims()
    {
        var markdown = File.ReadAllText(HardeningMarkdownPath);
        var json = File.ReadAllText(HardeningJsonPath);

        var forbiddenPhrases = new[]
        {
            "full tenant isolation complete",
            "production security certified",
            "write path enabled in production"
        };

        foreach (var phrase in forbiddenPhrases)
        {
            Assert.DoesNotContain(phrase, markdown, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(phrase, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string HardeningMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineeringworkflow-boundary-hardening.md");

    private static string HardeningJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineeringworkflow-boundary-hardening.json");

    private static string HardeningSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineeringworkflow-boundary-hardening.schema.json");
}
