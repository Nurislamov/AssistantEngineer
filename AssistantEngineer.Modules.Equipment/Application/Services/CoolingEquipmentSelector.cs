using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Equipment.Application.Abstractions;

namespace AssistantEngineer.Modules.Equipment.Application.Services;

public sealed class CoolingEquipmentSelector : ICoolingEquipmentSelector
{
    private readonly IRoomCoolingLoadCalculator _roomCoolingLoadCalculator;

    public CoolingEquipmentSelector(IRoomCoolingLoadCalculator roomCoolingLoadCalculator) =>
        _roomCoolingLoadCalculator = roomCoolingLoadCalculator;

    public async Task<EquipmentSelectionResult?> SelectForRoomAsync(
        Room room,
        string systemType,
        string unitType,
        IEnumerable<CoolingEquipmentCatalogItem> catalog,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        var calcResult = await _roomCoolingLoadCalculator.CalculateAsync(room, method, preferences, cancellationToken);
        var requiredCapacity = calcResult.DesignCapacityKw;

        var suitable = catalog
            .Where(item => item.IsActive &&
                           item.SystemType == systemType &&
                           item.UnitType == unitType &&
                           item.NominalCoolingCapacity.Kilowatts >= requiredCapacity)
            .OrderBy(item => item.NominalCoolingCapacity.Kilowatts)
            .FirstOrDefault();

        if (suitable == null)
            return null;

        return new EquipmentSelectionResult
        {
            RoomId = room.Id,
            TotalHeatLoadKw = calcResult.TotalHeatLoadKw,
            DesignCapacityKw = calcResult.DesignCapacityKw,
            RequestedSystemType = systemType,
            RequestedUnitType = unitType,
            SelectedCatalogItemId = suitable.Id,
            SelectedManufacturer = suitable.Manufacturer,
            SelectedModelName = suitable.ModelName,
            SelectedNominalCoolingCapacityKw = suitable.NominalCoolingCapacity.Kilowatts,
            CapacityReserveKw = Math.Round(
                suitable.NominalCoolingCapacity.Kilowatts - requiredCapacity,
                2,
                MidpointRounding.AwayFromZero)
        };
    }
}
