using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Mappers;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public class CoolingEquipmentCatalogQueryService
{
    private readonly IEquipmentCatalogRepository _catalog;
    private readonly ILogger<CoolingEquipmentCatalogQueryService> _logger;

    public CoolingEquipmentCatalogQueryService(
        IEquipmentCatalogRepository catalog,
        ILogger<CoolingEquipmentCatalogQueryService>? logger = null)
    {
        _catalog = catalog;
        _logger = logger ?? NullLogger<CoolingEquipmentCatalogQueryService>.Instance;
    }

    public async Task<Result<EquipmentCatalogItemResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await _catalog.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("Cooling equipment catalog item {CatalogItemId} was not found.", id);
            return Result<EquipmentCatalogItemResponse>.NotFound($"Catalog item with id {id} not found.");
        }

        _logger.LogDebug("Loaded cooling equipment catalog item {CatalogItemId}.", id);
        return Result<EquipmentCatalogItemResponse>.Success(EquipmentMapper.ToResponse(item));
    }

    public async Task<Result<List<EquipmentCatalogItemResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _catalog.ListAsync(cancellationToken);
        _logger.LogDebug("Loaded {CatalogItemCount} cooling equipment catalog items.", items.Count);
        return Result<List<EquipmentCatalogItemResponse>>.Success(items.Select(EquipmentMapper.ToResponse).ToList());
    }
}
