using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Equipment;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

internal sealed class EquipmentCatalogRepository : IEquipmentCatalogRepository
{
    private readonly AppDbContext _context;

    public EquipmentCatalogRepository(AppDbContext context) => _context = context;

    public async Task<CoolingEquipmentCatalogItem?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await _context.EquipmentCatalogItems.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _context.EquipmentCatalogItems
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
        string systemType,
        string unitType,
        CancellationToken cancellationToken = default) =>
        await _context.EquipmentCatalogItems
            .Where(item => item.IsActive && item.SystemType == systemType && item.UnitType == unitType)
            .OrderBy(item => item.NominalCoolingCapacity.Watts)
            .ToListAsync(cancellationToken);

    public void Add(CoolingEquipmentCatalogItem item) => _context.EquipmentCatalogItems.Add(item);
}
