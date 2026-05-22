using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8TerminologyClaimsVocabularyTests
{
    [Fact]
    public void VocabularyArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            VocabularyMarkdownPath,
            VocabularyJsonPath,
            VocabularySchemaPath);
    }

    [Fact]
    public void VocabularyJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(VocabularyJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);

        Assert.NotEmpty(root.GetProperty("allowedClaims").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("forbiddenClaims").EnumerateArray());
    }

    [Fact]
    public void VocabularyContainsRequiredForbiddenClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(VocabularyJsonPath);
        var forbidden = document.RootElement.GetProperty("forbiddenClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("full pyBuildingEnergy parity", forbidden);
        Assert.Contains("EnergyPlus parity", forbidden);
        Assert.Contains("ASHRAE 140 validated", forbidden);
        Assert.Contains("full tenant isolation", forbidden);
        Assert.Contains("production security certified", forbidden);
        Assert.Contains("production apply enabled", forbidden);
    }

    [Fact]
    public void VocabularyContainsRequiredPreferredTerms()
    {
        using var document = GovernanceJsonTestHelper.Parse(VocabularyJsonPath);
        var preferred = document.RootElement.GetProperty("preferredTerms")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            preferred,
            ["reference-informed", "validation anchors", "write-path intentionally disabled"]);
    }

    private static string VocabularyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-and-claims-vocabulary.md");

    private static string VocabularyJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-and-claims-vocabulary.json");

    private static string VocabularySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-and-claims-vocabulary.schema.json");
}
