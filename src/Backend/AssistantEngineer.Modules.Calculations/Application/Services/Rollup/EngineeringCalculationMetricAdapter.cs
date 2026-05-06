using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

public sealed class EngineeringCalculationMetricAdapter
{
    public IReadOnlyList<EngineeringCalculationModeMetric> MapGroundBoundary(
        GroundBoundaryCondition source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return
        [
            new EngineeringCalculationModeMetric("GroundHeatTransferWPerK", source.HeatTransferCoefficientWPerK),
            new EngineeringCalculationModeMetric("GroundWeight", source.GroundTemperatureWeight),
            new EngineeringCalculationModeMetric("OutdoorWeight", source.OutdoorTemperatureWeight),
            new EngineeringCalculationModeMetric("IndoorWeight", source.IndoorTemperatureWeight)
        ];
    }

    public IReadOnlyList<EngineeringCalculationModeMetric> MapDomesticHotWater(
        DomesticHotWaterDemandResult source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return
        [
            new EngineeringCalculationModeMetric("DhwEnergyKWh", source.AnnualEnergyKWh),
            new EngineeringCalculationModeMetric("DhwAnnualVolumeLiters", source.AnnualVolumeLiters)
        ];
    }

    public IReadOnlyList<EngineeringCalculationModeMetric> MapSystemEnergy(
        SystemEnergyResult source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return
        [
            new EngineeringCalculationModeMetric("SystemFinalEnergyKWh", source.TotalFinalEnergyKWh),
            new EngineeringCalculationModeMetric("SystemPrimaryEnergyKWh", source.PrimaryEnergyKWh ?? 0.0),
            new EngineeringCalculationModeMetric("HeatingLoadW", source.FinalHeatingEnergyKWh),
            new EngineeringCalculationModeMetric("CoolingLoadW", source.FinalCoolingEnergyKWh)
        ];
    }
}
