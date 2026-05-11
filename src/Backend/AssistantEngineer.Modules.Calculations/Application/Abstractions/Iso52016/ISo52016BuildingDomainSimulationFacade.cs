using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016BuildingDomainSimulationFacade
{
    Result<Iso52016BuildingDomainSimulationFacadeResult> Simulate(
        Iso52016BuildingDomainSimulationFacadeRequest request);
}