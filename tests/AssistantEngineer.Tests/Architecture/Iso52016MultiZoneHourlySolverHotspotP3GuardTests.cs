namespace AssistantEngineer.Tests.Architecture;

public sealed class Iso52016MultiZoneHourlySolverHotspotP3GuardTests
{
    [Fact]
    public void Iso52016MultiZoneHourlySolver_RemainsFacadeSizedAfterP3Phase12()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "MultiZone",
            "Iso52016MultiZoneHourlySolver.cs");

        Assert.True(File.Exists(path), $"Multi-zone hourly solver file was not found: {path}");

        var nonBlankLines = File.ReadAllLines(path).Count(line => !string.IsNullOrWhiteSpace(line));
        Assert.True(
            nonBlankLines <= 220,
            $"Iso52016MultiZoneHourlySolver must remain a focused orchestration facade after P3-12. " +
            $"Current non-blank line count: {nonBlankLines}; expected <= 220.");
    }

    [Fact]
    public void Iso52016MultiZoneHourlySolver_UsesExtractedFocusedComponents()
    {
        var root = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "MultiZone");

        var solverPath = Path.Combine(root, "Iso52016MultiZoneHourlySolver.cs");
        var solverSource = File.ReadAllText(solverPath);

        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneHourlySimulationLoop.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneBoundaryResolver.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneLinearSystem.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneHvacController.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneCouplingBuilder.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneResultAggregator.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Iso52016MultiZoneSolverDiagnostics.cs")));

        Assert.Contains("Iso52016MultiZoneHourlySimulationLoop.TryRun", solverSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016MultiZoneResultAggregator.BuildMonthlySummaries", solverSource, StringComparison.Ordinal);
        Assert.Contains("Iso52016MultiZoneSolverDiagnostics.CreateInfo", solverSource, StringComparison.Ordinal);
    }
}
