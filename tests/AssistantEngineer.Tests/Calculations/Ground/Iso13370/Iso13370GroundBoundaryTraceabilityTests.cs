using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresStageStatusAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso13370GroundBoundaryStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-GROUND-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("ISO13370-inspired ground boundary engineering calculator.", claimBoundary);
        Assert.Contains("No full ISO 13370 compliance claim.", claimBoundary);
        Assert.Contains("No pyBuildingEnergy parity claim.", claimBoundary);
        Assert.Contains("No EnergyPlus parity claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 validation claim.", claimBoundary);
        Assert.Contains("No external certification claim.", claimBoundary);
        Assert.DoesNotContain("ExternalParityCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequiredDocsAndFixtures_ExistAndContainNoParityClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ground",
            "Iso13370GroundBoundaryCalculator.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        var docText = File.ReadAllText(docPath);
        Assert.DoesNotContain("ISO 13370 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalParityCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 13370 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "pyBuildingEnergy parity");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus parity");

        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "slab-on-ground-simple.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "conditioned-basement-buried.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "unconditioned-basement-outdoor-coupled.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "ventilated-crawlspace-outdoor-dominant.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ExternalParityCovered", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ASHRAE 140 validated", text, StringComparison.OrdinalIgnoreCase);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "pyBuildingEnergy parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO 13370 compliance");
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
