using AssistantEngineer.Domain.Equipment;

namespace AssistantEngineer.Domain.Services.Equipment;

public class CoolingEquipmentSelector
{
    public CoolingEquipmentCatalogItem? SelectSmallestSuitable(
        IEnumerable<CoolingEquipmentCatalogItem> items,
        double designCapacityKw)
    {
        return items
            .Where(item =>
                item.IsActive &&
                item.NominalCoolingCapacityKw >= designCapacityKw)
            .OrderBy(item => item.NominalCoolingCapacityKw)
            .FirstOrDefault();
    }
}
