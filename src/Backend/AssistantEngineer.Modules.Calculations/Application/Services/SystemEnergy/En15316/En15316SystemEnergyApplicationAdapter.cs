using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316SystemEnergyApplicationAdapter
{
    public En15316SystemEnergyInput MapFromSystemEnergyInput(SystemEnergyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var endUses = new List<En15316SystemEnergyEndUseInput>();

        if (input.UsefulHeatingEnergyKWh > 0)
        {
            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.Heating,
                EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                GenerationTechnology: En15316GenerationTechnology.Boiler,
                UsefulEnergyKWh: input.UsefulHeatingEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationEfficiency: input.HeatingEfficiency,
                GenerationCop: input.HeatingCop,
                AuxiliaryEnergyKWh: 0,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        if (input.UsefulCoolingEnergyKWh > 0)
        {
            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.Cooling,
                EnergyCarrier: En15316EnergyCarrier.Electricity,
                GenerationTechnology: En15316GenerationTechnology.Chiller,
                UsefulEnergyKWh: input.UsefulCoolingEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationCop: input.CoolingCop,
                AuxiliaryEnergyKWh: 0,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        if (input.UsefulDhwEnergyKWh > 0)
        {
            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.DomesticHotWater,
                EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                GenerationTechnology: En15316GenerationTechnology.Boiler,
                UsefulEnergyKWh: input.UsefulDhwEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationEfficiency: input.DhwEfficiency,
                GenerationCop: input.DhwCop,
                AuxiliaryEnergyKWh: 0,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        if (input.FanEnergyKWh > 0)
        {
            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.VentilationFans,
                EnergyCarrier: En15316EnergyCarrier.Electricity,
                GenerationTechnology: En15316GenerationTechnology.DirectElectric,
                UsefulEnergyKWh: 0,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationEfficiency: 1.0,
                AuxiliaryEnergyKWh: input.FanEnergyKWh,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        return new En15316SystemEnergyInput(endUses, input.DiagnosticsContext);
    }
}
