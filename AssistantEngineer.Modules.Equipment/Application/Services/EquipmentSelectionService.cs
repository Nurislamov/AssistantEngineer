using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public class EquipmentSelectionService
{
    private readonly IEquipmentCatalogRepository _catalog;
    private readonly ICoolingEquipmentSelector _selector;
    private readonly ILogger<EquipmentSelectionService> _logger;

    public EquipmentSelectionService(
        IEquipmentCatalogRepository catalog,
        ICoolingEquipmentSelector selector,
        ILogger<EquipmentSelectionService>? logger = null)
    {
        _catalog = catalog;
        _selector = selector;
        _logger = logger ?? NullLogger<EquipmentSelectionService>.Instance;
    }

    public async Task<Result<EquipmentSelectionResult>> SelectForRoomAsync(
        int roomId,
        EquipmentSelectionRequest request,
        double totalHeatLoadKw,
        double designCapacityKw,
        CancellationToken cancellationToken = default)
    {
        if (roomId <= 0)
            return Result<EquipmentSelectionResult>.Validation("Room id must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.SystemType))
            return Result<EquipmentSelectionResult>.Validation("System type is required.");

        if (string.IsNullOrWhiteSpace(request.UnitType))
            return Result<EquipmentSelectionResult>.Validation("Unit type is required.");

        if (totalHeatLoadKw < 0)
            return Result<EquipmentSelectionResult>.Validation("Total heat load cannot be negative.");

        if (designCapacityKw <= 0)
            return Result<EquipmentSelectionResult>.Validation("Design capacity must be greater than zero.");

        _logger.LogInformation(
            "Selecting equipment for room {RoomId}, system {SystemType}, unit {UnitType}, design capacity {DesignCapacityKw} kW.",
            roomId,
            request.SystemType,
            request.UnitType,
            designCapacityKw);

        var catalog = await _catalog.ListActiveByTypeAsync(
            request.SystemType,
            request.UnitType,
            cancellationToken);

        _logger.LogInformation(
            "Loaded {CatalogItemCount} active catalog items for system {SystemType}, unit {UnitType}.",
            catalog.Count,
            request.SystemType,
            request.UnitType);

        var result = _selector.SelectForRoom(
            roomId,
            request.SystemType,
            request.UnitType,
            catalog,
            totalHeatLoadKw,
            designCapacityKw);

        if (result is null)
        {
            _logger.LogWarning(
                "No suitable equipment found for room {RoomId}, system {SystemType}, unit {UnitType}, design capacity {DesignCapacityKw} kW.",
                roomId,
                request.SystemType,
                request.UnitType,
                designCapacityKw);

            return Result<EquipmentSelectionResult>.Failure("No suitable equipment found.");
        }

        _logger.LogInformation(
            "Selected catalog item {CatalogItemId} for room {RoomId}.",
            result.SelectedCatalogItemId,
            roomId);

        return Result<EquipmentSelectionResult>.Success(result);
    }
}