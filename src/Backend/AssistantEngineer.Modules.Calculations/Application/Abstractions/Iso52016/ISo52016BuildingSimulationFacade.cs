using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016BuildingSimulationFacade
{
    Result<Iso52016BuildingSimulationFacadeResult> Simulate(
        Iso52016BuildingSimulationFacadeRequest request);
}