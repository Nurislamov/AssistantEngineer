using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public class RoomQueryService
{
    private readonly IRoomRepository _rooms;
    private readonly ILogger<RoomQueryService> _logger;

    public RoomQueryService(
        IRoomRepository rooms,
        ILogger<RoomQueryService>? logger = null)
    {
        _rooms = rooms;
        _logger = logger ?? NullLogger<RoomQueryService>.Instance;
    }

    public async Task<Result<List<RoomResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.ListAsync(cancellationToken);
        _logger.LogDebug("Loaded {RoomCount} rooms.", rooms.Count);
        return Result<List<RoomResponse>>.Success(rooms.Select(BuildingsMapper.ToResponse).ToList());
    }

    public async Task<Result<RoomResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(id, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Room {RoomId} was not found.", id);
            return Result<RoomResponse>.NotFound($"Room with id {id} not found.");
        }

        _logger.LogDebug("Loaded room {RoomId}.", id);
        return Result<RoomResponse>.Success(BuildingsMapper.ToResponse(room));
    }

    public async Task<Result<List<WindowResponse>>> GetWindowsAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var roomExists = await _rooms.ExistsAsync(roomId, cancellationToken);
        if (!roomExists)
        {
            _logger.LogWarning("Cannot load windows because room {RoomId} was not found.", roomId);
            return Result<List<WindowResponse>>.NotFound($"Room with id {roomId} not found.");
        }

        var windows = await _rooms.ListWindowsAsync(roomId, cancellationToken);
        _logger.LogDebug("Loaded {WindowCount} windows for room {RoomId}.", windows.Count, roomId);
        return Result<List<WindowResponse>>.Success(windows.Select(BuildingsMapper.ToResponse).ToList());
    }

    public async Task<Result<List<WallResponse>>> GetWallsAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var roomExists = await _rooms.ExistsAsync(roomId, cancellationToken);
        if (!roomExists)
        {
            _logger.LogWarning("Cannot load walls because room {RoomId} was not found.", roomId);
            return Result<List<WallResponse>>.NotFound($"Room with id {roomId} not found.");
        }

        var walls = await _rooms.ListWallsAsync(roomId, cancellationToken);
        _logger.LogDebug("Loaded {WallCount} walls for room {RoomId}.", walls.Count, roomId);
        return Result<List<WallResponse>>.Success(walls.Select(BuildingsMapper.ToResponse).ToList());
    }
}