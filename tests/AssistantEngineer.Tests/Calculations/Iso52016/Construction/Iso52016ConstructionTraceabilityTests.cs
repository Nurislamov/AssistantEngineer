using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso52016ConstructionLayerAndMassClassFoundationManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-CONSTRUCTION-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("ISO52016-inspired construction layer and mass class engineering foundation.", claimBoundary);
        Assert.Contains("Compatibility envelope behavior preserved by default.", claimBoundary);
        Assert.Contains("No full ISO 52016 compliance claim.", claimBoundary);
        Assert.Contains("No pyBuildingEnergy parity claim.", claimBoundary);
        Assert.Contains("No EnergyPlus parity claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 validation claim.", claimBoundary);
        Assert.Contains("No external certification claim.", claimBoundary);
    }

    [Fact]
    public void RequiredDocsAndFixtures_ExistAndContainNoPositiveParityClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "iso52016",
            "Iso52016ConstructionLayerAndMassClassFoundation.md");
        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        var docText = File.ReadAllText(docPath);
        Assert.DoesNotContain("ExternalParityCovered", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 52016 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "pyBuildingEnergy parity");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus parity");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "ASHRAE 140 validated");

        var fixtures = Directory.GetFiles(
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "iso52016", "construction"),
            "*.json",
            SearchOption.TopDirectoryOnly);
        Assert.NotEmpty(fixtures);

        foreach (var fixturePath in fixtures)
        {
            var text = File.ReadAllText(fixturePath);
            Assert.DoesNotContain("ExternalParityCovered", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("certified", text, StringComparison.OrdinalIgnoreCase);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO 52016 compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "pyBuildingEnergy parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "ASHRAE 140 validated");
        }
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("forbidden", StringComparison.OrdinalIgnoreCase),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
