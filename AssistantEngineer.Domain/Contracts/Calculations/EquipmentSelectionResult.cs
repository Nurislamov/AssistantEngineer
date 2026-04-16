namespace AssistantEngineer.Domain.Contracts.Calculations;

public class EquipmentSelectionResult
{
    public int RoomId { get; set; }

    public double TotalHeatLoadKw { get; set; }
    public double DesignCapacityKw { get; set; }

    public string RequestedSystemType { get; set; } = string.Empty;
    public string RequestedUnitType { get; set; } = string.Empty;

    public int SelectedCatalogItemId { get; set; }
    public string SelectedManufacturer { get; set; } = string.Empty;
    public string SelectedModelName { get; set; } = string.Empty;
    public double SelectedNominalCoolingCapacityKw { get; set; }

    public double CapacityReserveKw { get; set; }
}