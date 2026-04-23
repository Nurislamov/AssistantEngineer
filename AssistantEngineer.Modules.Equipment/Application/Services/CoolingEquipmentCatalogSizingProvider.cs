using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Models.Sizing;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public sealed class CoolingEquipmentCatalogSizingProvider : ICoolingEquipmentCatalogSizingProvider
{
    private readonly IEquipmentCatalogRepository _catalog;

    public CoolingEquipmentCatalogSizingProvider(IEquipmentCatalogRepository catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>> ListActiveCoolingCandidatesAsync(
        string systemType,
        string unitType,
        CancellationToken cancellationToken = default)
    {
        var items = await _catalog.ListActiveByTypeAsync(systemType, unitType, cancellationToken);

        return items
            .Select(item => new CoolingEquipmentCatalogSizingCandidate(
                CatalogItemId: item.Id,
                Manufacturer: item.Manufacturer,
                SystemType: item.SystemType,
                UnitType: item.UnitType,
                ModelName: item.ModelName,
                NominalCoolingCapacityKw: item.NominalCoolingCapacity.Kilowatts))
            .ToList();
    }
}