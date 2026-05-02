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

    public async Task<Result<RoomResponse>> UpdateAsync(
        int roomId,
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating room {RoomId}.", roomId);

        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot update room because room {RoomId} was not found.", roomId);
            return Result<RoomResponse>.NotFound($"Room with id {roomId} not found.");
        }

        var floor = await _floors.GetWithRoomsAsync(room.FloorId, cancellationToken);
        if (floor is null)
            return Result<RoomResponse>.Validation("Unable to validate room floor ownership.");

        if (floor.Rooms.Any(existing =>
                existing.Id != roomId &&
                existing.Name.Equals((request.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result<RoomResponse>.Conflict($"Room with name '{request.Name}' already exists on this floor.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
            return Result<RoomResponse>.Failure(areaResult);

        var indoorTempResult = Temperature.FromCelsius(request.IndoorTemperatureC);
        if (indoorTempResult.IsFailure)
            return Result<RoomResponse>.Failure(indoorTempResult);

        Temperature? outdoorTemperatureOverride = null;
        if (request.OutdoorTemperatureOverrideC.HasValue)
        {
            var outdoorTempResult = Temperature.FromCelsius(request.OutdoorTemperatureOverrideC.Value);
            if (outdoorTempResult.IsFailure)
                return Result<RoomResponse>.Failure(outdoorTempResult);

            outdoorTemperatureOverride = outdoorTempResult.Value;
        }

        var equipmentLoadResult = Power.FromWatts(request.EquipmentLoadW);
        if (equipmentLoadResult.IsFailure)
            return Result<RoomResponse>.Failure(equipmentLoadResult);

        var lightingLoadResult = Power.FromWatts(request.LightingLoadW);
        if (lightingLoadResult.IsFailure)
            return Result<RoomResponse>.Failure(lightingLoadResult);

        var updateNameResult = room.UpdateName(request.Name);
        if (updateNameResult.IsFailure)
            return Result<RoomResponse>.Failure(updateNameResult);

        var updateAreaResult = room.UpdateArea(areaResult.Value);
        if (updateAreaResult.IsFailure)
            return Result<RoomResponse>.Failure(updateAreaResult);

        var updateHeightResult = room.UpdateHeight(request.HeightM);
        if (updateHeightResult.IsFailure)
            return Result<RoomResponse>.Failure(updateHeightResult);

        var updateIndoorResult = room.UpdateIndoorTemperature(indoorTempResult.Value);
        if (updateIndoorResult.IsFailure)
            return Result<RoomResponse>.Failure(updateIndoorResult);

        var updateOutdoorResult = room.UpdateOutdoorTemperatureOverride(outdoorTemperatureOverride);
        if (updateOutdoorResult.IsFailure)
            return Result<RoomResponse>.Failure(updateOutdoorResult);

        var updatePeopleResult = room.UpdatePeopleCount(request.PeopleCount);
        if (updatePeopleResult.IsFailure)
            return Result<RoomResponse>.Failure(updatePeopleResult);

        var updateEquipmentResult = room.UpdateEquipmentLoad(equipmentLoadResult.Value);
        if (updateEquipmentResult.IsFailure)
            return Result<RoomResponse>.Failure(updateEquipmentResult);

        var updateLightingResult = room.UpdateLightingLoad(lightingLoadResult.Value);
        if (updateLightingResult.IsFailure)
            return Result<RoomResponse>.Failure(updateLightingResult);

        var updateTypeResult = room.UpdateType(BuildingsContractEnumMapper.ToDomain(request.Type));
        if (updateTypeResult.IsFailure)
            return Result<RoomResponse>.Failure(updateTypeResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated room {RoomId}.", roomId);
        return Result<RoomResponse>.Success(BuildingsMapper.ToResponse(room));
    }

    public async Task<Result> DeleteAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting room {RoomId}.", roomId);

        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot delete room because room {RoomId} was not found.", roomId);
            return Result.NotFound($"Room with id {roomId} not found.");
        }

        _rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted room {RoomId}.", roomId);
        return Result.Success();
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

    public async Task<Result<WindowResponse>> UpdateWindowAsync(
        int roomId,
        int windowId,
        UpdateWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating window {WindowId} in room {RoomId}.", windowId, roomId);

        var room = await _rooms.GetWithWindowsAndWallsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot update window because room {RoomId} was not found.", roomId);
            return Result<WindowResponse>.NotFound($"Room with id {roomId} not found.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
            return Result<WindowResponse>.Failure(areaResult);

        var uValueResult = ThermalTransmittance.FromValue(request.UValue);
        if (uValueResult.IsFailure)
            return Result<WindowResponse>.Failure(uValueResult);

        var shgcResult = SolarHeatGainCoefficient.FromValue(request.Shgc);
        if (shgcResult.IsFailure)
            return Result<WindowResponse>.Failure(shgcResult);

        var shadingResult = WindowShadingParameters.Create(
            request.Shading.OverhangDepthM,
            request.Shading.SideFinDepthM,
            request.Shading.RevealDepthM,
            request.Shading.WindowHeightM,
            request.Shading.WindowWidthM,
            request.Shading.MinimumDirectSolarReductionFactor,
            request.Shading.DiffuseSolarShareUnaffected);
        if (shadingResult.IsFailure)
            return Result<WindowResponse>.Failure(shadingResult);

        var updateResult = room.UpdateWindow(
            windowId,
            areaResult.Value,
            uValueResult.Value,
            shgcResult.Value,
            BuildingsContractEnumMapper.ToDomain(request.Orientation),
            shadingResult.Value);
        if (updateResult.IsFailure)
            return Result<WindowResponse>.Failure(updateResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated window {WindowId} in room {RoomId}.", windowId, roomId);
        return Result<WindowResponse>.Success(BuildingsMapper.ToResponse(updateResult.Value));
    }

    public async Task<Result> DeleteWindowAsync(
        int roomId,
        int windowId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting window {WindowId} from room {RoomId}.", windowId, roomId);

        var room = await _rooms.GetWithWindowsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot delete window because room {RoomId} was not found.", roomId);
            return Result.NotFound($"Room with id {roomId} not found.");
        }

        var window = room.Windows.FirstOrDefault(item => item.Id == windowId);
        if (window is null)
            return Result.NotFound($"Window with id {windowId} not found.");

        var removeResult = room.RemoveWindow(windowId);
        if (removeResult.IsFailure)
            return removeResult;

        _rooms.RemoveWindow(window);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted window {WindowId} from room {RoomId}.", windowId, roomId);
        return Result.Success();
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

    public async Task<Result<WallResponse>> UpdateWallAsync(
        int roomId,
        int wallId,
        UpdateWallRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating wall {WallId} in room {RoomId}.", wallId, roomId);

        var room = await _rooms.GetWithWallsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot update wall because room {RoomId} was not found.", roomId);
            return Result<WallResponse>.NotFound($"Room with id {roomId} not found.");
        }

        var areaResult = Area.FromSquareMeters(request.AreaM2);
        if (areaResult.IsFailure)
            return Result<WallResponse>.Failure(areaResult);

        var uValueResult = ThermalTransmittance.FromValue(request.UValue);
        if (uValueResult.IsFailure)
            return Result<WallResponse>.Failure(uValueResult);

        Room? adjacentRoom = null;
        var boundaryType = request.BoundaryType.ToDomain();

        if (boundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
        {
            if (!request.AdjacentRoomId.HasValue)
                return Result<WallResponse>.Validation("AdjacentRoomId is required for adjacent wall boundary types.");

            adjacentRoom = await _rooms.GetByIdAsync(request.AdjacentRoomId.Value, cancellationToken);
            if (adjacentRoom is null)
            {
                _logger.LogWarning("Wall update failed because adjacent room {AdjacentRoomId} was not found.", request.AdjacentRoomId.Value);
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

        var updateResult = room.UpdateWall(
            wallId,
            areaResult.Value,
            uValueResult.Value,
            BuildingsContractEnumMapper.ToDomain(request.Orientation),
            boundaryType,
            adjacentRoom);
        if (updateResult.IsFailure)
            return Result<WallResponse>.Failure(updateResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated wall {WallId} in room {RoomId}.", wallId, roomId);
        return Result<WallResponse>.Success(BuildingsMapper.ToResponse(updateResult.Value));
    }

    public async Task<Result> DeleteWallAsync(
        int roomId,
        int wallId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting wall {WallId} from room {RoomId}.", wallId, roomId);

        var room = await _rooms.GetWithWallsAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cannot delete wall because room {RoomId} was not found.", roomId);
            return Result.NotFound($"Room with id {roomId} not found.");
        }

        var wall = room.Walls.FirstOrDefault(item => item.Id == wallId);
        if (wall is null)
            return Result.NotFound($"Wall with id {wallId} not found.");

        var removeResult = room.RemoveWall(wallId);
        if (removeResult.IsFailure)
            return removeResult;

        _rooms.RemoveWall(wall);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted wall {WallId} from room {RoomId}.", wallId, roomId);
        return Result.Success();
    }
}
