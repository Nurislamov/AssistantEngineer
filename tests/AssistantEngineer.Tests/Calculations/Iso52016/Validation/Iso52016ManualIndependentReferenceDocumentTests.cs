namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ManualIndependentReferenceDocumentTests
{
    [Theory]
    [InlineData("manual-independent-steady-heating-simple-room.md")]
    [InlineData("manual-independent-steady-cooling-simple-room.md")]
    [InlineData("manual-independent-annual-8760-seasonal-loads.md")]
    public void DerivationDocument_ContainsEquationsAssumptionsToleranceAndNonClaims(string fileName)
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "validation",
            "iso52016",
            fileName);

        Assert.True(File.Exists(path), $"Derivation document was not found: {path}");
        var text = File.ReadAllText(path);

        Assert.Contains("Assumptions", text);
        Assert.Contains("equation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("arithmetic", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expected results", text);
        Assert.Contains("Tolerance rationale", text);
        Assert.Contains("Validation/internal engineering anchors only.", text);
        Assert.Contains("Manual independent reference fixtures only.", text);
        Assert.Contains("No full ISO 52016 equivalence claim.", text);
        Assert.Contains("No StandardReference equivalence claim.", text);
        Assert.Contains("No EnergyPlus comparison workflow claim.", text);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor claim.", text);
        Assert.Contains("ExternalReferenceCovered is not allowed in this stage.", text);
    }
}
