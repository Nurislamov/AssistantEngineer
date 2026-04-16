using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Domain.Services.Equipment;

namespace AssistantEngineer.Services.Calculations;

public class EquipmentSelectionService
{
    private readonly IAppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;
    private readonly CoolingEquipmentSelector _coolingEquipmentSelector;

    public EquipmentSelectionService(
        IAppDbContext context,
        RoomCalculationService roomCalculationService,
        CoolingEquipmentSelector coolingEquipmentSelector)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
        _coolingEquipmentSelector = coolingEquipmentSelector;
    }

    public async Task<EquipmentSelectionResult?> SelectForRoomAsync(
        int roomId,
        string systemType,
        string unitType)
    {
        var room = _context.Rooms.FirstOrDefault(r => r.Id == roomId);

        if (room == null)
            return null;

        var windows = _context.Windows
            .Where(w => w.RoomId == roomId)
            .ToList();

        var walls = _context.Walls
            .Where(w => w.RoomId == roomId)
            .ToList();

        var calculation = _roomCalculationService.Calculate(room, windows, walls);

        var candidateItems = _context.EquipmentCatalogItems
            .Where(x =>
                x.IsActive &&
                x.SystemType == systemType &&
                x.UnitType == unitType)
            .ToList();

        var selectedItem = _coolingEquipmentSelector.SelectSmallestSuitable(
            candidateItems,
            calculation.DesignCapacityKw);

        if (selectedItem == null)
            return null;

        return new EquipmentSelectionResult
        {
            RoomId = room.Id,
            TotalHeatLoadKw = calculation.TotalHeatLoadKw,
            DesignCapacityKw = calculation.DesignCapacityKw,
            RequestedSystemType = systemType,
            RequestedUnitType = unitType,
            SelectedCatalogItemId = selectedItem.Id,
            SelectedManufacturer = selectedItem.Manufacturer,
            SelectedModelName = selectedItem.ModelName,
            SelectedNominalCoolingCapacityKw = selectedItem.NominalCoolingCapacityKw,
            CapacityReserveKw = Math.Round(
                selectedItem.NominalCoolingCapacityKw - calculation.DesignCapacityKw,
                2)
        };
    }
}
