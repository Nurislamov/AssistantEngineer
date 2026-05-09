namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationStage2FixturePresenceTests
{
    [Fact]
    public void Stage2Fixtures_ArePresentWithNonClaimBoundary()
    {
        var directory = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "ventilation",
            "natural-stage2");

        Assert.True(Directory.Exists(directory), $"Fixture directory was not found: {directory}");

        var requiredFiles = new[]
        {
            "closed-window.json",
            "always-open-wind-driven.json",
            "stack-driven.json",
            "temperature-driven-cooling-assist.json",
            "heating-lockout.json",
            "invalid-opening-topology.json"
        };

        foreach (var file in requiredFiles)
        {
            var path = Path.Combine(directory, file);
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.Contains("Internal engineering deterministic fixture.", text, StringComparison.Ordinal);
            Assert.Contains("No full EN16798 compliance claim.", text, StringComparison.Ordinal);
        }
    }
}
