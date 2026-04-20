using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Application;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Rooms;

public class RoomCommandService
{
    private readonly IFloorRepository _floors;
    private readonly IRoomRepository _rooms;
    private readonly IAppDbContext _context;
    private readonly ILogger<RoomCommandService> _logger;

    public RoomCommandService(
        IFloorRepository floors,
        IRoomRepository rooms,
        IAppDbContext context,
        ILogger<RoomCommandService>? logger = null)
    {
        _floors = floors;
        _rooms = rooms;
        _context = context;
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

        var outdoorTempResult = Temperature.FromCelsius(request.OutdoorTemperatureC);
        if (outdoorTempResult.IsFailure)
        {
            _logger.LogWarning("Room creation failed for floor {FloorId}: {Error}.", request.FloorId, outdoorTempResult.Error);
            return Result<RoomResponse>.Failure(outdoorTempResult);
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
            outdoorTempResult.Value,
            request.PeopleCount,
            equipLoad.Value,
            lightLoad.Value,
            request.Type.ToDomain());

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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created room {RoomId} for floor {FloorId}.", roomResult.Value.Id, request.FloorId);
        return Result<RoomResponse>.Success(ApplicationMapper.ToResponse(roomResult.Value));
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
            request.Orientation.ToDomain(),
            shadingResult.Value);
        if (addResult.IsFailure)
        {
            _logger.LogWarning("Window creation failed for room {RoomId}: {Error}.", roomId, addResult.Error);
            return Result<WindowResponse>.Failure(addResult);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added window {WindowId} to room {RoomId}.", addResult.Value.Id, roomId);
        return Result<WindowResponse>.Success(ApplicationMapper.ToResponse(addResult.Value));
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

        var addResult = room.AddWall(areaResult.Value, request.IsExternal, uValueResult.Value, request.Orientation.ToDomain());
        if (addResult.IsFailure)
        {
            _logger.LogWarning("Wall creation failed for room {RoomId}: {Error}.", roomId, addResult.Error);
            return Result<WallResponse>.Failure(addResult);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added wall {WallId} to room {RoomId}.", addResult.Value.Id, roomId);
        return Result<WallResponse>.Success(ApplicationMapper.ToResponse(addResult.Value));
    }
}
