using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016SolarChainVerificationEvidenceTests
{
    [Fact]
    public void VerificationScriptIncludesCriticalSolarChainGuardTests()
    {
        var script = ReadRepoFile(
            "scripts",
            "engineering-core",
            "verify-iso52016-solar-chain.ps1");

        var requiredTestClasses = new[]
        {
            "Iso52016WeatherSolarContextPerezDiagnosticsTests",
            "Iso52016WeatherSolarWindowGainIntegrationTests",
            "Iso52016HourlyHeatBalanceSolarContextIntegrationTests",
            "Iso52016HourlySteadyStateWeatherSolarContextIntegrationTests",
            "Iso52016AnnualDiagnosticsVisibilityTests",
            "Iso52016ResponseDiagnosticsVisibilityTests",
            "EngineeringCoreDiagnosticsFrontendRenderingTests",
            "Iso52016ProductionSolarPathRegistrationTests",
            "Iso52016ProductionSolarRuntimeSmokeTests",
            "Iso52016ApiDiagnosticsContractEvidenceTests"
        };

        foreach (var testClass in requiredTestClasses)
        {
            Assert.Contains(testClass, script, StringComparison.Ordinal);
        }

        Assert.Contains("dotnet build", script, StringComparison.Ordinal);
        Assert.Contains("dotnet test", script, StringComparison.Ordinal);
        Assert.Contains("RunAllTests", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestDocumentsClosedStagesAndRequiredDiagnostics()
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
        Assert.Equal("closed-internal-engineering-gate", root.GetProperty("status").GetString());

        var stages = root.GetProperty("closedStages");
        Assert.True(stages.GetArrayLength() >= 11);

        var requiredCodes = root.GetProperty("requiredDiagnosticCodes")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Iso52016.WeatherSolarContextUsed", requiredCodes);
        Assert.Contains("Iso52016.SolarGainComponentPathUsed", requiredCodes);
        Assert.Contains("Iso52016.PerezAnisotropicModelVisibleInAnnualResult", requiredCodes);
        Assert.Contains("Iso52016.MatrixSolarRadiationFallbackUsed", requiredCodes);
        Assert.Contains("SolarWeather.PerezAnisotropicModelUsed", requiredCodes);
        Assert.Contains("SolarWeather.PerezSkyState", requiredCodes);
    }

    [Fact]
    public void VerificationDocumentationStatesNonClaims()
    {
        var documentation = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52016SolarChainVerification.md");

        Assert.Contains("No exact EnergyPlus numerical equivalence", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact StandardReference numerical equivalence", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No full ISO 52016 node/matrix solver equivalence", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("verify-iso52016-solar-chain.ps1", documentation, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestAndVerificationDocsReferenceSameVerificationScript()
    {
        var manifest = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52016SolarChainManifest.json");

        var documentation = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52016SolarChainVerification.md");

        Assert.Contains("scripts/engineering-core/verify-iso52016-solar-chain.ps1", manifest, StringComparison.Ordinal);
        Assert.Contains("verify-iso52016-solar-chain.ps1", documentation, StringComparison.Ordinal);
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

