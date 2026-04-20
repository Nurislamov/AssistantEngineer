using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;

namespace AssistantEngineer.Domain.Equipment;

public class CoolingEquipmentCatalogItem
{
    public int Id { get; private set; }
    public string Manufacturer { get; private set; } = string.Empty;
    public string SystemType { get; private set; } = string.Empty;
    public string UnitType { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public Power NominalCoolingCapacity { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private CoolingEquipmentCatalogItem() { }

    private CoolingEquipmentCatalogItem(
        string manufacturer,
        string systemType,
        string unitType,
        string modelName,
        Power nominalCoolingCapacity,
        bool isActive)
    {
        Manufacturer = manufacturer;
        SystemType = systemType;
        UnitType = unitType;
        ModelName = modelName;
        NominalCoolingCapacity = nominalCoolingCapacity;
        IsActive = isActive;
    }

    public static Result<CoolingEquipmentCatalogItem> Create(
        string manufacturer,
        string systemType,
        string unitType,
        string modelName,
        Power nominalCoolingCapacity,
        bool isActive = true)
    {
        var manufacturerResult = manufacturer.ToRequiredTrimmed("Manufacturer");
        if (manufacturerResult.IsFailure) return Result<CoolingEquipmentCatalogItem>.Failure(manufacturerResult);

        var systemResult = systemType.ToRequiredTrimmed("System type");
        if (systemResult.IsFailure) return Result<CoolingEquipmentCatalogItem>.Failure(systemResult);

        var unitResult = unitType.ToRequiredTrimmed("Unit type");
        if (unitResult.IsFailure) return Result<CoolingEquipmentCatalogItem>.Failure(unitResult);

        var modelResult = modelName.ToRequiredTrimmed("Model name");
        if (modelResult.IsFailure) return Result<CoolingEquipmentCatalogItem>.Failure(modelResult);

        return Result<CoolingEquipmentCatalogItem>.Success(new CoolingEquipmentCatalogItem(
            manufacturerResult.Value,
            systemResult.Value,
            unitResult.Value,
            modelResult.Value,
            nominalCoolingCapacity,
            isActive));
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
