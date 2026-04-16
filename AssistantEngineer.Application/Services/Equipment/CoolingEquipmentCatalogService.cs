using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Equipment;

namespace AssistantEngineer.Application.Services.Equipment;

public class CoolingEquipmentCatalogService
{
    private readonly IAppDbContext _context;

    public CoolingEquipmentCatalogService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<EquipmentCatalogItemResponse?> GetByIdAsync(int id)
    {
        return _context.EquipmentCatalogItems
            .Where(x => x.Id == id)
            .Select(x => ToResponse(x))
            .FirstOrDefault();
    }

    public async Task<List<EquipmentCatalogItemResponse>> GetAllAsync()
    {
        return _context.EquipmentCatalogItems
            .OrderBy(x => x.SystemType)
            .ThenBy(x => x.UnitType)
            .ThenBy(x => x.NominalCoolingCapacityKw)
            .Select(x => ToResponse(x))
            .ToList();
    }

    public async Task<EquipmentCatalogItemResponse> CreateAsync(
        CreateEquipmentCatalogItemRequest request)
    {
        var item = new CoolingEquipmentCatalogItem
        {
            Manufacturer = request.Manufacturer,
            SystemType = request.SystemType,
            UnitType = request.UnitType,
            ModelName = request.ModelName,
            NominalCoolingCapacityKw = request.NominalCoolingCapacityKw,
            IsActive = request.IsActive
        };

        _context.AddCoolingEquipmentCatalogItem(item);
        await _context.SaveChangesAsync();

        return ToResponse(item);
    }

    private static EquipmentCatalogItemResponse ToResponse(CoolingEquipmentCatalogItem item)
    {
        return new EquipmentCatalogItemResponse
        {
            Id = item.Id,
            Manufacturer = item.Manufacturer,
            SystemType = item.SystemType,
            UnitType = item.UnitType,
            ModelName = item.ModelName,
            NominalCoolingCapacityKw = item.NominalCoolingCapacityKw,
            IsActive = item.IsActive
        };
    }
}
