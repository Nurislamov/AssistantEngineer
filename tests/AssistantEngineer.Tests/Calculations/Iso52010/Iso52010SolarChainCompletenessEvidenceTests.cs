using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52010;

public class Iso52010SolarChainCompletenessEvidenceTests
{
    [Fact]
    public void FocusedVerificationIncludesLegacySolarRadiationCleanupGuard()
    {
        var script = ReadRepoFile(
            "scripts",
            "engineering-core",
            "verify-iso52016-solar-chain.ps1");

        Assert.Contains("SolarRadiationServiceLegacyTimeLocationTests", script, StringComparison.Ordinal);
        Assert.Contains("Iso52010SolarChainCompletenessEvidenceTests", script, StringComparison.Ordinal);
        Assert.Contains("Iso52016ProductionSolarRuntimeSmokeTests", script, StringComparison.Ordinal);
        Assert.Contains("Iso52016ApiDiagnosticsContractEvidenceTests", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestMarksSolarChainClosedGuarded100AndIncludesStagesThroughLegacyCleanup()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ISO52016SolarChainManifest.json");

        Assert.True(File.Exists(manifestPath), $"Missing manifest: {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal("AE-ISO52016-SOLAR-CHAIN", root.GetProperty("id").GetString());
        Assert.Equal("closed-guarded-100", root.GetProperty("status").GetString());

        var stages = root.GetProperty("closedStages")
            .EnumerateArray()
            .Select(stage => stage.GetProperty("stage").GetInt32())
            .ToHashSet();

        foreach (var expectedStage in Enumerable.Range(1, 13))
        {
            Assert.Contains(expectedStage, stages);
        }

        var criticalTests = root.GetProperty("criticalVerificationTests")
            .EnumerateArray()
            .Select(test => test.GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SolarRadiationServiceLegacyTimeLocationTests", criticalTests);
        Assert.Contains("Iso52010SolarChainCompletenessEvidenceTests", criticalTests);
    }

    [Fact]
    public void LegacySolarRadiationServiceCleanupIsDocumentedAsCompatibilityPathOnly()
    {
        var doc = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52010LegacySolarRadiationServiceCleanup.md");

        Assert.Contains("The preferred annual path remains `AnnualWeatherSolarProfileBuilder -> Iso52016WeatherSolarContext`", doc, StringComparison.Ordinal);
        Assert.Contains("The old 5-parameter method remains as a compatibility wrapper", doc, StringComparison.Ordinal);
        Assert.Contains("longitude", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timezone", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("year", doc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacySolarRadiationConfiguredFallbackUsesOptionsInsteadOfHiddenZeroLongitudeUtc()
    {
        var heatBalance = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Iso52016HourlyHeatBalanceCalculator.cs");

        Assert.Contains("_options.LongitudeDegrees", heatBalance, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromHours(_options.TimeZoneOffsetHours)", heatBalance, StringComparison.Ordinal);
        Assert.Contains("_options.DefaultWeatherYear", heatBalance, StringComparison.Ordinal);

        var service = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "SolarRadiationService.cs");

        Assert.Contains("CompatibilityFallbackYear", service, StringComparison.Ordinal);
        Assert.Contains("CompatibilityFallbackLongitudeDegrees", service, StringComparison.Ordinal);
        Assert.Contains("LongitudeDegrees: longitudeDegrees", service, StringComparison.Ordinal);
        Assert.Contains("offset: timeZoneOffset", service, StringComparison.Ordinal);
        Assert.Contains("year: normalizedYear", service, StringComparison.Ordinal);
    }

    [Fact]
    public void CompletionEvidenceKeepsNonClaimsExplicit()
    {
        var manifest = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52016SolarChainManifest.json");

        Assert.Contains("No exact EnergyPlus numerical parity claim.", manifest, StringComparison.Ordinal);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", manifest, StringComparison.Ordinal);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", manifest, StringComparison.Ordinal);
        Assert.Contains("No full ISO 52016 node/matrix solver parity claim.", manifest, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(
            parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
