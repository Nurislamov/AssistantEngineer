using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316SystemEnergyApplicationAdapter
{
    public En15316SystemEnergyInput MapToEn15316Input(
        SystemEnergyInput input,
        SystemEnergyOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);

        var endUses = new List<En15316SystemEnergyEndUseInput>();

        if (input.UsefulHeatingEnergyKWh > 0)
        {
            var heatingEfficiency = input.HeatingEfficiency is > 0
                ? input.HeatingEfficiency
                : null;
            var heatingCop = heatingEfficiency.HasValue
                ? null
                : (input.HeatingCop is > 0 ? input.HeatingCop : null);

            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.Heating,
                EnergyCarrier: options.DefaultHeatingCarrier,
                GenerationTechnology: options.DefaultHeatingTechnology,
                UsefulEnergyKWh: input.UsefulHeatingEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationEfficiency: heatingEfficiency,
                GenerationCop: heatingCop,
                AuxiliaryEnergyKWh: 0,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        if (input.UsefulCoolingEnergyKWh > 0)
        {
            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.Cooling,
                EnergyCarrier: options.DefaultCoolingCarrier,
                GenerationTechnology: options.DefaultCoolingTechnology,
                UsefulEnergyKWh: input.UsefulCoolingEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationCop: input.CoolingCop is > 0 ? input.CoolingCop : null,
                AuxiliaryEnergyKWh: 0,
                RecoveredLossFraction: 0,
                PrimaryEnergyFactor: input.PrimaryEnergyFactor,
                DiagnosticsContext: input.DiagnosticsContext));
        }

        if (input.UsefulDhwEnergyKWh > 0)
        {
            var dhwEfficiency = input.DhwEfficiency is > 0
                ? input.DhwEfficiency
                : null;
            var dhwCop = dhwEfficiency.HasValue
                ? null
                : (input.DhwCop is > 0 ? input.DhwCop : null);

            endUses.Add(new En15316SystemEnergyEndUseInput(
                EndUse: En15316EndUse.DomesticHotWater,
                EnergyCarrier: options.DefaultDhwCarrier,
                GenerationTechnology: options.DefaultDhwTechnology,
                UsefulEnergyKWh: input.UsefulDhwEnergyKWh,
                Emission: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Distribution: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                Storage: new En15316SystemEnergyModuleInput(Efficiency: 1.0),
                GenerationEfficiency: dhwEfficiency,
                GenerationCop: dhwCop,
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

    public SystemEnergyResult MapToSystemEnergyResult(
        En15316SystemEnergyResult input,
        SystemEnergyInput source)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(source);

        var finalHeating = input.EndUses
            .Where(item => item.EndUse == En15316EndUse.Heating)
            .Sum(item => item.FinalEnergyKWh);
        var finalCooling = input.EndUses
            .Where(item => item.EndUse == En15316EndUse.Cooling)
            .Sum(item => item.FinalEnergyKWh);
        var finalDhw = input.EndUses
            .Where(item => item.EndUse == En15316EndUse.DomesticHotWater)
            .Sum(item => item.FinalEnergyKWh);
        var finalFan = input.EndUses
            .Where(item => item.EndUse is En15316EndUse.VentilationFans or En15316EndUse.Auxiliary)
            .Sum(item => item.FinalEnergyKWh);

        var diagnostics = input.Diagnostics
            .Select(item => new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                item.Code,
                item.Message,
                source.DiagnosticsContext))
            .ToArray();

        return new SystemEnergyResult(
            UsefulHeatingKWh: Round(Math.Max(0, source.UsefulHeatingEnergyKWh)),
            UsefulCoolingKWh: Round(Math.Max(0, source.UsefulCoolingEnergyKWh)),
            UsefulDhwKWh: Round(Math.Max(0, source.UsefulDhwEnergyKWh)),
            FinalHeatingEnergyKWh: Round(finalHeating),
            FinalCoolingEnergyKWh: Round(finalCooling),
            FinalDhwEnergyKWh: Round(finalDhw),
            FinalFanEnergyKWh: Round(finalFan),
            TotalFinalEnergyKWh: Round(input.TotalFinalEnergyKWh),
            PrimaryEnergyKWh: source.PrimaryEnergyFactor.HasValue ? Round(input.TotalPrimaryEnergyKWh) : null,
            Diagnostics: diagnostics,
            AssumptionsUsed: input.AssumptionsUsed);
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
