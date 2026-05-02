using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Facades;

public interface IEquipmentFacade
{
    Task<Result<EquipmentCatalogItemResponse>> CreateCatalogItemAsync(
        CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken);

    Task<Result<EquipmentCatalogItemResponse>> GetCatalogItemByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<EquipmentCatalogItemResponse>> UpdateCatalogItemAsync(
        int id,
        UpdateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken);

    Task<Result> DeactivateCatalogItemAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<List<EquipmentCatalogItemResponse>>> GetCatalogItemsAsync(
        CancellationToken cancellationToken);

    Task<Result<EquipmentSelectionResult>> SelectRoomEquipmentAsync(
        int roomId,
        EquipmentSelectionRequest request,
        double totalHeatLoadKw,
        double designCapacityKw,
        CancellationToken cancellationToken);
}
