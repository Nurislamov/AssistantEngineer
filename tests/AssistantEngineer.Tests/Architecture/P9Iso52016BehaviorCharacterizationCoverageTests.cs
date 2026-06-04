using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016BehaviorCharacterizationCoverageTests
{
    [Fact]
    public void CoverageLevelsAreAllowedAndEachComponentHasClassification()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "FocusedCharacterization",
            "BroadIntegrationCharacterization",
            "InternalInvariant",
            "GovernanceOnly",
            "Missing"
        };

        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var coverage = document.RootElement.GetProperty("componentCoverage").EnumerateArray().ToArray();
        Assert.NotEmpty(coverage);

        foreach (var item in coverage)
        {
            var level = item.GetProperty("coverageLevel").GetString() ?? string.Empty;
            Assert.Contains(level, allowed);
        }
    }

    [Fact]
    public void CoverageEntriesContainExplicitFollowUpOrGapNotes()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var coverage = document.RootElement.GetProperty("componentCoverage").EnumerateArray().ToArray();

        foreach (var item in coverage)
        {
            var notes = item.GetProperty("gapNotes").GetString() ?? string.Empty;
            var followUp = item.GetProperty("proposedFollowUpStage").GetString() ?? string.Empty;

            Assert.False(string.IsNullOrWhiteSpace(notes));
            Assert.False(string.IsNullOrWhiteSpace(followUp));
        }
    }

    [Fact]
    public void NoFormalValidationOrParityClaimsArePresentInInventory()
    {
        var scanner = new AssistantEngineer.Modules.Calculations.Application.Services.Governance.EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json")
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void InventoryIsLinkedFromComponentMapAndDecompositionReview()
    {
        var componentMapMarkdown = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.md"));
        Assert.Contains("iso52016-behavior-characterization-inventory", componentMapMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iso52016-matrix-solver-seam-design", componentMapMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iso52016-matrix-solver-characterization-hardening", componentMapMarkdown, StringComparison.OrdinalIgnoreCase);

        var decompositionMarkdown = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.md"));
        Assert.Contains("P9-01A", decompositionMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P9-01B", decompositionMarkdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P9-01B1", decompositionMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json");
}
