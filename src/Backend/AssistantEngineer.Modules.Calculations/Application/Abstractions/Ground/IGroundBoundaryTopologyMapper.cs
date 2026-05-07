using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundBoundaryTopologyMapper
{
    GroundBoundaryCalculationInput Map(
        BuildingThermalTopology topology,
        ThermalTopologySurface surface,
        GroundSurfaceMetadata metadata);
}
