using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IBuildingComfortAnalysisFacade
{
    Task<Result<BuildingComfortMetricsResponse>> CalculateMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingZoneComfortMetricsResponse>> CalculateZoneMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingRoomComfortMetricsResponse>> CalculateRoomMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);
}