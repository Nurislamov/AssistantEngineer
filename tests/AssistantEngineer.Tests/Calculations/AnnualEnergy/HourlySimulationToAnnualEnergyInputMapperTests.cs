using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
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
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable");
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

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("ground", StringComparison.OrdinalIgnoreCase));
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

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable");
    }

    [Fact]
    public void Map_DoesNotWarnForExplicitZeroInfiltrationWhenSplitIsAvailable()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateHoursWithAvailableComponentsExceptInfiltration();

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test",
            infiltrationSplitAvailable: true);

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.HourlyComponentBreakdownPartial" &&
            diagnostic.Message.Contains("infiltration", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable");
    }

    [Fact]
    public void Map_PreservesSignedComponentBalanceFields()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateSignedComponentHours();

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        var first = result.Input.Hours[0];

        Assert.Equal(-100.0, first.TransmissionBalanceW, precision: 6);
        Assert.Equal(-50.0, first.VentilationBalanceW, precision: 6);
        Assert.Equal(0.0, first.InfiltrationBalanceW, precision: 6);
        Assert.Equal(10.0, first.GroundBalanceW, precision: 6);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "AnnualEnergy.SignedComponentBalanceAvailable");
    }

    [Fact]
    public void Map_PreservesInfiltrationMagnitudeAndSignedBalanceWhenProvided()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateSignedComponentHoursWithInfiltration();

        var result = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        var first = result.Input.Hours[0];

        Assert.Equal(20.0, first.InfiltrationW, precision: 6);
        Assert.Equal(-20.0, first.InfiltrationBalanceW, precision: 6);
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable");
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

    [Fact]
    public void Map_SignedOutputAggregatesNetComponentTotalsInAnnualEnergyEngine()
    {
        var mapper = new HourlySimulationToAnnualEnergyInputMapper();
        var records = CreateSignedComponentHours();

        var mapping = mapper.Map(
            buildingId: 1,
            buildingName: "Building",
            buildingAreaM2: 120,
            year: 2026,
            hourlyRecords: records,
            diagnosticsContext: "test");

        var annual = new AnnualEnergyBalanceEngine().Calculate(mapping.Input);

        Assert.True(annual.IsSuccess, annual.Error);

        Assert.Equal(876, annual.Value.ComponentBreakdown.TransmissionKWh, precision: 6);
        Assert.Equal(-876, annual.Value.ComponentBreakdown.NetTransmissionKWh, precision: 6);

        Assert.Equal(438, annual.Value.ComponentBreakdown.VentilationKWh, precision: 6);
        Assert.Equal(-438, annual.Value.ComponentBreakdown.NetVentilationKWh, precision: 6);

        Assert.Equal(262.8, annual.Value.ComponentBreakdown.GroundKWh, precision: 6);
        Assert.Equal(87.6, annual.Value.ComponentBreakdown.NetGroundKWh, precision: 6);

        Assert.Contains(annual.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "AnnualEnergy.SignedComponentBalanceAvailable");
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

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateSignedComponentHours()
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
                    TransmissionW: 100,
                    VentilationW: 50,
                    InfiltrationW: 0,
                    SolarGainsW: 20,
                    InternalGainsW: 10,
                    GroundW: 30,
                    HourDurationH: 1,
                    TransmissionBalanceW: -100,
                    VentilationBalanceW: -50,
                    InfiltrationBalanceW: 0,
                    GroundBalanceW: 10));
            }
        }

        return records;
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateSignedComponentHoursWithInfiltration()
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
                    TransmissionW: 100,
                    VentilationW: 50,
                    InfiltrationW: 20,
                    SolarGainsW: 20,
                    InternalGainsW: 10,
                    GroundW: 30,
                    HourDurationH: 1,
                    TransmissionBalanceW: -100,
                    VentilationBalanceW: -50,
                    InfiltrationBalanceW: -20,
                    GroundBalanceW: 10));
            }
        }

        return records;
    }

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
