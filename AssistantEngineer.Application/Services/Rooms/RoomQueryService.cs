using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Rooms;

public class RoomQueryService
{
    private readonly IRoomRepository _rooms;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IRoomCoolingLoadCalculator _calculator;
    private readonly IRoomHeatingLoadCalculator _heatingLoadCalculator;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<RoomQueryService> _logger;

    public RoomQueryService(
        IRoomRepository rooms,
        ICalculationPreferencesRepository preferences,
        IRoomCoolingLoadCalculator calculator,
        IRoomHeatingLoadCalculator heatingLoadCalculator,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<RoomQueryService>? logger = null)
    {
        _rooms = rooms;
        _preferences = preferences;
        _calculator = calculator;
        _heatingLoadCalculator = heatingLoadCalculator;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<RoomQueryService>.Instance;
    }

    public async Task<Result<List<RoomResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.ListAsync(cancellationToken);
        _logger.LogDebug("Loaded {RoomCount} rooms.", rooms.Count);
        return Result<List<RoomResponse>>.Success(rooms.Select(ApplicationMapper.ToResponse).ToList());
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
        return Result<RoomResponse>.Success(ApplicationMapper.ToResponse(room));
    }

    public async Task<Result<RoomCalculationResult>> CalculateAsync(
        int roomId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating cooling load for room {RoomId} using {CalculationMethod}.",
            roomId,
            method);

        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Cooling load calculation failed because room {RoomId} was not found.", roomId);
            return Result<RoomCalculationResult>.NotFound($"Room with id {roomId} not found.");
        }

        var validation = await _iso52016ClimateDataValidator.ValidateAsync(room, method, cancellationToken);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Cooling load validation failed for room {RoomId}: {Error}.",
                roomId,
                validation.Error);
            return Result<RoomCalculationResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(room.Floor.Building.ProjectId, cancellationToken);
        var result = await _calculator.CalculateAsync(room, method, preferences, cancellationToken);
        _logger.LogInformation(
            "Calculated cooling load for room {RoomId}: design capacity {DesignCapacityKw} kW.",
            roomId,
            result.DesignCapacityKw);
        return Result<RoomCalculationResult>.Success(result);
    }

    public async Task<Result<RoomHeatingLoadResult>> CalculateHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating heating load for room {RoomId} using {CalculationMethod}.",
            roomId,
            method);

        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Heating load calculation failed because room {RoomId} was not found.", roomId);
            return Result<RoomHeatingLoadResult>.NotFound($"Room with id {roomId} not found.");
        }

        var validation = ValidateHeatingLoadData(room);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Heating load validation failed for room {RoomId}: {Error}.",
                roomId,
                validation.Error);
            return Result<RoomHeatingLoadResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(room.Floor.Building.ProjectId, cancellationToken);
        var result = await _heatingLoadCalculator.CalculateAsync(room, method, preferences, cancellationToken);
        _logger.LogInformation(
            "Calculated heating load for room {RoomId}: design load {TotalDesignHeatingLoadKw} kW.",
            roomId,
            result.TotalDesignHeatingLoadKw);
        return Result<RoomHeatingLoadResult>.Success(result);
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
        return Result<List<WindowResponse>>.Success(windows.Select(ApplicationMapper.ToResponse).ToList());
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
        return Result<List<WallResponse>>.Success(walls.Select(ApplicationMapper.ToResponse).ToList());
    }

    private static Result ValidateHeatingLoadData(Room room)
    {
        if (room.Floor.Building.ClimateZone is null)
            return Result.Validation("Building climate zone is required for EN 12831 heating load calculation.");

        return Result.Success();
    }
}


