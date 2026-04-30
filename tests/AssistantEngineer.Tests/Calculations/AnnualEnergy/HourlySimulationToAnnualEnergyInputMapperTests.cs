using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Tests.Calculations.AnnualEnergy;

public class HourlySimulationToAnnualEnergyInputMapperTests
{
    [Fact]
    public void Map_Maps8760RecordsAndPreservesHourCount()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateHours(heatingW: 100, coolingW: 50, includeComponents: true);

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        Assert.Equal("TrueHourlySimulation", result.Input.EnergyDataSource);
        Assert.True(result.IsTrueHourly8760);
        Assert.Equal(8760, result.HourlyRecordCount);
        Assert.Equal(8760, result.Input.Hours.Count);
        Assert.Equal(0, result.Input.Hours[0].HourIndex);
        Assert.Equal(8759, result.Input.Hours[^1].HourIndex);
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial");
    }

    [Fact]
    public void Map_MissingOptionalComponentBreakdownProducesDiagnostics()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateHours(heatingW: 100, coolingW: 50, includeComponents: false);

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("transmission", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("ventilation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_DoesNotWarnForAvailableComponentsAndStillReportsMissingInfiltration()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateHoursWithAvailableComponentsExceptInfiltration();

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("transmission", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("ventilation", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("ground", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("infiltration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_OutputAggregatesInAnnualEnergyEngine()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateHours(heatingW: 1000, coolingW: 500, includeComponents: true);
        var mapping = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records);

        var annual = new AnnualEnergyBalanceEngine().Calculate(mapping.Input);

        Assert.True(annual.IsSuccess, annual.Error);
        Assert.Equal(8760, annual.Value.AnnualHeatingDemandKWh, precision: 6);
        Assert.Equal(4380, annual.Value.AnnualCoolingDemandKWh, precision: 6);
        Assert.Equal(
            annual.Value.AnnualTotalDemandKWh,
            annual.Value.MonthlyResults.Sum(month => month.TotalKWh),
            precision: 6);
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateHours(
        double heatingW,
        double coolingW,
        bool includeComponents)
    {
        var records = new List<AnnualEnergyBalanceHourInput>(8760);
        var hour = 0;
        foreach (var (month, hours) in MonthHours())
        {
            for (var i = 0; i < hours; i++)
            {
                records.Add(new AnnualEnergyBalanceHourInput(
                    HourIndex: hour++,
                    Month: month,
                    HeatingLoadW: heatingW,
                    CoolingLoadW: coolingW,
                    TransmissionW: includeComponents ? heatingW : 0,
                    VentilationW: includeComponents ? heatingW * 0.1 : 0,
                    InfiltrationW: includeComponents ? heatingW * 0.05 : 0,
                    SolarGainsW: coolingW,
                    InternalGainsW: coolingW * 0.2,
                    GroundW: includeComponents ? heatingW * 0.02 : 0));
            }
        }

        return records;
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateHoursWithAvailableComponentsExceptInfiltration()
    {
        var records = new List<AnnualEnergyBalanceHourInput>(8760);
        var hour = 0;
        foreach (var (month, hours) in MonthHours())
        {
            for (var i = 0; i < hours; i++)
            {
                records.Add(new AnnualEnergyBalanceHourInput(
                    HourIndex: hour++,
                    Month: month,
                    HeatingLoadW: 100,
                    CoolingLoadW: 50,
                    TransmissionW: 40,
                    VentilationW: 20,
                    InfiltrationW: 0,
                    SolarGainsW: 30,
                    InternalGainsW: 10,
                    GroundW: 15));
            }
        }

        return records;
    }

    private static IEnumerable<(int Month, int Hours)> MonthHours()
    {
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        for (var month = 1; month <= 12; month++)
            yield return (month, daysPerMonth[month - 1] * 24);
    }
}
