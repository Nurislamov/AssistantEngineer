using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9ValidationFixtureExpectedValuesUnchangedTests
{
    [Fact]
    public void P9_03GovernanceFlagsKeepExpectedValuesUnchanged()
    {
        using var provenance = GovernanceJsonTestHelper.Parse(ProvenanceInventoryJsonPath);
        var provenanceRoot = provenance.RootElement;

        Assert.True(provenanceRoot.GetProperty("expectedValuesChanged").ValueKind == System.Text.Json.JsonValueKind.False);
        foreach (var entry in provenanceRoot.GetProperty("entries").EnumerateArray())
            Assert.True(entry.GetProperty("expectedValuesChangedInP903").ValueKind == System.Text.Json.JsonValueKind.False);

        using var model = GovernanceJsonTestHelper.Parse(ProvenanceModelJsonPath);
        Assert.True(model.RootElement.GetProperty("expectedValuesChanged").ValueKind == System.Text.Json.JsonValueKind.False);
    }

    [Fact]
    public void RoadmapAndEvidenceInventoryRemainNoChangeForPhysicsAndRuntime()
    {
        using var roadmap = GovernanceJsonTestHelper.Parse(RoadmapJsonPath);
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            roadmap.RootElement,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged", "validationClaimChanged"]);

        using var evidence = GovernanceJsonTestHelper.Parse(EvidenceInventoryJsonPath);
        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            evidence.RootElement,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);
    }

    private static string ProvenanceModelJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.json");

    private static string ProvenanceInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json");

    private static string RoadmapJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.json");

    private static string EvidenceInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.json");
}
