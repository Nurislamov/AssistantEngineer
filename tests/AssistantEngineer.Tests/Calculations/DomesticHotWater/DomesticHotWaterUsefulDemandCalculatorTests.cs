using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterUsefulDemandCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly DomesticHotWaterUsefulDemandCalculator _calculator = new(
        new DomesticHotWaterDemandInputValidator(),
        new DomesticHotWaterDemandBasisCalculator(new Iso12831DomesticHotWaterReferenceDataProvider()),
        new DomesticHotWaterDrawProfileBuilder(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void CalculatesUsefulEnergyForPeopleBasedDemand()
    {
        var input = CreateInput();

        var result = _calculator.Calculate(input);

        var expectedDailyKWh = 200.0 * 1.0 * 4186.0 * 45.0 / 3_600_000.0;

        Assert.Equal(200.0, result.DailyVolumeLiters, 6);
        Assert.Equal(expectedDailyKWh, result.DailyUsefulEnergyKWh, 6);
        Assert.Equal(expectedDailyKWh * 365.0, result.AnnualUsefulEnergyKWh, 4);
    }

    [Fact]
    public void Builds8760HourlyVolumeAndEnergyProfiles()
    {
        var input = CreateInput();

        var result = _calculator.Calculate(input);

        Assert.Equal(8760, result.HourlyVolumeLiters8760.Count);
        Assert.Equal(8760, result.HourlyUsefulEnergyKWh8760.Count);
        Assert.Equal(result.AnnualVolumeLiters, result.HourlyVolumeLiters8760.Sum(), 6);
        Assert.Equal(result.AnnualUsefulEnergyKWh, result.HourlyUsefulEnergyKWh8760.Sum(), 6);
    }

    [Fact]
    public void MonthlySumsUseNonLeapMonthLengths()
    {
        var input = CreateInput();

        var result = _calculator.Calculate(input);

        Assert.True(result.MonthlyVolumeLiters[0] > result.MonthlyVolumeLiters[1]);
        Assert.Equal(result.AnnualVolumeLiters, result.MonthlyVolumeLiters.Sum(), 6);
    }

    [Fact]
    public void UsesCustomHourlyVolumeDirectly()
    {
        var customHourly = Enumerable.Repeat(1.0, 8760).ToArray();
        var input = CreateInput() with
        {
            Demand = CreateDemandInput() with
            {
                DemandBasis = DomesticHotWaterDemandBasis.CustomHourlyVolume,
                OccupantCount = null,
                DailyVolumeLitersPerPerson = null,
                CustomHourlyVolumeLiters = customHourly
            }
        };

        var result = _calculator.Calculate(input);

        Assert.Equal(customHourly.Sum(), result.AnnualVolumeLiters, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CUSTOM-HOURLY-VOLUME-USED");
    }

    [Fact]
    public void DefaultsWaterDensityAndCpWithDiagnostics()
    {
        var input = CreateInput() with
        {
            WaterDensityKgPerLiter = null,
            WaterSpecificHeatJPerKgKelvin = null
        };

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-WATER-DENSITY-DEFAULTED");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-WATER-CP-DEFAULTED");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.ISO12831,
            Stage: StandardCalculationStage.DomesticHotWater,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/DhwOverride",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims:
                [
                    "safe claim",
                    "Full EN compliance",
                    "prefix Full ISO compliance suffix"
                ],
                ForbiddenClaims: [],
                Limitations: ["Unit test"],
                Assumptions: ["Unit test"]),
            Diagnostics: []);

        var input = CreateInput() with
        {
            DisclosureOverride = disclosureOverride
        };

        var result = _calculator.Calculate(input);

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(forbiddenClaim, result.Disclosure.ClaimBoundary.ForbiddenClaims, StringComparer.Ordinal);
            Assert.DoesNotContain(
                result.Disclosure.ClaimBoundary.AllowedClaims,
                claim => claim.Contains(forbiddenClaim, StringComparison.Ordinal));
        }
    }

    private static DomesticHotWaterUsefulDemandInput CreateInput() =>
        new(
            CalculationId: "DHW-CALC-1",
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            Demand: CreateDemandInput(),
            TemperatureModel: new DomesticHotWaterTemperatureModel(
                ColdWaterTemperatureCelsius: 10.0,
                HotWaterSetpointTemperatureCelsius: 55.0,
                UseTemperatureCelsius: null,
                Source: "UnitTest",
                Diagnostics: []),
            DrawProfile: new DomesticHotWaterDrawProfileInput(
                ProfileId: "P1",
                HourlyFractions24: null,
                MonthlyFractions12: null,
                AnnualHourlyFractions8760: null,
                Source: "UnitTest",
                Diagnostics: []),
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
}
