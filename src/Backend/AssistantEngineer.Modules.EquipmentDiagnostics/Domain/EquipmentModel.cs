namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record EquipmentModel(
    EquipmentManufacturer Manufacturer,
    EquipmentSeries Series,
    string ModelCode,
    string NormalizedModelCode,
    EquipmentCategory Category);
