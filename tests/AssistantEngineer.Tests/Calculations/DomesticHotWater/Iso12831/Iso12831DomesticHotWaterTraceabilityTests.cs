using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresStageAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso12831DomesticHotWaterDemandStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-DHW-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("ISO12831-3-inspired domestic hot water engineering calculator.", claimBoundary);
        Assert.Contains("No full ISO 12831-3 compliance claim.", claimBoundary);
        Assert.Contains("No pyBuildingEnergy parity claim.", claimBoundary);
        Assert.Contains("No EnergyPlus parity claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 validation claim.", claimBoundary);
        Assert.Contains("No external certification claim.", claimBoundary);
        Assert.DoesNotContain("ExternalParityCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocsAndFixtures_ExistAndDoNotContainPositiveParityClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "domestic-hot-water",
            "Iso12831DomesticHotWaterDemandCalculator.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        var docText = File.ReadAllText(docPath);
        Assert.DoesNotContain("ISO 12831-3 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalParityCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 12831-3 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "pyBuildingEnergy parity");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus parity");

        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "residential-people-based-basic.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "office-area-based-daytime.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "hotel-unit-based-morning-evening.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "custom-volume-flat-profile.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "zero-occupants-zero-demand.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ExternalParityCovered", text, StringComparison.OrdinalIgnoreCase);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "pyBuildingEnergy parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO 12831-3 compliance");
        }
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("must not", StringComparison.OrdinalIgnoreCase),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
