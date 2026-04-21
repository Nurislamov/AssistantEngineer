using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.Modules.Equipment.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public class EquipmentSelectionService
{
    private readonly IRoomRepository _rooms;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IEquipmentCatalogRepository _catalog;
    private readonly ICoolingEquipmentSelector _selector;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<EquipmentSelectionService> _logger;

    public EquipmentSelectionService(
        IRoomRepository rooms,
        ICalculationPreferencesRepository preferences,
        IEquipmentCatalogRepository catalog,
        ICoolingEquipmentSelector selector,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<EquipmentSelectionService>? logger = null)
    {
        _rooms = rooms;
        _preferences = preferences;
        _catalog = catalog;
        _selector = selector;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<EquipmentSelectionService>.Instance;
    }

    public async Task<Result<EquipmentSelectionResult>> SelectForRoomAsync(
        int roomId,
        EquipmentSelectionRequest request,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Selecting equipment for room {RoomId} using {CalculationMethod}, system {SystemType}, unit {UnitType}.",
            roomId,
            method,
            request.SystemType,
            request.UnitType);

        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Equipment selection failed because room {RoomId} was not found.", roomId);
            return Result<EquipmentSelectionResult>.NotFound($"Room with id {roomId} not found.");
        }

        var validation = await _iso52016ClimateDataValidator.ValidateAsync(room, method, cancellationToken);
        if (validation.IsFailure)
        {
            _logger.LogWarning("Equipment selection validation failed for room {RoomId}: {Error}.", roomId, validation.Error);
            return Result<EquipmentSelectionResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(room.Floor.Building.ProjectId, cancellationToken);
        var catalog = await _catalog.ListActiveByTypeAsync(request.SystemType, request.UnitType, cancellationToken);
        _logger.LogInformation(
            "Loaded {CatalogItemCount} active catalog items for system {SystemType}, unit {UnitType}.",
            catalog.Count,
            request.SystemType,
            request.UnitType);

        var result = await _selector.SelectForRoomAsync(
            room,
            request.SystemType,
            request.UnitType,
            catalog,
            method,
            preferences,
            cancellationToken);
        if (result is null)
        {
            _logger.LogWarning(
                "No suitable equipment found for room {RoomId}, system {SystemType}, unit {UnitType}.",
                roomId,
                request.SystemType,
                request.UnitType);
            return Result<EquipmentSelectionResult>.Failure("No suitable equipment found.");
        }

        _logger.LogInformation(
            "Selected catalog item {CatalogItemId} for room {RoomId}.",
            result.SelectedCatalogItemId,
            roomId);
        return Result<EquipmentSelectionResult>.Success(result);
    }
}


