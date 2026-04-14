namespace AssistantEngineer.Models;

public class EquipmentCatalogItem
{
    public int Id { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;   // VRF / Split / FCU
    public string UnitType { get; set; } = string.Empty;     // Cassette / WallMounted / Duct

    public string ModelName { get; set; } = string.Empty;

    public double NominalCoolingCapacityKw { get; set; }

    public bool IsActive { get; set; } = true;
}