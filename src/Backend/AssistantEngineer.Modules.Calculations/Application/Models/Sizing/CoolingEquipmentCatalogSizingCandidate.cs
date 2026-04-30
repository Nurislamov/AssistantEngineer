namespace AssistantEngineer.Modules.Calculations.Application.Models.Sizing;

public sealed record CoolingEquipmentCatalogSizingCandidate(
    int CatalogItemId,
    string Manufacturer,
    string SystemType,
    string UnitType,
    string ModelName,
    double NominalCoolingCapacityKw,
    double? NominalHeatingCapacityKw = null);
