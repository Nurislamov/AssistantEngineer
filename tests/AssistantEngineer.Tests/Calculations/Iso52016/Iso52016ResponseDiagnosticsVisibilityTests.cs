using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016ResponseDiagnosticsVisibilityTests
{
    [Fact]
    public void HourlyResultsResponseCarriesDiagnostics()
    {
        var diagnostics = new[]
        {
            new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "Iso52016.WeatherSolarContextUsed",
                "Weather-solar context was used.",
                "test")
        };

        var response = new Iso52016HourlyResultsResponse(
            BuildingId: 1,
            BuildingName: "Building",
            Year: 2026,
            MonthFilter: null,
            HourCount: 0,
            CalculationTimeStep: Iso52016CalculationTimeStepDto.Hourly,
            HourlyResults: [],
            Diagnostics: diagnostics);

        Assert.NotNull(response.Diagnostics);
        Assert.Contains(response.Diagnostics!, diagnostic =>
            diagnostic.Code == "Iso52016.WeatherSolarContextUsed");
    }

    [Fact]
    public void MonthlyResultsResponseCarriesDiagnostics()
    {
        var diagnostics = new[]
        {
            new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Iso52016.LegacySolarRadiationFallbackUsed",
                "Legacy fallback was used.",
                "test")
        };

        var response = new Iso52016MonthlyResultsResponse(
            BuildingId: 1,
            BuildingName: "Building",
            Year: 2026,
            CalculationTimeStep: Iso52016CalculationTimeStepDto.Hourly,
            MonthlyResults: [],
            AnnualHeatingDemandKWh: 0,
            AnnualCoolingDemandKWh: 0,
            Breakdown: new Iso52016EnergyBalanceBreakdown(
                SolarGainsKWh: 0,
                InternalGainsKWh: 0,
                HeatingInputKWh: 0,
                CoolingExtractedKWh: 0),
            Diagnostics: diagnostics);

        Assert.NotNull(response.Diagnostics);
        Assert.Contains(response.Diagnostics!, diagnostic =>
            diagnostic.Code == "Iso52016.LegacySolarRadiationFallbackUsed");
    }

    [Fact]
    public void BuildingPerformanceServiceMapsAnnualDiagnosticsToIso52016Responses()
    {
        var repoRoot = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Performance",
            "BuildingPerformanceService.cs");

        var source = File.ReadAllText(sourcePath);

        Assert.Contains("Diagnostics: energyNeed.Value.Diagnostics", source, StringComparison.Ordinal);
        Assert.Contains("Iso52016HourlyResultsResponse", source, StringComparison.Ordinal);
        Assert.Contains("Iso52016MonthlyResultsResponse", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }
}
