using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Facades;

public interface IEquipmentCatalogFacade
{
    Task<Result<EquipmentCatalogItemResponse>> CreateAsync(
        CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken);

    Task<Result<EquipmentCatalogItemResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<List<EquipmentCatalogItemResponse>>> GetAllAsync(CancellationToken cancellationToken);
}
