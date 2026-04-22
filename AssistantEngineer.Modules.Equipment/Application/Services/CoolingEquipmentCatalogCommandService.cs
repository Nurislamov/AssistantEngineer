using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Mappers;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public class CoolingEquipmentCatalogCommandService
{
    private readonly IEquipmentCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoolingEquipmentCatalogCommandService> _logger;

    public CoolingEquipmentCatalogCommandService(
        IEquipmentCatalogRepository catalog,
        IUnitOfWork unitOfWork,
        ILogger<CoolingEquipmentCatalogCommandService>? logger = null)
    {
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<CoolingEquipmentCatalogCommandService>.Instance;
    }

    public async Task<Result<EquipmentCatalogItemResponse>> CreateAsync(
        CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating cooling equipment catalog item {Manufacturer} {ModelName}.",
            request.Manufacturer,
            request.ModelName);

        var powerResult = Power.FromWatts(request.NominalCoolingCapacityKw * 1000);
        if (powerResult.IsFailure)
        {
            _logger.LogWarning(
                "Cooling equipment catalog item creation failed for {Manufacturer} {ModelName}: {Error}.",
                request.Manufacturer,
                request.ModelName,
                powerResult.Error);
            return Result<EquipmentCatalogItemResponse>.Failure(powerResult);
        }

        var itemResult = CoolingEquipmentCatalogItem.Create(
            request.Manufacturer,
            request.SystemType,
            request.UnitType,
            request.ModelName,
            powerResult.Value,
            request.IsActive);

        if (itemResult.IsFailure)
        {
            _logger.LogWarning(
                "Cooling equipment catalog item creation failed for {Manufacturer} {ModelName}: {Error}.",
                request.Manufacturer,
                request.ModelName,
                itemResult.Error);
            return Result<EquipmentCatalogItemResponse>.Failure(itemResult);
        }

        _catalog.Add(itemResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created cooling equipment catalog item {CatalogItemId}.",
            itemResult.Value.Id);
        return Result<EquipmentCatalogItemResponse>.Success(EquipmentMapper.ToResponse(itemResult.Value));
    }
}
