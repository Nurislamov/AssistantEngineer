using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P8RouteInventoryClassificationClosureTests
{
    [Fact]
    public void ClosureArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ClosureMarkdownPath,
            ClosureJsonPath,
            ClosureSchemaPath);
    }

    [Fact]
    public void ClosureJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "publicApiChanged",
                "authorizationSemanticsChanged",
                "routeBehaviorChanged"
            ]);
    }

    [Fact]
    public void ClosureJsonContainsCountsAndClassificationChanges()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("classificationCountsBefore", out var before));
        Assert.True(root.TryGetProperty("classificationCountsAfter", out var after));
        Assert.True(before.TryGetProperty("total", out _));
        Assert.True(after.TryGetProperty("total", out _));

        var entriesReclassified = root.GetProperty("entriesReclassified").EnumerateArray().ToArray();
        var deferredRetained = root.GetProperty("deferredEntriesRetained").EnumerateArray().ToArray();
        Assert.True(entriesReclassified.Length > 0 || deferredRetained.Length > 0);
    }

    [Fact]
    public void ClosureJsonContainsRequiredNonClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(ClosureJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No public API route change claim",
                "No authorization behavior change claim",
                "No full tenant isolation claim"
            ]);
    }

    [Fact]
    public void P8AuditMarksRouteInventoryFindingAsAddressedOrPartiallyAddressed()
    {
        using var audit = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-domain-architecture-audit.json"));

        var finding = audit.RootElement.GetProperty("findings")
            .EnumerateArray()
            .Single(item => string.Equals(item.GetProperty("id").GetString(), "P8-00-F05", StringComparison.Ordinal));

        Assert.Contains(
            finding.GetProperty("resolutionStatus").GetString(),
            new[] { "Addressed", "PartiallyAddressed", "InProgress" });
        Assert.Equal("P8-05", finding.GetProperty("resolutionStage").GetString());
    }

    private static string ClosureMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "route-inventory-classification-closure.md");

    private static string ClosureJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "route-inventory-classification-closure.json");

    private static string ClosureSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "route-inventory-classification-closure.schema.json");
}
