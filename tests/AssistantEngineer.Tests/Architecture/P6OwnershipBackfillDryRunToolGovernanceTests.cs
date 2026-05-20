using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillDryRunToolGovernanceTests
{
    [Fact]
    public void ToolProjectExistsAndIsInSolution()
    {
        Assert.True(File.Exists(ToolProjectPath), $"Missing tool project: {ToolProjectPath}");

        var solutionContent = File.ReadAllText(SolutionPath);
        Assert.Contains(@"tools\AssistantEngineer.Tools.OwnershipBackfill\AssistantEngineer.Tools.OwnershipBackfill.csproj", solutionContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToolSourceContainsNoApplyImplementationOrWritePaths()
    {
        GovernanceSourceScanHelper.AssertNoWritePatterns(ToolDirectoryPath);
    }

    [Fact]
    public void EvidenceArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.Contains("artifacts/ownership-backfill/", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-dry-run-summary-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-dry-run-summary-*.md", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-unresolved-records-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-previous-values-*.json", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void StrategyAndEvidenceDocsReferenceP6_01Tool()
    {
        var strategy = File.ReadAllText(StrategyMarkdownPath);
        var evidence = File.ReadAllText(EvidenceModelMarkdownPath);

        Assert.Contains("P6-01", strategy, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-dry-run-tool.md", strategy, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-dry-run-tool.md", evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionInventoryContainsP6_01RoadmapItem()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProductionInventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-01", roadmapItems);
    }

    [Fact]
    public void GuardrailsReferenceP6_01NoWriteGuard()
    {
        var markdown = File.ReadAllText(SecurityGuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-DRY-RUN-TOOL", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(SecurityGuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-DRY-RUN-TOOL", guardIds);
    }

    [Fact]
    public void DocsDoNotClaimBackfillExecutedOrFullIsolationOrGlobalFiltersOrDbRls()
    {
        GovernanceAssertions.AssertNoFalseSecurityClaims(
            [
            StrategyMarkdownPath,
            EvidenceModelMarkdownPath,
            DryRunToolMarkdownPath,
            ProductionInventoryMarkdownPath
            ],
            [
                "ownership backfill has been executed",
                "ownership backfill has been completed",
                "ownership backfill is fully completed",
                "global ef query filters are enabled",
                "database row-level security is enabled",
                "full tenant isolation is implemented"
            ]);
    }

    private static string SolutionPath =>
        Path.Combine(TestPaths.RepoRoot, "AssistantEngineer.sln");

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string ToolProjectPath =>
        Path.Combine(ToolDirectoryPath, "AssistantEngineer.Tools.OwnershipBackfill.csproj");

    private static string StrategyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string EvidenceModelMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-evidence-model.md");

    private static string DryRunToolMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-dry-run-tool.md");

    private static string ProductionInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProductionInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string SecurityGuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string SecurityGuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
