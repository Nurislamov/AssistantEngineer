namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationDocumentationGuardTests
{
    [Fact]
    public void NaturalVentilationDoc_ContainsRequiredNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "natural-ventilation.md");

        Assert.True(File.Exists(path), $"Natural ventilation document was not found: {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("This is not a full ISO52016 compliance claim.", text, StringComparison.Ordinal);
        Assert.Contains("does not claim one-to-one equivalence with external third-party tools", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not claim external-engine numerical identity", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internal engineering calculation implementation", text, StringComparison.OrdinalIgnoreCase);
    }
}
