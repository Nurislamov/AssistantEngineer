namespace AssistantEngineer.Tests.Architecture;

public sealed class ThermalZoneBoundaryCalculatorHotspotP4GuardTests
{
    [Fact]
    public void ThermalZoneBoundaryCalculator_RemainsFacadeSizedAfterHelperExtraction()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Topology",
            "ThermalZoneBoundaryCalculator.cs");

        Assert.True(File.Exists(path), $"ThermalZoneBoundaryCalculator file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 300,
            $"ThermalZoneBoundaryCalculator must stay as a focused facade after P4 hotspot decomposition. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 300.");
    }

    [Fact]
    public void ThermalZoneBoundaryCalculator_UsesExtractedFocusedHelpers()
    {
        var root = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Topology");

        var calculatorPath = Path.Combine(root, "ThermalZoneBoundaryCalculator.cs");
        var calculatorSource = File.ReadAllText(calculatorPath);

        Assert.True(File.Exists(Path.Combine(root, "ThermalZoneBoundaryClassifier.cs")));
        Assert.True(File.Exists(Path.Combine(root, "ThermalZoneAdjacentBoundaryResolver.cs")));
        Assert.True(File.Exists(Path.Combine(root, "ThermalZoneBoundaryDiagnosticsBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(root, "ThermalZoneBoundaryAggregation.cs")));
        Assert.True(File.Exists(Path.Combine(root, "ThermalZoneBoundaryResultAssembler.cs")));

        Assert.Contains("ThermalZoneBoundaryClassifier.Classify", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("ThermalZoneBoundaryResultAssembler.BuildRoomResults", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("ThermalZoneBoundaryResultAssembler.BuildZoneResults", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("ThermalZoneBoundaryAggregation.AggregateBuildingTotals", calculatorSource, StringComparison.Ordinal);
    }
}
