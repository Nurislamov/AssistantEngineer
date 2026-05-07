using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;

public interface IThermalBoundaryConditionResolver
{
    ThermalBoundaryResolutionResult Resolve(
        ThermalTopologySurface surface,
        BuildingThermalTopology topology);
}
