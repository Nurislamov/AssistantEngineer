using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public class RoomCommandService
{
    private readonly IFloorRepository _floors;
    private readonly IRoomRepository _rooms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomCommandService> _logger;

    public RoomCommandService(
        IFloorRepository floors,
        IRoomRepository rooms,
        IUnitOfWork unitOfWork,
        ILogger<RoomCommandService>? logger = null)
    {
        _floors = floors;
        _rooms = rooms;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<RoomCommandService>.Instance;
    }

    public async Task<Result<RoomResponse>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating room with name {RoomName} for floor {FloorId}.", request.Name, request.FloorId);

        var floor = await _floors.GetWithRoomsAsync(request.FloorId, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Cannot create room because floor {FloorId} was not found.", request.FloorId);
            return Result<RoomResponse>.NotFound($"Floor with id {request.FloorId} not found.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
        {
            _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, areaResult.Error);
            return Result<RoomResponse>.Failure(areaResult);
        }

        var indoorTempResult = Temperature.FromCelsius(request.IndoorTemperatureC);
        if (indoorTempResult.IsFailure)
        {
            _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, indoorTempResult.Error);
            return Result<RoomResponse>.Failure(indoorTempResult);
        }

        Temperature? outdoorTemperatureOverride = null;
        if (request.OutdoorTemperatureOverrideC.HasValue)
        {
            var outdoorTempResult = Temperature.FromCelsius(request.OutdoorTemperatureOverrideC.Value);
            if (outdoorTempResult.IsFailure)
            {
                _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, outdoorTempResult.Error);
                return Result<RoomResponse>.Failure(outdoorTempResult);
            }

            outdoorTemperatureOverride = outdoorTempResult.Value;
        }

        var equipLoad = Power.FromWatts(request.EquipmentLoadW);
        if (equipLoad.IsFailure)
        {
            _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, equipLoad.Error);
            return Result<RoomResponse>.Failure(equipLoad);
        }

        var lightLoad = Power.FromWatts(request.LightingLoadW);
        if (lightLoad.IsFailure)
        {
            _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, lightLoad.Error);
            return Result<RoomResponse>.Failure(lightLoad);
        }

        var roomResult = floor.AddRoom(
            request.Name,
            areaResult.Value,
            request.HeightM,
            indoorTempResult.Value,
            outdoorTemperatureOverride,
            request.PeopleCount,
            equipLoad.Value,
            lightLoad.Value,
            BuildingsContractEnumMapper.ToDomain(request.Type));

        if (roomResult.IsFailure)
        {
            _logger.LogWarning(
                "Room creation failed for floor {FloorId} with name {RoomName}: {Error}.",
                request.FloorId,
                request.Name,
                roomResult.Error);
            return Result<RoomResponse>.Failure(roomResult);
        }

        _rooms.Add(roomResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created room {RoomId} for floor {FloorId}.", roomResult.Value.Id, request.FloorId);
        return Result<RoomResponse>.Success(BuildingsMapper.ToResponse(roomResult.Value));
    }

    public async Task<Result<WindowResponse>> AddWindowAsync(
        int roomId,
        CreateWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding window to room {RoomId}.", roomId);

        var room = await _rooms.GetWithWindowsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot add window because room {RoomId} was not found.", roomId);
            return Result<WindowResponse>.NotFound($"Room with id {roomId} not found.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, areaResult.Error);
            return Result<WindowResponse>.Failure(areaResult);
        }

        var uValueResult = ThermalTransmittance.FromValue(request.UValue);
        if (uValueResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, uValueResult.Error);
            return Result<WindowResponse>.Failure(uValueResult);
        }

        var shgcResult = SolarHeatGainCoefficient.FromValue(request.Shgc);
        if (shgcResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, shgcResult.Error);
            return Result<WindowResponse>.Failure(shgcResult);
        }

        var shadingResult = WindowShadingParameters.Create(
            request.Shading.OverhangDepthM,
            request.Shading.SideFinDepthM,
            request.Shading.RevealDepthM,
            request.Shading.WindowHeightM,
            request.Shading.WindowWidthM,
            request.Shading.MinimumDirectSolarReductionFactor,
            request.Shading.DiffuseSolarShareUnaffected);

        if (shadingResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, shadingResult.Error);
            return Result<WindowResponse>.Failure(shadingResult);
        }

        var addResult = room.AddWindow(
            areaResult.Value,
            uValueResult.Value,
            shgcResult.Value,
            BuildingsContractEnumMapper.ToDomain(request.Orientation),
            shadingResult.Value);

        if (addResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, addResult.Error);
            return Result<WindowResponse>.Failure(addResult);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added window {WindowId} to room {RoomId}.", addResult.Value.Id, roomId);
        return Result<WindowResponse>.Success(BuildingsMapper.ToResponse(addResult.Value));
    }

    public async Task<Result<WallResponse>> AddWallAsync(
        int roomId,
        CreateWallRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding wall to room {RoomId}.", roomId);

        var room = await _rooms.GetWithWallsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot add wall because room {RoomId} was not found.", roomId);
            return Result<WallResponse>.NotFound($"Room with id {roomId} not found.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
        {
            _logger.LogWarning("Wall creation failed for room {RoomId}: {Error}.", roomId, areaResult.Error);
            return Result<WallResponse>.Failure(areaResult);
        }

        var uValueResult = ThermalTransmittance.FromValue(request.UValue);
        if (uValueResult.IsFailure)
        {
            _logger.LogWarning("Wall creation failed for room {RoomId}: {Error}.", roomId, uValueResult.Error);
            return Result<WallResponse>.Failure(uValueResult);
        }

        Room? adjacentRoom = null;
        var boundaryType = request.BoundaryType.ToDomain();

        if (boundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
        {
            if (!request.AdjacentRoomId.HasValue)
                return Result<WallResponse>.Validation("AdjacentRoomId is required for adjacent wall boundary types.");

            adjacentRoom = await _rooms.GetByIdAsync(request.AdjacentRoomId.Value, cancellationToken);
            if (adjacentRoom is null)
            {
                _logger.LogWarning("Wall creation failed because adjacent room {AdjacentRoomId} was not found.", request.AdjacentRoomId.Value);
                return Result<WallResponse>.NotFound($"Adjacent room with id {request.AdjacentRoomId.Value} not found.");
            }

            if (adjacentRoom.Id == room.Id)
                return Result<WallResponse>.Validation("A wall cannot reference the same room as its adjacent room.");

            var sourceFloor = await _floors.GetByIdAsync(room.FloorId, cancellationToken);
            var adjacentFloor = await _floors.GetByIdAsync(adjacentRoom.FloorId, cancellationToken);
            if (sourceFloor is null || adjacentFloor is null)
                return Result<WallResponse>.Validation("Unable to validate adjacent room building ownership.");

            if (sourceFloor.BuildingId != adjacentFloor.BuildingId)
                return Result<WallResponse>.Validation("Adjacent room must belong to the same building.");
        }

        var addResult = room.AddWall(
            areaResult.Value,
            uValueResult.Value,
            BuildingsContractEnumMapper.ToDomain(request.Orientation),
            boundaryType,
            adjacentRoom);

        if (addResult.IsFailure)
        {
            _logger.LogWarning("Wall creation failed for room {RoomId}: {Error}.", roomId, addResult.Error);
            return Result<WallResponse>.Failure(addResult);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added wall {WallId} to room {RoomId}.", addResult.Value.Id, roomId);
        return Result<WallResponse>.Success(BuildingsMapper.ToResponse(addResult.Value));
    }
}