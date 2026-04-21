using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;

namespace AssistantEngineer.Modules.Equipment.Application.Mappers;

internal static class EquipmentMapper
{
    public static EquipmentCatalogItemResponse ToResponse(CoolingEquipmentCatalogItem item) =>
        new()
        {
            Id = item.Id,
            Manufacturer = item.Manufacturer,
            SystemType = item.SystemType,
            UnitType = item.UnitType,
            ModelName = item.ModelName,
            NominalCoolingCapacityKw = item.NominalCoolingCapacity.Kilowatts,
            IsActive = item.IsActive
        };
}
