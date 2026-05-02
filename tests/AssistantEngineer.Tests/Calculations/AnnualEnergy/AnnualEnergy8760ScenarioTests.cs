using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Tests.Calculations.AnnualEnergy;

public class AnnualEnergy8760ScenarioTests
{
    [Fact]
    public void Calculate_EndToEndTrueHourly8760ScenarioAggregatesMonthlyAnnualPeakAndEui()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 101,
            BuildingName: "Engineering Core V1 8760 Scenario",
            BuildingAreaM2: 250,
            Year: 2026,
            Hours: CreateSeasonal8760Scenario(),
            EnergyDataSource: "TrueHourlySimulation",
            IsTrueHourly8760: true,
            WeatherSource: "PVGIS TMY normalized 8760",
            ActualMethod: "EngineeringCoreV1.TrueHourly8760"));

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value.HasErrors);

        Assert.Equal("TrueHourlySimulation", result.Value.EnergyDataSource);
        Assert.True(result.Value.IsTrueHourly8760);
        Assert.Equal(8760, result.Value.HourlyRecordCount);
        Assert.Equal("EngineeringCoreV1.TrueHourly8760", result.Value.ActualMethod);

        Assert.Equal(4286.4, result.Value.AnnualHeatingDemandKWh, precision: 6);
        Assert.Equal(2281.2, result.Value.AnnualCoolingDemandKWh, precision: 6);
        Assert.Equal(6567.6, result.Value.AnnualTotalDemandKWh, precision: 6);
        Assert.Equal(26.2704, result.Value.EnergyUseIntensityKWhPerM2Year, precision: 6);

        Assert.Equal(
            result.Value.AnnualHeatingDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.HeatingKWh),
            precision: 6);

        Assert.Equal(
            result.Value.AnnualCoolingDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.CoolingKWh),
            precision: 6);

        Assert.Equal(
            result.Value.AnnualTotalDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.TotalKWh),
            precision: 6);

        AssertMonth(
            result.Value,
            month: 1,
            expectedHeatingKWh: 892.8,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 2,
            expectedHeatingKWh: 672,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 3,
            expectedHeatingKWh: 595.2,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 4,
            expectedHeatingKWh: 288,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 5,
            expectedHeatingKWh: 0,
            expectedCoolingKWh: 223.2);

        AssertMonth(
            result.Value,
            month: 6,
            expectedHeatingKWh: 0,
            expectedCoolingKWh: 432);

        AssertMonth(
            result.Value,
            month: 7,
            expectedHeatingKWh: 0,
            expectedCoolingKWh: 669.6);

        AssertMonth(
            result.Value,
            month: 8,
            expectedHeatingKWh: 0,
            expectedCoolingKWh: 632.4);

        AssertMonth(
            result.Value,
            month: 9,
            expectedHeatingKWh: 0,
            expectedCoolingKWh: 324);

        AssertMonth(
            result.Value,
            month: 10,
            expectedHeatingKWh: 372,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 11,
            expectedHeatingKWh: 648,
            expectedCoolingKWh: 0);

        AssertMonth(
            result.Value,
            month: 12,
            expectedHeatingKWh: 818.4,
            expectedCoolingKWh: 0);

        Assert.Equal(1200, result.Value.PeakHeatingLoadW, precision: 6);
        Assert.Equal(0, result.Value.PeakHeatingHour);

        Assert.Equal(900, result.Value.PeakCoolingLoadW, precision: 6);
        Assert.Equal(4344, result.Value.PeakCoolingHour);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "AnnualEnergy.TrueHourlySimulationUsed");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "SolarWeather.HourlyWeatherSourceUsed");

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.Not8760");

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.MonthlyBalanceAdapter");

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.SyntheticWeather");

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.SyntheticWeatherUsed");
    }

    private static void AssertMonth(
        AnnualEnergyBalanceResult result,
        int month,
        double expectedHeatingKWh,
        double expectedCoolingKWh)
    {
        var monthResult = Assert.Single(
            result.MonthlyResults,
            item => item.Month == month);

        Assert.Equal(expectedHeatingKWh, monthResult.HeatingKWh, precision: 6);
        Assert.Equal(expectedCoolingKWh, monthResult.CoolingKWh, precision: 6);
        Assert.Equal(expectedHeatingKWh + expectedCoolingKWh, monthResult.TotalKWh, precision: 6);
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateSeasonal8760Scenario()
    {
        var result = new List<AnnualEnergyBalanceHourInput>(8760);
        var hourIndex = 0;

        foreach (var (month, hoursInMonth) in MonthHours())
        {
            var heatingW = HeatingLoadForMonth(month);
            var coolingW = CoolingLoadForMonth(month);

            for (var hourInMonth = 0; hourInMonth < hoursInMonth; hourInMonth++)
            {
                result.Add(new AnnualEnergyBalanceHourInput(
                    HourIndex: hourIndex++,
                    Month: month,
                    HeatingLoadW: heatingW,
                    CoolingLoadW: coolingW,
                    TransmissionW: Math.Max(0, heatingW * 0.55 + coolingW * 0.10),
                    VentilationW: Math.Max(0, heatingW * 0.20 + coolingW * 0.20),
                    InfiltrationW: Math.Max(0, heatingW * 0.10 + coolingW * 0.10),
                    SolarGainsW: Math.Max(0, coolingW * 0.35),
                    InternalGainsW: Math.Max(0, coolingW * 0.15),
                    GroundW: Math.Max(0, heatingW * 0.15 + coolingW * 0.10),
                    HourDurationH: 1,
                    TransmissionBalanceW: heatingW > 0
                        ? -heatingW * 0.55
                        : coolingW * 0.10,
                    VentilationBalanceW: heatingW > 0
                        ? -heatingW * 0.20
                        : coolingW * 0.20,
                    InfiltrationBalanceW: heatingW > 0
                        ? -heatingW * 0.10
                        : coolingW * 0.10,
                    GroundBalanceW: heatingW > 0
                        ? -heatingW * 0.15
                        : coolingW * 0.10));
            }
        }

        Assert.Equal(8760, result.Count);
        Assert.Equal(Enumerable.Range(0, 8760), result.Select(hour => hour.HourIndex));

        return result;
    }

    private static double HeatingLoadForMonth(int month) =>
        month switch
        {
            1 => 1200,
            2 => 1000,
            3 => 800,
            4 => 400,
            10 => 500,
            11 => 900,
            12 => 1100,
            _ => 0
        };

    private static double CoolingLoadForMonth(int month) =>
        month switch
        {
            5 => 300,
            6 => 600,
            7 => 900,
            8 => 850,
            9 => 450,
            _ => 0
        };

    private static IEnumerable<(int Month, int Hours)> MonthHours()
    {
        var daysPerMonth = new[]
        {
            31,
            28,
            31,
            30,
            31,
            30,
            31,
            31,
            30,
            31,
            30,
            31
        };

        for (var month = 1; month <= 12; month++)
            yield return (month, daysPerMonth[month - 1] * 24);
    }
}