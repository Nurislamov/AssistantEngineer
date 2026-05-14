namespace AssistantEngineer.Tests.Architecture;

public sealed class Iso52016HourlyHeatBalanceCalculatorHotspotP4GuardTests
{
    [Fact]
    public void Iso52016HourlyHeatBalanceCalculator_RemainsFacadeSizedForP4Decomposition()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Iso52016HourlyHeatBalanceCalculator.cs");

        Assert.True(File.Exists(path), $"ISO52016 hourly heat balance calculator file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 520,
            $"Iso52016HourlyHeatBalanceCalculator must keep P4 hotspot decomposition progress while preserving numeric kernel behavior. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 520.");
    }

    [Fact]
    public void Iso52016HourlyHeatBalanceCalculator_UsesExtractedNonNumericHelpers()
    {
        var root = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016");

        var calculatorPath = Path.Combine(root, "Iso52016HourlyHeatBalanceCalculator.cs");
        var calculatorSource = File.ReadAllText(calculatorPath);

        Assert.True(File.Exists(Path.Combine(root, "Iso52016HourlyHeatBalanceValidation.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016HourlyHeatBalanceRequestNormalizer.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016HourlyHeatBalanceDiagnosticsBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016HourlyHeatBalanceResultAssembler.cs")));

        Assert.Contains("Iso52016HourlyHeatBalanceValidation.ThrowIfCancelled", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016HourlyHeatBalanceRequestNormalizer.Normalize", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016HourlyHeatBalanceDiagnosticsBuilder.BuildSolarWindowContext", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016HourlyHeatBalanceResultAssembler.BuildRoomResult", calculatorSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016HourlyHeatBalanceResultAssembler.BuildZoneResult", calculatorSource, StringComparison.Ordinal);
    }
}
