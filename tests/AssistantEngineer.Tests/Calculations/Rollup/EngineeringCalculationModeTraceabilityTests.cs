using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Rollup;

public sealed class EngineeringCalculationModeTraceabilityTests
{
    [Fact]
    public void RollupDocsManifestAndStatusSamples_Exist()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "rollup",
            "EngineeringCalculationModeComparisonAndDisclosureRollup.md");
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCalculationModeComparisonRollupManifest.json");
        var samplePath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v1",
            "calculation-mode-rollup.sample.json");

        Assert.True(File.Exists(docPath), $"Rollup document was not found: {docPath}");
        Assert.True(File.Exists(manifestPath), $"Rollup manifest was not found: {manifestPath}");
        Assert.True(File.Exists(samplePath), $"Rollup status sample was not found: {samplePath}");
    }

    [Fact]
    public void RollupManifest_DeclaresExpectedStageAndDependencies()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCalculationModeComparisonRollupManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-CALC-ROLLUP-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-governance-anchor", root.GetProperty("status").GetString());

        var dependencies = root.GetProperty("dependsOn")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("AE-VENT-002", dependencies);
        Assert.Contains("AE-ISO52016-CONSTRUCTION-001", dependencies);
        Assert.Contains("AE-ISO52016-CONSTRUCTION-002", dependencies);
        Assert.Contains("AE-BUI-VALIDATION-001", dependencies);
        Assert.Contains("AE-GROUND-002", dependencies);
        Assert.Contains("AE-DHW-002", dependencies);
        Assert.Contains("AE-EN15316-002", dependencies);
        Assert.Contains("AE-VALIDATION-ISO52016-001", dependencies);
        Assert.Contains("AE-VALIDATION-ISO52016-002", dependencies);
        Assert.Contains("AE-VALIDATION-PYBE-001", dependencies);
    }

    [Fact]
    public void RollupFiles_DoNotContainPositiveForbiddenClaims()
    {
        var paths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "rollup", "EngineeringCalculationModeComparisonAndDisclosureRollup.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCalculationModeComparisonRollupManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "calculation-mode-rollup.sample.json")
        };

        foreach (var path in paths)
        {
            var text = File.ReadAllText(path);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full EN compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "validated against pyBuildingEnergy");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "pyBuildingEnergy parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus parity");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "ASHRAE 140 validated");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "ExternalParityCovered");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "certified");
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
