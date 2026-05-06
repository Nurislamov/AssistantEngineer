using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterDemandCalculatorTests
{
    private readonly Iso12831DomesticHotWaterDemandCalculator _calculator = new(
        new Iso12831DomesticHotWaterReferenceDataProvider(),
        new Iso12831DomesticHotWaterDrawProfileProvider());

    [Fact]
    public void ResidentialPeopleBased_ReturnsDeterministicResult()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.ResidentialApartment,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            PeopleCount: 4,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 50,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 55,
            ColdWaterTemperatureC: 10,
            DistributionLossFactor: 0.1,
            StorageLossKWhPerDay: 1.0,
            CirculationLossKWhPerDay: 0.5,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(200.0, result.Value.DailyVolumeLiters, 3);
        Assert.Equal(10.465, result.Value.DailyDrawEnergyKWh, 3);
        Assert.Equal(13.012, result.Value.DailyTotalEnergyKWh, 3);
        Assert.Equal(12, result.Value.MonthlyResults.Count);
        Assert.Empty(result.Value.HourlyResults);
    }

    [Fact]
    public void IncludeHourlyProfile_Returns8760Records()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.Office,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.AreaBased,
            PeopleCount: 0,
            EquivalentOccupants: 0,
            AreaM2: 1000,
            UnitsCount: 0,
            LitersPerPersonDay: 0,
            LitersPerM2Day: 0.4,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 60,
            ColdWaterTemperatureC: 12,
            DistributionLossFactor: 0.08,
            StorageLossKWhPerDay: 3.0,
            CirculationLossKWhPerDay: 2.0,
            IncludeHourlyProfile: true,
            Year: 2025,
            HolidayDates: new HashSet<DateOnly> { new(2025, 1, 1) },
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(8760, result.Value.HourlyResults.Count);
        Assert.Equal(12, result.Value.MonthlyResults.Count);
        Assert.Equal(
            result.Value.AnnualTotalEnergyKWh,
            result.Value.MonthlyResults.Sum(item => item.TotalEnergyKWh),
            3);
    }

    [Fact]
    public void ZeroOccupantsPeopleBased_ReturnsZeroDemandWhenLossesZero()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.ResidentialApartment,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            PeopleCount: 0,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 45,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 50,
            ColdWaterTemperatureC: 10,
            DistributionLossFactor: 0.0,
            StorageLossKWhPerDay: 0.0,
            CirculationLossKWhPerDay: 0.0,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(0.0, result.Value.DailyVolumeLiters, 6);
        Assert.Equal(0.0, result.Value.AnnualTotalEnergyKWh, 6);
    }

    [Fact]
    public void InvalidTemperatureDelta_ReturnsValidationFailure()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.Custom,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.CustomVolume,
            PeopleCount: 0,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 0,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 100,
            HotWaterTemperatureC: 10,
            ColdWaterTemperatureC: 20,
            DistributionLossFactor: 0.0,
            StorageLossKWhPerDay: 0.0,
            CirculationLossKWhPerDay: 0.0,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsFailure);
    }
}
