using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFinalEnergyCalculatorTests
{
    [Fact]
    public void CalculatesBoilerFinalEnergyFromGenerationHandoff()
    {
        var calculator = CreateCalculator();
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 9.0);
        var boiler = SystemEnergyTestData.CreateGenerator(efficiency: 0.9);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            handoff,
            SystemEnergyTestData.CreateGeneratorSet([boiler]));

        var result = calculator.Calculate(input);

        Assert.Equal(10.0, result.HourlyFinalEnergyByCarrierKWh8760[SystemEnergyCarrier.NaturalGas][0], 6);
        Assert.Equal(87600.0, result.AnnualFinalEnergyByCarrierKWh[SystemEnergyCarrier.NaturalGas], 6);
    }

    [Fact]
    public void CalculatesHeatPumpFinalEnergyFromGenerationHandoff()
    {
        var calculator = CreateCalculator();
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 12.0);
        var hp = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.HeatPump,
            mode: SystemEnergyGeneratorCalculationMode.FixedCop,
            carrier: SystemEnergyCarrier.Electricity,
            efficiency: null,
            cop: 3.0);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            handoff,
            SystemEnergyTestData.CreateGeneratorSet([hp]));

        var result = calculator.Calculate(input);

        Assert.Equal(4.0, result.HourlyFinalEnergyByCarrierKWh8760[SystemEnergyCarrier.Electricity][0], 6);
    }

    [Fact]
    public void CalculatesMultiGeneratorCapacityLimitedPriority()
    {
        var calculator = CreateCalculator();
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var boiler1 = SystemEnergyTestData.CreateGenerator(generatorId: "B1", priority: 0, capacity: 6.0, efficiency: 0.9);
        var boiler2 = SystemEnergyTestData.CreateGenerator(generatorId: "B2", priority: 1, capacity: null, efficiency: 0.8);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            handoff,
            SystemEnergyTestData.CreateGeneratorSet([boiler1, boiler2], SystemEnergyLoadSplitMode.CapacityLimitedPriority));

        var result = calculator.Calculate(input);
        var expectedHourly = (6.0 / 0.9) + (4.0 / 0.8);

        Assert.Equal(expectedHourly, result.HourlyFinalEnergyByCarrierKWh8760[SystemEnergyCarrier.NaturalGas][0], 6);
        Assert.Equal(0.0, result.AnnualTotalUnmetSystemLoadKWh, 6);
    }

    [Fact]
    public void TracksUnmetLoadWhenCapacityInsufficient()
    {
        var calculator = CreateCalculator();
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var boiler = SystemEnergyTestData.CreateGenerator(capacity: 6.0, efficiency: 0.9);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            handoff,
            SystemEnergyTestData.CreateGeneratorSet([boiler], SystemEnergyLoadSplitMode.CapacityLimitedPriority));

        var result = calculator.Calculate(input);

        Assert.Equal(4.0 * 8760, result.AnnualTotalUnmetSystemLoadKWh, 6);
        Assert.Equal(SystemEnergyFinalEnergyStatus.PartiallyCalculated, result.Status);
    }

    [Fact]
    public void PrimaryEnergyIsDeferred()
    {
        var calculator = CreateCalculator();
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput();

        var result = calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FINAL-PRIMARY-ENERGY-DEFERRED");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var calculator = CreateCalculator();
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput();

        var result = calculator.Calculate(input);
        var boundary = result.Disclosure.ClaimBoundary;

        Assert.Contains("Full ISO compliance", boundary.ForbiddenClaims);
        Assert.Contains("Full EN compliance", boundary.ForbiddenClaims);
        Assert.Contains("pyBuildingEnergy parity", boundary.ForbiddenClaims);
        Assert.Contains("EnergyPlus parity", boundary.ForbiddenClaims);
        Assert.Contains("ASHRAE 140 validation", boundary.ForbiddenClaims);
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full ISO compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full EN compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("pyBuildingEnergy parity", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("EnergyPlus parity", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("ASHRAE 140 validation", StringComparison.Ordinal));
    }

    private static SystemEnergyFinalEnergyCalculator CreateCalculator()
    {
        var disclosureFactory = new StandardCalculationDisclosureFactory();
        return new SystemEnergyFinalEnergyCalculator(
            new SystemEnergyGeneratorInputValidator(),
            new SystemEnergyGeneratorLoadSplitter(),
            new SystemEnergyGeneratorFinalEnergyCalculator(),
            new SystemEnergyFinalEnergyAggregator(disclosureFactory));
    }
}
