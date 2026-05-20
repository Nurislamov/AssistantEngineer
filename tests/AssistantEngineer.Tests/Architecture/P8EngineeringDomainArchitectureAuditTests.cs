using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8EngineeringDomainArchitectureAuditTests
{
    [Fact]
    public void AuditArtifactsExist()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            AuditMarkdownPath,
            AuditJsonPath,
            AuditSchemaPath);
    }

    [Fact]
    public void AuditDocumentContainsRequiredSectionsAndNonClaims()
    {
        GovernanceDocumentTestHelper.AssertMarkdownContainsSections(
            AuditMarkdownPath,
            [
                "## Purpose",
                "## Scope",
                "## Non-claims",
                "## Current architecture snapshot",
                "## Module/layer inventory",
                "## SOLID findings",
                "## KISS findings",
                "## DRY findings",
                "## DDD/naming findings",
                "## Legacy/dead code findings",
                "## Scripts/tools findings",
                "## Tests architecture findings",
                "## Engineering calculation findings",
                "## Documentation/claims findings",
                "## Risk summary",
                "## Recommended P8 backlog",
                "## Next steps"
            ]);

        GovernanceDocumentTestHelper.AssertMarkdownContainsPhrases(
            AuditMarkdownPath,
            [
                "No calculation physics change claim.",
                "No full donor-model match claim.",
                "No external simulator match claim.",
                "No external standard-case validation completion claim.",
                "No production security certification claim.",
                "No full tenant isolation claim.",
                "No ownership backfill execution claim.",
                "No DB RLS/global EF query filter claim."
            ]);
    }

    [Fact]
    public void AuditJsonParsesAndFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(AuditJsonPath);
        var root = document.RootElement;

        Assert.False(root.GetProperty("runtimeBehaviorChanged").GetBoolean());
        Assert.False(root.GetProperty("calculationPhysicsChanged").GetBoolean());
        Assert.False(root.GetProperty("publicApiChanged").GetBoolean());

        Assert.NotEmpty(root.GetProperty("recommendedBacklog").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("nonClaims").EnumerateArray());

        var serialized = root.GetRawText();
        Assert.DoesNotContain("production certified", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full tenant isolation complete", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditDocumentDoesNotContainFalseParityOrCertificationClaims()
    {
        var content = File.ReadAllText(AuditMarkdownPath);
        Assert.DoesNotContain("production security certified", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full tenant isolation complete", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecurityInventoryGuardrailsAndIndexContainP8_00References()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = inventory.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P8-00", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var guardrailIds = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ENGINEERING-DOMAIN-ARCHITECTURE-AUDIT", guardrailIds);

        using var index = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-governance-index.json"));
        var paths = index.RootElement.GetProperty("documents")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("docs/architecture/engineering-domain-architecture-audit.md", paths);
        Assert.Contains("docs/architecture/assistantengineer-architecture-map.md", paths);
        Assert.Contains("docs/architecture/legacy-and-dead-code-inventory.md", paths);
        Assert.Contains("docs/architecture/scripts-tools-inventory.md", paths);
    }

    private static string AuditMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.md");

    private static string AuditJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json");

    private static string AuditSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.schema.json");
}
