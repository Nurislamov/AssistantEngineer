using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringCorporateStatusSampleTests
{
    [Fact]
    public void CorporateStatusSamples_ExistAndContainExpectedClaims()
    {
        var readinessSamplePath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v2",
            "engineering-release-readiness.sample.json");
        var statusSamplePath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v2",
            "status.sample.json");

        Assert.True(File.Exists(readinessSamplePath));
        Assert.True(File.Exists(statusSamplePath));

        using var readinessDocument = JsonDocument.Parse(File.ReadAllText(readinessSamplePath));
        var readinessRoot = readinessDocument.RootElement;

        Assert.NotEqual("ExternallyCertified", readinessRoot.GetProperty("status").GetString());
        Assert.True(readinessRoot.GetProperty("claimBoundary").GetArrayLength() > 0);

        var limitations = readinessRoot.GetProperty("knownLimitations")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        Assert.Contains("No EnergyPlus parity claim.", limitations);
        Assert.Contains("No pyBuildingEnergy parity claim.", limitations);
        Assert.Contains("No ASHRAE 140 validation claim.", limitations);

        var optInFlags = readinessRoot.GetProperty("optInFlags").EnumerateArray().ToArray();
        Assert.NotEmpty(optInFlags);
        Assert.All(optInFlags, flag => Assert.False(flag.GetProperty("defaultValue").GetBoolean()));
    }
}
