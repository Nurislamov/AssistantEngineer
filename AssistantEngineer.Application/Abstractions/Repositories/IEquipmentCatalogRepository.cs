using AssistantEngineer.Domain.Equipment;

namespace AssistantEngineer.Application.Abstractions;

public interface IEquipmentCatalogRepository
{
    Task<CoolingEquipmentCatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
        string systemType,
        string unitType,
        CancellationToken cancellationToken = default);
    void Add(CoolingEquipmentCatalogItem item);
}
