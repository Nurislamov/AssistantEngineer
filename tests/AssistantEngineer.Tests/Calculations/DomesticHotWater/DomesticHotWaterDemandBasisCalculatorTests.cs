using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterDemandBasisCalculatorTests
{
    private readonly DomesticHotWaterDemandBasisCalculator _calculator =
        new(new Iso12831DomesticHotWaterReferenceDataProvider());

    [Fact]
    public void CalculatesPeopleBasedDailyVolume()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.People,
            OccupantCount = 4,
            DailyVolumeLitersPerPerson = 50
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(200.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void CalculatesDwellingUnitBasedDailyVolume()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.DwellingUnit,
            DwellingUnitCount = 2,
            DailyVolumeLitersPerDwellingUnit = 120
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(240.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void CalculatesAreaBasedDailyVolume()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.FloorArea,
            FloorAreaSquareMeters = 100,
            DailyVolumeLitersPerSquareMeter = 0.5
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(50.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void CalculatesFixtureUseVolumeFromLitersPerUse()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.FixtureUse,
            FixtureUses =
            [
                new DomesticHotWaterFixtureUseInput(
                    FixtureId: "F1",
                    Name: "Sink",
                    UsesPerDay: 10,
                    LitersPerUse: 6,
                    UseDurationMinutes: null,
                    FlowRateLitersPerMinute: null,
                    Source: "UnitTest",
                    Diagnostics: [])
            ]
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(60.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void CalculatesFixtureUseVolumeFromFlowAndDuration()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.FixtureUse,
            FixtureUses =
            [
                new DomesticHotWaterFixtureUseInput(
                    FixtureId: "F1",
                    Name: "Shower",
                    UsesPerDay: 5,
                    LitersPerUse: null,
                    UseDurationMinutes: 2,
                    FlowRateLitersPerMinute: 8,
                    Source: "UnitTest",
                    Diagnostics: [])
            ]
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(80.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void UsesCustomDailyVolume()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.CustomDailyVolume,
            CustomDailyVolumeLiters = 300
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(300.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void CustomHourlyVolumeCalculatesAverageDailyVolume()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.CustomHourlyVolume,
            CustomHourlyVolumeLiters = Enumerable.Repeat(1.0, 8760).ToArray()
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.True(result.UsesCustomHourlyVolume);
        Assert.Equal(24.0, result.DailyVolumeLiters, 6);
        Assert.Equal(8760.0, result.CustomHourlyVolumeLiters8760.Sum(), 6);
    }

    [Fact]
    public void ScheduledEnergyStoresHourlyProfile()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.ScheduledEnergy,
            CustomHourlyUsefulEnergyKWh = Enumerable.Repeat(0.5, 8760).ToArray()
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.True(result.UsesScheduledUsefulEnergy);
        Assert.NotNull(result.ScheduledUsefulEnergyKWh);
        Assert.Equal(8760, result.ScheduledUsefulEnergyKWh!.Count);
        Assert.Equal(0.0, result.DailyVolumeLiters, 6);
    }

    [Fact]
    public void UnsupportedBasisDoesNotFallbackSilently()
    {
        var input = CreateInput() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.Other
        };

        var result = _calculator.CalculateDailyVolume(input);

        Assert.Equal(0.0, result.DailyVolumeLiters, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DEMAND-BASIS-UNSUPPORTED");
    }

    private static DomesticHotWaterDemandBasisInput CreateInput() =>
        new(
            DemandBasis: DomesticHotWaterDemandBasis.People,
            UseCategory: DomesticHotWaterUseCategory.Residential,
            OccupantCount: 1,
            DwellingUnitCount: null,
            FloorAreaSquareMeters: null,
            DailyVolumeLitersPerPerson: 40,
            DailyVolumeLitersPerDwellingUnit: null,
            DailyVolumeLitersPerSquareMeter: null,
            CustomDailyVolumeLiters: null,
            FixtureUses: [],
            CustomHourlyVolumeLiters: null,
            CustomDailyProfileFractions: null,
            Source: "UnitTest",
            Diagnostics: []);
}
