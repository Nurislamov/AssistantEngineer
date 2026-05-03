using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016ApiDiagnosticsContractEvidenceTests
{
    [Fact]
    public void Iso52016ApiDiagnosticsSampleDocumentsRequiredSolarPathCodes()
    {
        var samplePath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v1",
            "iso52016-diagnostics.sample.json");

        Assert.True(File.Exists(samplePath), $"Missing API sample: {samplePath}");

        var json = File.ReadAllText(samplePath);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.True(root.TryGetProperty("diagnostics", out var diagnostics));
        Assert.Equal(JsonValueKind.Array, diagnostics.ValueKind);

        Assert.Contains("Iso52016.WeatherSolarContextUsed", json, StringComparison.Ordinal);
        Assert.Contains("Iso52016.SolarGainComponentPathUsed", json, StringComparison.Ordinal);
        Assert.Contains("Iso52016.PerezAnisotropicModelVisibleInAnnualResult", json, StringComparison.Ordinal);
        Assert.Contains("severity", json, StringComparison.Ordinal);
        Assert.Contains("message", json, StringComparison.Ordinal);
        Assert.Contains("context", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Iso52016ApiContractDocumentationExplainsLegacyFallbackWarning()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v1",
            "ISO52016DiagnosticsContract.md");

        Assert.True(File.Exists(docPath), $"Missing API contract doc: {docPath}");

        var content = File.ReadAllText(docPath);

        Assert.Contains("Iso52016.WeatherSolarContextUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.SolarGainComponentPathUsed", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.PerezAnisotropicModelVisibleInAnnualResult", content, StringComparison.Ordinal);
        Assert.Contains("Iso52016.LegacySolarRadiationFallbackUsed", content, StringComparison.Ordinal);
        Assert.Contains("should be shown as a warning", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must not infer the solar calculation path only from `solarGainsKWh`", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Iso52016DiagnosticsContractIsConnectedFromBackendToFrontendEvidence()
    {
        var annualResult = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Iso52016AnnualEnergyNeedResult.cs");

        var hourlyResponse = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Iso52016HourlyResultsResponse.cs");

        var monthlyResponse = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Contracts",
            "Iso52016",
            "Iso52016MonthlyResultsResponse.cs");

        var performanceService = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Performance",
            "BuildingPerformanceService.cs");

        var frontendPanel = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-disclosure",
            "ui",
            "EngineeringCoreDisclosurePanel.tsx");

        Assert.Contains("Diagnostics", annualResult, StringComparison.Ordinal);
        Assert.Contains("Diagnostics", hourlyResponse, StringComparison.Ordinal);
        Assert.Contains("Diagnostics", monthlyResponse, StringComparison.Ordinal);
        Assert.Contains("Diagnostics: energyNeed.Value.Diagnostics", performanceService, StringComparison.Ordinal);
        Assert.Contains("Calculation diagnostics", frontendPanel, StringComparison.Ordinal);
        Assert.Contains("Iso52016.LegacySolarRadiationFallbackUsed", frontendPanel, StringComparison.Ordinal);
    }

    [Fact]
    public void Iso52016SolarDiagnosticsEndToEndEvidenceDocumentsClosedChain()
    {
        var evidence = ReadRepoFile(
            "docs",
            "calculations",
            "ISO52016SolarDiagnosticsEndToEndEvidence.md");

        Assert.Contains("Production DI registers Perez anisotropic surface irradiance by default", evidence, StringComparison.Ordinal);
        Assert.Contains("Annual result exposes diagnostics", evidence, StringComparison.Ordinal);
        Assert.Contains("Hourly/monthly response DTOs expose diagnostics", evidence, StringComparison.Ordinal);
        Assert.Contains("Frontend disclosure panel renders diagnostics", evidence, StringComparison.Ordinal);
        Assert.Contains("API consumer documentation includes a diagnostics sample", evidence, StringComparison.Ordinal);
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
