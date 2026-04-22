using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Facades;

public sealed class EquipmentCatalogFacade : IEquipmentCatalogFacade
{
    private readonly CoolingEquipmentCatalogCommandService _command;
    private readonly CoolingEquipmentCatalogQueryService _query;

    public EquipmentCatalogFacade(
        CoolingEquipmentCatalogCommandService command,
        CoolingEquipmentCatalogQueryService query)
    {
        _command = command;
        _query = query;
    }

    public Task<Result<EquipmentCatalogItemResponse>> CreateAsync(
        CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken) =>
        _command.CreateAsync(request, cancellationToken);

    public Task<Result<EquipmentCatalogItemResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _query.GetByIdAsync(id, cancellationToken);

    public Task<Result<List<EquipmentCatalogItemResponse>>> GetAllAsync(CancellationToken cancellationToken) =>
        _query.GetAllAsync(cancellationToken);
}
