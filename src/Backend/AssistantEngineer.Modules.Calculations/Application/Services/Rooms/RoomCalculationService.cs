using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Rooms;

/// <summary>
/// Compatibility room-load service kept for controlled transition.
/// </summary>
/// <remarks>
/// Deprecation marker (documentation-level only): first-party API controllers and load facade use
/// <c>EnergyCalculationPipelineService</c> as the active production path.
/// Keep this service until all compatibility tests and DI consumers are migrated.
/// </remarks>
public class RoomCalculationService
{
    private readonly IRoomRepository _rooms;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IRoomCoolingLoadCalculator _coolingLoadCalculator;
    private readonly IRoomHeatingLoadCalculator _heatingLoadCalculator;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<RoomCalculationService> _logger;

    public RoomCalculationService(
        IRoomRepository rooms,
        ICalculationPreferencesRepository preferences,
        IRoomCoolingLoadCalculator coolingLoadCalculator,
        IRoomHeatingLoadCalculator heatingLoadCalculator,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<RoomCalculationService>? logger = null)
    {
        _rooms = rooms;
        _preferences = preferences;
        _coolingLoadCalculator = coolingLoadCalculator;
        _heatingLoadCalculator = heatingLoadCalculator;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<RoomCalculationService>.Instance;
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
        var result = await _coolingLoadCalculator.CalculateAsync(room, method, preferences, cancellationToken);

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

    private static Result ValidateHeatingLoadData(Room room)
    {
        if (room.Floor.Building.ClimateZone is null)
            return Result.Validation("Building climate zone is required for EN 12831 heating load calculation.");

        return Result.Success();
    }
}

