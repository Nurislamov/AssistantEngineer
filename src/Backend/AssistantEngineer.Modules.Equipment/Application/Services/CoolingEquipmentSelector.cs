using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public sealed class CoolingEquipmentSelector : ICoolingEquipmentSelector
{
    public Result<EquipmentSelectionResult> SelectForRoom(
        int roomId,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        double totalHeatLoadKw,
        double designCapacityKw)
    {
        var suitable = catalog
            .Where(item =>
                item.IsActive &&
                item.SystemType == systemType &&
                item.UnitType == unitType &&
                item.NominalCoolingCapacity.Kilowatts >= designCapacityKw)
            .OrderBy(item => item.NominalCoolingCapacity.Kilowatts)
            .FirstOrDefault();

        if (suitable is null)
            return Result<EquipmentSelectionResult>.NotFound("No suitable equipment found.");

        return Result<EquipmentSelectionResult>.Success(new EquipmentSelectionResult
        {
            RoomId = roomId,
            TotalHeatLoadKw = totalHeatLoadKw,
            DesignCapacityKw = designCapacityKw,
            RequestedSystemType = systemType,
            RequestedUnitType = unitType,
            SelectedCatalogItemId = suitable.Id,
            SelectedManufacturer = suitable.Manufacturer,
            SelectedModelName = suitable.ModelName,
            SelectedNominalCoolingCapacityKw = suitable.NominalCoolingCapacity.Kilowatts,
            CapacityReserveKw = Math.Round(
                suitable.NominalCoolingCapacity.Kilowatts - designCapacityKw,
                2,
                MidpointRounding.AwayFromZero)
        });
    }
}
