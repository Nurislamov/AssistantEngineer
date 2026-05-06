using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyCarrierAggregationTests
{
    private readonly En15316SystemEnergyChainCalculator _calculator = new(
        new En15316SystemEnergyReferenceDataProvider());

    [Fact]
    public void AggregatesFinalAndPrimaryEnergy_ByCarrierAndEndUse()
    {
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Heating,
                    EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                    GenerationTechnology: En15316GenerationTechnology.Boiler,
                    UsefulEnergyKWh: 8000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    GenerationEfficiency: 0.8,
                    AuxiliaryEnergyKWh: 100,
                    PrimaryEnergyFactor: 1.1),
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.DomesticHotWater,
                    EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                    GenerationTechnology: En15316GenerationTechnology.CondensingBoiler,
                    UsefulEnergyKWh: 2000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    GenerationEfficiency: 0.9,
                    AuxiliaryEnergyKWh: 20,
                    PrimaryEnergyFactor: 1.1),
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Cooling,
                    EnergyCarrier: En15316EnergyCarrier.Electricity,
                    GenerationTechnology: En15316GenerationTechnology.Chiller,
                    UsefulEnergyKWh: 3000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                    GenerationCop: 3.0,
                    AuxiliaryEnergyKWh: 30,
                    PrimaryEnergyFactor: 2.2)
            ]);

        var result = _calculator.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);

        Assert.Equal(10100, result.Value.FinalEnergyByEndUseKWh[En15316EndUse.Heating], precision: 6);
        Assert.Equal(2242.222222, result.Value.FinalEnergyByEndUseKWh[En15316EndUse.DomesticHotWater], precision: 6);
        Assert.Equal(1030, result.Value.FinalEnergyByEndUseKWh[En15316EndUse.Cooling], precision: 6);

        Assert.Equal(12342.222222, result.Value.FinalEnergyByCarrierKWh[En15316EnergyCarrier.NaturalGas], precision: 6);
        Assert.Equal(1030, result.Value.FinalEnergyByCarrierKWh[En15316EnergyCarrier.Electricity], precision: 6);

        Assert.Equal(13576.444444, result.Value.PrimaryEnergyByCarrierKWh[En15316EnergyCarrier.NaturalGas], precision: 6);
        Assert.Equal(2266, result.Value.PrimaryEnergyByCarrierKWh[En15316EnergyCarrier.Electricity], precision: 6);

        Assert.Equal(13372.222222, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(15842.444444, result.Value.TotalPrimaryEnergyKWh, precision: 6);
    }
}
