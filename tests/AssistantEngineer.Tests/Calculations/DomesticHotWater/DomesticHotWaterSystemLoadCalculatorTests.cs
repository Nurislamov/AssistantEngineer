using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterSystemLoadCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly DomesticHotWaterSystemLoadCalculator _calculator = new(
        new DomesticHotWaterSystemLossInputValidator(),
        new DomesticHotWaterStorageLossCalculator(),
        new DomesticHotWaterDistributionLossCalculator(),
        new DomesticHotWaterCirculationLossCalculator(),
        new DomesticHotWaterEn15316HandoffBuilder(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void CalculatesSystemHeatRequirement()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand(hourlyUsefulEnergy: Enumerable.Repeat(10.0, 8760).ToArray());
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(
            usefulDemand: useful,
            storage: DomesticHotWaterSystemLossTestData.CreateStorageInput(standingLossW: 100.0),
            distribution: DomesticHotWaterSystemLossTestData.CreateDistributionInput(),
            circulation: DomesticHotWaterSystemLossTestData.CreateCirculationInput(pumpPowerW: 50.0));

        var result = _calculator.Calculate(input);

        var expected = useful.AnnualUsefulEnergyKWh + result.AnnualStorageLossKWh + result.AnnualDistributionLossKWh + result.AnnualCirculationLossKWh;
        Assert.Equal(expected, result.AnnualSystemHeatRequirementKWh, 6);
        Assert.Equal(438.0, result.AnnualAuxiliaryElectricityKWh, 3);
    }

    [Fact]
    public void Builds8760HourlySystemHeatRequirementProfile()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput();

        var result = _calculator.Calculate(input);

        Assert.Equal(8760, result.HourlySystemHeatRequirementKWh8760.Count);
        Assert.Equal(result.AnnualSystemHeatRequirementKWh, result.HourlySystemHeatRequirementKWh8760.Sum(), 6);
    }

    [Fact]
    public void AggregatesMonthlySystemHeatRequirement()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput();

        var result = _calculator.Calculate(input);

        Assert.Equal(12, result.MonthlySystemHeatRequirementKWh.Count);
        Assert.Equal(result.AnnualSystemHeatRequirementKWh, result.MonthlySystemHeatRequirementKWh.Sum(), 6);
    }

    [Fact]
    public void BuildsRecoverableAndNonRecoverableProfiles()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(
            storage: DomesticHotWaterSystemLossTestData.CreateStorageInput(recoverableFraction: 0.25),
            distribution: DomesticHotWaterSystemLossTestData.CreateDistributionInput(recoverableFraction: 0.5),
            circulation: DomesticHotWaterSystemLossTestData.CreateCirculationInput(recoverableFraction: 0.75));

        var result = _calculator.Calculate(input);

        Assert.Equal(8760, result.HourlyRecoverableLossKWh8760.Count);
        Assert.Equal(8760, result.HourlyNonRecoverableLossKWh8760.Count);

        var thermalLossAnnual = result.AnnualStorageLossKWh + result.AnnualDistributionLossKWh + result.AnnualCirculationLossKWh;
        Assert.Equal(thermalLossAnnual, result.AnnualRecoverableLossKWh + result.AnnualNonRecoverableLossKWh, 6);
    }

    [Fact]
    public void BuildsEn15316Handoff()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput();

        var result = _calculator.Calculate(input);

        Assert.Equal(8760, result.En15316Handoff.HourlyUsefulDhwEnergyKWh8760.Count);
        Assert.Equal(8760, result.En15316Handoff.HourlyDhwSystemHeatRequirementKWh8760.Count);
        Assert.Equal(8760, result.En15316Handoff.HourlyDhwAuxiliaryElectricityKWh8760.Count);
        Assert.Contains(result.En15316Handoff.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-EN15316-HANDOFF-ONLY");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.ISO12831,
            Stage: StandardCalculationStage.DomesticHotWater,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/DhwSystemOverride",
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

        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput() with
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
}
