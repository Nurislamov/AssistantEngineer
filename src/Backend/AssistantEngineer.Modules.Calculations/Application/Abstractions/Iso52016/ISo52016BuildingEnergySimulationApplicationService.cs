using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016BuildingEnergySimulationApplicationService
{
    Task<Result<Iso52016BuildingEnergySimulationApplicationResult>> SimulateAsync(
        Iso52016BuildingEnergySimulationApplicationRequest request,
        CancellationToken cancellationToken = default);
}