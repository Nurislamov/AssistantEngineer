using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Services.Comfort;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class BuildingComfortAnalysisFacade : IBuildingComfortAnalysisFacade
{
    private readonly BuildingComfortMetricsService _buildingService;
    private readonly BuildingZoneComfortMetricsService _zoneService;
    private readonly BuildingRoomComfortMetricsService _roomService;

    public BuildingComfortAnalysisFacade(
        BuildingComfortMetricsService buildingService,
        BuildingZoneComfortMetricsService zoneService,
        BuildingRoomComfortMetricsService roomService)
    {
        _buildingService = buildingService;
        _zoneService = zoneService;
        _roomService = roomService;
    }

    public Task<Result<BuildingComfortMetricsResponse>> CalculateMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _buildingService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingZoneComfortMetricsResponse>> CalculateZoneMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _zoneService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingRoomComfortMetricsResponse>> CalculateRoomMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _roomService.CalculateAsync(buildingId, year, request, cancellationToken);
}