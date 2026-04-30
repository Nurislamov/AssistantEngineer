using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Tests.Calculations.AnnualEnergy;

public class AnnualEnergyBalanceEngineTests
{
    private readonly AnnualEnergyBalanceEngine _engine = new();

    [Fact]
    public void Calculate_ConstantHeatingLoadConvertsHourlyWToAnnualKWh()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: 1000, coolingW: 0)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(8760, result.Value.AnnualHeatingDemandKWh, precision: 6);
        Assert.Equal(
            result.Value.AnnualHeatingDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.HeatingKWh),
            precision: 6);
    }

    [Fact]
    public void Calculate_TrueHourly8760SetsSourceMetadata()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: 1000, coolingW: 0),
            EnergyDataSource: "TrueHourlySimulation",
            IsTrueHourly8760: true));

        Assert.True(result.IsSuccess);
        Assert.Equal("TrueHourlySimulation", result.Value.EnergyDataSource);
        Assert.True(result.Value.IsTrueHourly8760);
        Assert.Equal(8760, result.Value.HourlyRecordCount);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.TrueHourlySimulationUsed");
    }

    [Fact]
    public void Calculate_MonthlyAdapterSourceIsNotTrueHourly8760()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateRepresentativeMonthlyRecords(),
            UsesSyntheticWeather: true,
            EnergyDataSource: "MonthlyBalanceAdapter",
            IsTrueHourly8760: false));

        Assert.True(result.IsSuccess);
        Assert.Equal("MonthlyBalanceAdapter", result.Value.EnergyDataSource);
        Assert.False(result.Value.IsTrueHourly8760);
        Assert.Equal(12, result.Value.HourlyRecordCount);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.MonthlyBalanceAdapter" &&
            diagnostic.Message.Contains("not a true hourly 8760", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_ConstantCoolingLoadConvertsHourlyWToAnnualKWh()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: 0, coolingW: 500)));

        Assert.True(result.IsSuccess);
        Assert.Equal(4380, result.Value.AnnualCoolingDemandKWh, precision: 6);
        Assert.Equal(
            result.Value.AnnualCoolingDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.CoolingKWh),
            precision: 6);
    }

    [Fact]
    public void Calculate_MonthlyAggregationEqualsAnnualTotal()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 100,
            Year: 2026,
            Hours: CreateMonthlyStepHours()));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            result.Value.AnnualTotalDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.TotalKWh),
            precision: 6);
    }

    [Fact]
    public void Calculate_EnergyUseIntensityUsesAnnualTotalAndArea()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: 10000.0 / 8760.0 * 1000.0, coolingW: 0)));

        Assert.True(result.IsSuccess);
        Assert.Equal(10000, result.Value.AnnualTotalDemandKWh, precision: 3);
        Assert.Equal(50, result.Value.EnergyUseIntensityKWhPerM2Year, precision: 3);
    }

    [Fact]
    public void Calculate_DetectsPeakHours()
    {
        var hours = CreateHours(heatingW: 100, coolingW: 50).ToArray();
        hours[100] = hours[100] with { HeatingLoadW = 1200 };
        hours[200] = hours[200] with { CoolingLoadW = 900 };

        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: hours));

        Assert.True(result.IsSuccess);
        Assert.Equal(1200, result.Value.PeakHeatingLoadW, precision: 6);
        Assert.Equal(100, result.Value.PeakHeatingHour);
        Assert.Equal(900, result.Value.PeakCoolingLoadW, precision: 6);
        Assert.Equal(200, result.Value.PeakCoolingHour);
    }

    [Fact]
    public void Calculate_SyntheticWeatherAddsDiagnostic()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: 100, coolingW: 0),
            UsesSyntheticWeather: true));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.SyntheticWeather");
    }

    [Fact]
    public void Calculate_NegativeLoadsDoNotCreateNegativeAnnualDemand()
    {
        var result = _engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Building",
            BuildingAreaM2: 200,
            Year: 2026,
            Hours: CreateHours(heatingW: -100, coolingW: -50)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.AnnualHeatingDemandKWh, precision: 6);
        Assert.Equal(0, result.Value.AnnualCoolingDemandKWh, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.NegativeHourlyValueClamped");
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateHours(
        double heatingW,
        double coolingW)
    {
        var result = new List<AnnualEnergyBalanceHourInput>(8760);
        var hour = 0;
        foreach (var (month, hours) in MonthHours())
        {
            for (var i = 0; i < hours; i++)
            {
                result.Add(new AnnualEnergyBalanceHourInput(
                    HourIndex: hour++,
                    Month: month,
                    HeatingLoadW: heatingW,
                    CoolingLoadW: coolingW,
                    TransmissionW: heatingW,
                    VentilationW: 0,
                    InfiltrationW: 0,
                    SolarGainsW: coolingW,
                    InternalGainsW: 0,
                    GroundW: 0));
            }
        }

        return result;
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateMonthlyStepHours()
    {
        var result = new List<AnnualEnergyBalanceHourInput>(8760);
        var hour = 0;
        foreach (var (month, hours) in MonthHours())
        {
            var heating = month <= 4 || month >= 10 ? month * 100 : 0;
            var cooling = month is >= 5 and <= 9 ? month * 50 : 0;
            for (var i = 0; i < hours; i++)
            {
                result.Add(new AnnualEnergyBalanceHourInput(
                    HourIndex: hour++,
                    Month: month,
                    HeatingLoadW: heating,
                    CoolingLoadW: cooling));
            }
        }

        return result;
    }

    private static IReadOnlyList<AnnualEnergyBalanceHourInput> CreateRepresentativeMonthlyRecords()
    {
        var result = new List<AnnualEnergyBalanceHourInput>(12);
        var hour = 0;
        foreach (var (month, hours) in MonthHours())
        {
            result.Add(new AnnualEnergyBalanceHourInput(
                HourIndex: hour,
                Month: month,
                HeatingLoadW: month <= 4 || month >= 10 ? 100 : 0,
                CoolingLoadW: month is >= 5 and <= 9 ? 50 : 0,
                HourDurationH: hours));
            hour += hours;
        }

        return result;
    }

    private static IEnumerable<(int Month, int Hours)> MonthHours()
    {
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        for (var month = 1; month <= 12; month++)
            yield return (month, daysPerMonth[month - 1] * 24);
    }
}
