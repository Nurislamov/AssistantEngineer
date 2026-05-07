using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;

public interface IThermalZoneBoundaryCalculator
{
    BuildingThermalBoundaryCalculationResult Calculate(ThermalZoneBoundaryCalculationInput input);
}
