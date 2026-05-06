using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyChainCalculatorTests
{
    private readonly En15316SystemEnergyChainCalculator _calculator = new(
        new En15316SystemEnergyReferenceDataProvider());

    [Fact]
    public void BoilerHeatingChain_ComputesExpectedFinalAndPrimaryEnergy()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Heating,
                    EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                    GenerationTechnology: En15316GenerationTechnology.Boiler,
                    UsefulEnergyKWh: 10000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 0.95),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 0.9),
                    Storage: new En15316SystemEnergyModuleInput(LossFactor: 0.1),
                    GenerationEfficiency: 0.88,
                    AuxiliaryEnergyKWh: 200,
                    PrimaryEnergyFactor: 1.1)
            ],
            DiagnosticsContext: "en15316-boiler");

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);

        Assert.Equal(14819.88304, result.Value.TotalFinalEnergyKWh, precision: 5);
        Assert.Equal(16301.871344, result.Value.TotalPrimaryEnergyKWh, precision: 5);
        Assert.NotEmpty(result.Value.AssumptionsUsed);
    }

    [Fact]
    public void HeatPumpCopPath_UsesCopAndIncludesRenewableAndNonRenewablePrimary()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Heating,
                    EnergyCarrier: En15316EnergyCarrier.Electricity,
                    GenerationTechnology: En15316GenerationTechnology.HeatPump,
                    UsefulEnergyKWh: 12000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 0.97),
                    Distribution: new En15316SystemEnergyModuleInput(LossFactor: 0.08),
                    Storage: new En15316SystemEnergyModuleInput(LossFactor: 0.05),
                    GenerationCop: 3.4,
                    AuxiliaryEnergyKWh: 150,
                    RecoveredLossFraction: 0.1,
                    PrimaryEnergyFactor: 2.2,
                    RenewablePrimaryEnergyFactor: 0.5,
                    NonRenewablePrimaryEnergyFactor: 1.7)
            ]);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);

        var endUse = Assert.Single(result.Value.EndUses);
        Assert.Equal(4073.250455, endUse.FinalEnergyKWh, precision: 5);
        Assert.Equal(8961.151001, endUse.PrimaryEnergyKWh, precision: 5);
        Assert.Equal(2036.625227, endUse.RenewablePrimaryEnergyKWh!.Value, precision: 5);
        Assert.Equal(6924.525774, endUse.NonRenewablePrimaryEnergyKWh!.Value, precision: 5);
    }

    [Fact]
    public void AuxiliaryEnergy_IsIncludedInFinalEnergy()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Cooling,
                    EnergyCarrier: En15316EnergyCarrier.Electricity,
                    GenerationTechnology: En15316GenerationTechnology.Chiller,
                    UsefulEnergyKWh: 6000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    GenerationCop: 3.0,
                    AuxiliaryEnergyKWh: 120,
                    RecoveredLossFraction: 0.0,
                    PrimaryEnergyFactor: 2.0)
            ]);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);

        var endUse = Assert.Single(result.Value.EndUses);
        Assert.Equal(2120.0, endUse.FinalEnergyKWh, precision: 6);
    }

    [Fact]
    public void NegativeUsefulEnergy_ReturnsValidationFailure()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Heating,
                    EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                    GenerationTechnology: En15316GenerationTechnology.Boiler,
                    UsefulEnergyKWh: -1,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 0.9),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 0.9),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 0.9),
                    GenerationEfficiency: 0.9)
            ]);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsFailure);
        Assert.Contains("En15316.InvalidUsefulEnergy", result.Error);
    }

    [Fact]
    public void NonPositiveGenerationCop_ReturnsValidationFailure()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Cooling,
                    EnergyCarrier: En15316EnergyCarrier.Electricity,
                    GenerationTechnology: En15316GenerationTechnology.Chiller,
                    UsefulEnergyKWh: 1000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    GenerationCop: 0)
            ]);

        var result = _calculator.Calculate(input);

        Assert.True(result.IsFailure);
        Assert.Contains("En15316.InvalidGenerationCop", result.Error);
    }
}
