using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterReferenceDataProviderTests
{
    private readonly Iso12831DomesticHotWaterReferenceDataProvider _provider = new();
    private readonly Iso12831DomesticHotWaterDemandCalculator _calculator;

    public Iso12831DomesticHotWaterReferenceDataProviderTests()
    {
        _calculator = new Iso12831DomesticHotWaterDemandCalculator(
            _provider,
            new Iso12831DomesticHotWaterDrawProfileProvider());
    }

    [Theory]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.ResidentialApartment)]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.Office)]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.School)]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.Hotel)]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.Healthcare)]
    [InlineData(Iso12831DomesticHotWaterUsageCategory.Custom)]
    public void TableDrivenReference_ResolvesSupportedCategories(Iso12831DomesticHotWaterUsageCategory usageCategory)
    {
        var resolved = _provider.Resolve(
            usageCategory,
            useTableDrivenReferenceData: true,
            tableDrivenUsageCategory: null);

        Assert.NotNull(resolved.UsageProfileSet);
        Assert.False(string.IsNullOrWhiteSpace(resolved.ReferenceEntryId));
        Assert.True(resolved.ReferenceDefaults.LitersPerPersonDay > 0);
        Assert.True(resolved.ReferenceDefaults.LitersPerM2Day >= 0);
        Assert.True(resolved.ReferenceDefaults.LitersPerUnitDay > 0);
        Assert.True(resolved.ReferenceDefaults.EquivalentOccupantFactor > 0);
    }

    [Fact]
    public void ExplicitInput_OverridesTableDefault()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.ResidentialApartment,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            PeopleCount: 2,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 90,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 55,
            ColdWaterTemperatureC: 10,
            DistributionLossFactor: 0,
            StorageLossKWhPerDay: 0,
            CirculationLossKWhPerDay: 0,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null,
            UseTableDrivenReferenceData: true,
            TableDrivenUsageCategory: DomesticHotWaterUsageCategory.ResidentialDwelling);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(180.0, result.Value.DailyVolumeLiters, 6);
    }

    [Fact]
    public void UnknownCategory_UsesGenericFallback()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: (Iso12831DomesticHotWaterUsageCategory)999,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            PeopleCount: 2,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 0,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 55,
            ColdWaterTemperatureC: 10,
            DistributionLossFactor: 0,
            StorageLossKWhPerDay: 0,
            CirculationLossKWhPerDay: 0,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null,
            UseTableDrivenReferenceData: true,
            TableDrivenUsageCategory: null);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(80.0, result.Value.DailyVolumeLiters, 6);
    }

    [Fact]
    public void AnnualEnergy_EqualsSumOfMonthlyEnergy()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.Office,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.AreaBased,
            PeopleCount: 0,
            EquivalentOccupants: 0,
            AreaM2: 2400,
            UnitsCount: 0,
            LitersPerPersonDay: 0,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 60,
            ColdWaterTemperatureC: 12,
            DistributionLossFactor: 0.08,
            StorageLossKWhPerDay: 2.5,
            CirculationLossKWhPerDay: 1.5,
            IncludeHourlyProfile: false,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null,
            UseTableDrivenReferenceData: true,
            TableDrivenUsageCategory: DomesticHotWaterUsageCategory.Office);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(
            result.Value.AnnualTotalEnergyKWh,
            result.Value.MonthlyResults.Sum(item => item.TotalEnergyKWh),
            3);
    }

    [Fact]
    public void HourlyProfile_SumsToDailyAndAnnualDemand()
    {
        var input = new Iso12831DomesticHotWaterInput(
            UsageCategory: Iso12831DomesticHotWaterUsageCategory.School,
            ReferenceMode: Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            PeopleCount: 120,
            EquivalentOccupants: 0,
            AreaM2: 0,
            UnitsCount: 0,
            LitersPerPersonDay: 0,
            LitersPerM2Day: 0,
            LitersPerUnitDay: 0,
            CustomDailyVolumeLiters: 0,
            HotWaterTemperatureC: 52,
            ColdWaterTemperatureC: 9,
            DistributionLossFactor: 0.05,
            StorageLossKWhPerDay: 1.2,
            CirculationLossKWhPerDay: 0.8,
            IncludeHourlyProfile: true,
            Year: 2025,
            HolidayDates: null,
            DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.SchoolDaytime,
            WeekdayDrawProfile: null,
            WeekendDrawProfile: null,
            CustomDrawProfile: null,
            UseTableDrivenReferenceData: true,
            TableDrivenUsageCategory: DomesticHotWaterUsageCategory.School);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(8760, result.Value.HourlyResults.Count);

        var annualVolumeFromHourly = result.Value.HourlyResults.Sum(item => item.VolumeLiters);
        var annualEnergyFromHourly = result.Value.HourlyResults.Sum(item => item.TotalEnergyKWh);

        Assert.Equal(result.Value.AnnualVolumeLiters, annualVolumeFromHourly, 1);
        Assert.InRange(
            Math.Abs(result.Value.AnnualTotalEnergyKWh - annualEnergyFromHourly),
            0.0,
            0.5);
    }
}
