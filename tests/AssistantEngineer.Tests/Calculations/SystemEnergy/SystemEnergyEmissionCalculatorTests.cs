using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyEmissionCalculatorTests
{
    private readonly SystemEnergyEmissionCalculator _calculator = new();

    [Fact]
    public void CalculatesEmissionsForCarrier()
    {
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)
            ],
            [
                SystemEnergyPrimaryTestData.CreateEmissionFactor(SystemEnergyCarrier.Electricity, kgPerKWh: 0.5)
            ]);

        var result = _calculator.Calculate(finalEnergy, factorSet);
        var carrierResult = Assert.Single(result, item => item.Carrier == SystemEnergyCarrier.Electricity);

        Assert.Equal(4380.0, carrierResult.AnnualEmissionsKg, 6);
    }

    [Fact]
    public void MissingEmissionFactorProducesDiagnostic()
    {
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.NaturalGas] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.NaturalGas, 0.0, 1.1, 1.1)],
            []);
        var partialFactors = factorSet with
        {
            EmissionFactors =
            [
                SystemEnergyPrimaryTestData.CreateEmissionFactor(SystemEnergyCarrier.Electricity, kgPerKWh: 0.5)
            ]
        };

        var result = _calculator.Calculate(finalEnergy, partialFactors);

        Assert.Contains(
            result.SelectMany(item => item.Diagnostics),
            diagnostic => diagnostic.Code == "AE-SYS-EMISSION-FACTOR-MISSING");
    }

    [Fact]
    public void NoEmissionFactorsReturnsEmptyWithDiagnostic()
    {
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)],
            []);

        var result = _calculator.Calculate(finalEnergy, factorSet);

        Assert.Empty(result);

        var primaryCalculator = new SystemEnergyPrimaryEnergyCalculator(
            new SystemEnergyFactorSetValidator(),
            _calculator,
            new StandardCalculationDisclosureFactory());
        var primaryResult = primaryCalculator.Calculate(finalEnergy, factorSet);

        Assert.Contains(
            primaryResult.Diagnostics,
            diagnostic => diagnostic.Code == "AE-SYS-EMISSION-FACTORS-NOT-PROVIDED");
    }
}
