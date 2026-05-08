using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterDemandInputValidatorTests
{
    private readonly DomesticHotWaterDemandInputValidator _validator = new();

    [Fact]
    public void AcceptsValidPeopleBasedInput()
    {
        var input = CreateInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = CreateInput() with { CalculationId = "" };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsUnknownDemandBasis()
    {
        var input = CreateInput() with
        {
            Demand = CreateDemandInput() with
            {
                DemandBasis = DomesticHotWaterDemandBasis.Unknown
            }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DEMAND-BASIS-UNKNOWN");
    }

    [Fact]
    public void RejectsNonPositiveTemperatureRise()
    {
        var input = CreateInput() with
        {
            TemperatureModel = CreateTemperatureModel(cold: 55.0, hot: 55.0)
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-TEMPERATURE-RISE-NONPOSITIVE");
    }

    [Fact]
    public void RejectsInvalidCustomHourlyVolume()
    {
        var hourly = Enumerable.Repeat(1.0, 8759).ToArray();
        var input = CreateInput() with
        {
            Demand = CreateDemandInput() with
            {
                DemandBasis = DomesticHotWaterDemandBasis.CustomHourlyVolume,
                CustomHourlyVolumeLiters = hourly
            }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CUSTOM-HOURLY-VOLUME-INVALID");
    }

    [Fact]
    public void RejectsInvalidHourlyProfile()
    {
        var input = CreateInput() with
        {
            DrawProfile = CreateDrawProfileInput() with
            {
                HourlyFractions24 = Enumerable.Repeat(1.0, 23).ToArray()
            }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DAILY-PROFILE-INVALID");
    }

    [Fact]
    public void RejectsInvalidMonthlyProfile()
    {
        var input = CreateInput() with
        {
            DrawProfile = CreateDrawProfileInput() with
            {
                MonthlyFractions12 = Enumerable.Repeat(1.0, 11).ToArray()
            }
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-MONTHLY-PROFILE-INVALID");
    }

    private static DomesticHotWaterUsefulDemandInput CreateInput() =>
        new(
            CalculationId: "DHW-VAL-1",
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            Demand: CreateDemandInput(),
            TemperatureModel: CreateTemperatureModel(),
            DrawProfile: CreateDrawProfileInput(),
            WaterDensityKgPerLiter: 1.0,
            WaterSpecificHeatJPerKgKelvin: 4186.0,
            DisclosureOverride: null,
            Source: "UnitTest");

    private static DomesticHotWaterDemandBasisInput CreateDemandInput() =>
        new(
            DemandBasis: DomesticHotWaterDemandBasis.People,
            UseCategory: DomesticHotWaterUseCategory.Residential,
            OccupantCount: 4,
            DwellingUnitCount: null,
            FloorAreaSquareMeters: null,
            DailyVolumeLitersPerPerson: 50,
            DailyVolumeLitersPerDwellingUnit: null,
            DailyVolumeLitersPerSquareMeter: null,
            CustomDailyVolumeLiters: null,
            FixtureUses: [],
            CustomHourlyVolumeLiters: null,
            CustomDailyProfileFractions: null,
            Source: "UnitTest",
            Diagnostics: []);

    private static DomesticHotWaterTemperatureModel CreateTemperatureModel(
        double cold = 10.0,
        double hot = 55.0) =>
        new(
            ColdWaterTemperatureCelsius: cold,
            HotWaterSetpointTemperatureCelsius: hot,
            UseTemperatureCelsius: null,
            Source: "UnitTest",
            Diagnostics: []);

    private static DomesticHotWaterDrawProfileInput CreateDrawProfileInput() =>
        new(
            ProfileId: "P1",
            HourlyFractions24: Enumerable.Repeat(1.0, 24).ToArray(),
            MonthlyFractions12: Enumerable.Repeat(1.0, 12).ToArray(),
            AnnualHourlyFractions8760: null,
            Source: "UnitTest",
            Diagnostics: []);
}
