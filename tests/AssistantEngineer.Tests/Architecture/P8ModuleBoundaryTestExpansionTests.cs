using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8ModuleBoundaryTestExpansionTests
{
    [Fact]
    public void ModuleBoundaryExpansionArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ExpansionMarkdownPath,
            ExpansionJsonPath,
            ExpansionSchemaPath,
            MatrixMarkdownPath,
            MatrixJsonPath,
            MatrixSchemaPath,
            MatrixAllowlistPath);
    }

    [Fact]
    public void ExpansionJsonParsesAndNoBehaviorFlagsChanged()
    {
        using var document = GovernanceJsonTestHelper.Parse(ExpansionJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);

        var modulesCovered = root.GetProperty("modulesCovered")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Contains("AssistantEngineer.Modules.EngineeringWorkflow", modulesCovered);

        var nonClaims = root.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No calculation physics change claim",
                "No public API route change claim",
                "No DTO shape change claim",
                "No ownership backfill execution claim",
                "No global EF query filter claim",
                "No DB RLS claim",
                "No production security certification claim"
            ]);
    }

    [Fact]
    public void MatrixJsonParsesAndContainsEngineeringWorkflow()
    {
        using var document = GovernanceJsonTestHelper.Parse(MatrixJsonPath);
        var components = document.RootElement.GetProperty("components").EnumerateArray().ToArray();
        Assert.NotEmpty(components);

        Assert.Contains(
            components,
            component => string.Equals(
                component.GetProperty("project").GetString(),
                "AssistantEngineer.Modules.EngineeringWorkflow",
                StringComparison.Ordinal));
    }

    [Fact]
    public void P8AuditMarksModuleBoundaryFindingAsAddressedInP8_02()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .First(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F02", StringComparison.Ordinal));

        Assert.Equal("Addressed", finding.GetProperty("resolutionStatus").GetString());
        Assert.Equal("P8-02", finding.GetProperty("resolutionStage").GetString());
    }

    [Fact]
    public void ExpansionDocsDoNotContainFalseParityOrCertificationClaims()
    {
        var markdown = File.ReadAllText(ExpansionMarkdownPath);
        var json = File.ReadAllText(ExpansionJsonPath);

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

    private static string ExpansionMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-test-expansion.md");

    private static string ExpansionJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-test-expansion.json");

    private static string ExpansionSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-test-expansion.schema.json");

    private static string MatrixMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-matrix.md");

    private static string MatrixJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-matrix.json");

    private static string MatrixSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "module-boundary-matrix.schema.json");

    private static string MatrixAllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "architecture", "module-boundary-allowlist.json");
}
