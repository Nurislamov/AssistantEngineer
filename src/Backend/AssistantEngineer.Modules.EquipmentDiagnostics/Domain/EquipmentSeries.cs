namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record EquipmentSeries(
    EquipmentManufacturer Manufacturer,
    string Name,
    string NormalizedName,
    EquipmentCategory Category);
