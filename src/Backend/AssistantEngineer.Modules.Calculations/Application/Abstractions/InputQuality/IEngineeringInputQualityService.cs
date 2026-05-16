using AssistantEngineer.Modules.Calculations.Application.Contracts.InputQuality;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.InputQuality;

public interface IEngineeringInputQualityService
{
    Task<Result<EngineeringInputQualityReport>> CheckBuildingInputQualityAsync(
        int buildingId,
        CancellationToken cancellationToken = default);

    Task<Result<EngineeringInputQualityReport>> CheckRoomInputQualityAsync(
        int roomId,
        CancellationToken cancellationToken = default);
}
