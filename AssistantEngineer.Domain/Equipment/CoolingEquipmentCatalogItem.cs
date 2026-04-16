namespace AssistantEngineer.Domain.Equipment;

public class CoolingEquipmentCatalogItem
{
    public int Id { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public double NominalCoolingCapacityKw { get; set; }

    public bool IsActive { get; set; } = true;
}
