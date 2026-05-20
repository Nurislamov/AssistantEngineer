using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P6OwnershipBackfillEvidenceGateGovernanceTests
{
    [Fact]
    public void EvidenceGateSourceAndDocsExist()
    {
        Assert.True(File.Exists(EvidenceGateEvaluatorPath), $"Missing gate evaluator: {EvidenceGateEvaluatorPath}");
        Assert.True(File.Exists(EvidenceValidationDocPath), $"Missing evidence gate doc: {EvidenceValidationDocPath}");
    }

    [Fact]
    public void ToolSourceContainsNoSaveChangesAndNoDestructiveSql()
    {
        GovernanceSourceScanHelper.AssertNoWritePatterns(ToolDirectoryPath);
    }

    [Fact]
    public void ValidateEvidenceCommandIsDocumented()
    {
        var content = File.ReadAllText(EvidenceValidationDocPath);
        Assert.Contains("validate-evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exit codes", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratedGateArtifactsAreIgnored()
    {
        var gitignore = File.ReadAllText(GitIgnorePath);

        Assert.Contains("artifacts/ownership-backfill/", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-evidence-gate-result-*.json", gitignore, StringComparison.Ordinal);
        Assert.Contains("ownership-backfill-evidence-gate-result-*.md", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void StrategyAndEvidenceDocsReferenceEvidenceGates()
    {
        var strategy = File.ReadAllText(StrategyDocPath);
        var evidenceModel = File.ReadAllText(EvidenceModelDocPath);

        Assert.Contains("P6-03", strategy, StringComparison.Ordinal);
        Assert.Contains("evidence gates", strategy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ownership-backfill-evidence-gate-result", evidenceModel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionInventoryContainsP6_03RoadmapItem()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var roadmapItems = document.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("P6-03", roadmapItems);
    }

    [Fact]
    public void GuardrailsContainP6_03EvidenceGateGuard()
    {
        var markdown = File.ReadAllText(GuardrailsMarkdownPath);
        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-EVIDENCE-GATES", markdown, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsJsonPath));
        var guardIds = document.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SEC-GUARD-OWNERSHIP-BACKFILL-EVIDENCE-GATES", guardIds);
    }

    [Fact]
    public void DocsDoNotClaimBackfillExecutedOrGlobalFiltersOrRlsOrFullIsolation()
    {
        GovernanceAssertions.AssertNoFalseSecurityClaims(
            [
                EvidenceValidationDocPath,
                StrategyDocPath,
                EvidenceModelDocPath,
                InventoryMarkdownPath
            ],
            [
                "ownership backfill has been executed",
                "full tenant isolation is implemented",
                "global ef query filters are enabled",
                "database row-level security is enabled"
            ]);
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string EvidenceGateEvaluatorPath =>
        Path.Combine(ToolDirectoryPath, "Gates", "OwnershipBackfillEvidenceGateEvaluator.cs");

    private static string EvidenceValidationDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-evidence-validation-gates.md");

    private static string StrategyDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-strategy.md");

    private static string EvidenceModelDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "ownership-backfill-evidence-model.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GuardrailsJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GitIgnorePath =>
        Path.Combine(TestPaths.RepoRoot, ".gitignore");
}
