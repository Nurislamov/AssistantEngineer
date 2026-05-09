namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationClaimBoundaryGuardTests
{
    [Fact]
    public void NaturalVentilationDocs_KeepNonClaimBoundary()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ventilation",
            "Iso16798NaturalVentilationCalculator.md");

        var text = File.ReadAllText(docPath);

        Assert.Contains("No full EN16798 compliance claim.", text);
        Assert.Contains("No external validation claim.", text);
    }

    [Fact]
    public void NaturalVentilationFixtures_KeepInternalAnalyticalAnchorBoundary()
    {
        var fixtureDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "ventilation",
            "natural");

        foreach (var fixturePath in Directory.GetFiles(fixtureDirectory, "*.json"))
        {
            var text = File.ReadAllText(fixturePath);

            Assert.Contains("Internal analytical anchor only.", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No full EN16798 compliance claim.", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No external validation claim.", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
