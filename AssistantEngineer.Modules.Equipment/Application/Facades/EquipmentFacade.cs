using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Facades;

public sealed class EquipmentFacade : IEquipmentFacade
{
    private readonly CoolingEquipmentCatalogCommandService _catalogCommand;
    private readonly CoolingEquipmentCatalogQueryService _catalogQuery;
    private readonly EquipmentSelectionService _equipmentSelection;

    public EquipmentFacade(
        CoolingEquipmentCatalogCommandService catalogCommand,
        CoolingEquipmentCatalogQueryService catalogQuery,
        EquipmentSelectionService equipmentSelection)
    {
        _catalogCommand = catalogCommand;
        _catalogQuery = catalogQuery;
        _equipmentSelection = equipmentSelection;
    }

    public Task<Result<EquipmentCatalogItemResponse>> CreateCatalogItemAsync(
        CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken) =>
        _catalogCommand.CreateAsync(request, cancellationToken);

    public Task<Result<EquipmentCatalogItemResponse>> GetCatalogItemByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _catalogQuery.GetByIdAsync(id, cancellationToken);

    public Task<Result<List<EquipmentCatalogItemResponse>>> GetCatalogItemsAsync(
        CancellationToken cancellationToken) =>
        _catalogQuery.GetAllAsync(cancellationToken);

    public Task<Result<EquipmentSelectionResult>> SelectRoomEquipmentAsync(
        int roomId,
        EquipmentSelectionRequest request,
        double totalHeatLoadKw,
        double designCapacityKw,
        CancellationToken cancellationToken) =>
        _equipmentSelection.SelectForRoomAsync(
            roomId,
            request,
            totalHeatLoadKw,
            designCapacityKw,
            cancellationToken);
}