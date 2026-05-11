using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class EngineeringWorkflowBulkInputQueryService
{
    private readonly IRoomRepository _rooms;
    private readonly ILogger<EngineeringWorkflowBulkInputQueryService> _logger;

    public EngineeringWorkflowBulkInputQueryService(
        IRoomRepository rooms,
        ILogger<EngineeringWorkflowBulkInputQueryService>? logger = null)
    {
        _rooms = rooms;
        _logger = logger ?? NullLogger<EngineeringWorkflowBulkInputQueryService>.Instance;
    }

    public async Task<Result<EngineeringWorkflowBulkInputResponse>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.ListWithEngineeringInputsByBuildingIdAsync(buildingId, cancellationToken);
        _logger.LogDebug(
            "Loaded engineering workflow bulk input for building {BuildingId} with {RoomCount} rooms.",
            buildingId,
            rooms.Count);

        var roomInputs = rooms
            .OrderBy(room => room.Id)
            .Select(room => new EngineeringWorkflowRoomInputResponse
            {
                RoomId = room.Id,
                RoomName = room.Name,
                HasVentilationParameters = room.VentilationParameters is not null,
                HasGroundContactMetadata = room.GroundContactMetadata is not null
            })
            .ToList();

        var walls = rooms
            .OrderBy(room => room.Id)
            .SelectMany(room => room.Walls.OrderBy(wall => wall.Id))
            .Select(BuildingsMapper.ToResponse)
            .ToList();

        var windows = rooms
            .OrderBy(room => room.Id)
            .SelectMany(room => room.Windows.OrderBy(window => window.Id))
            .Select(BuildingsMapper.ToResponse)
            .ToList();

        return Result<EngineeringWorkflowBulkInputResponse>.Success(new EngineeringWorkflowBulkInputResponse
        {
            Rooms = roomInputs,
            Walls = walls,
            Windows = windows,
            VentilationConfiguredRoomCount = roomInputs.Count(room => room.HasVentilationParameters),
            GroundConfiguredRoomCount = roomInputs.Count(room => room.HasGroundContactMetadata)
        });
    }
}
