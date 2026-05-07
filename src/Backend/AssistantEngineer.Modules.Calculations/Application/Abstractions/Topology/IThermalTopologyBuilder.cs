using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;

public interface IThermalTopologyBuilder
{
    BuildingThermalTopology Build(ThermalTopologyBuildInput input);
}
