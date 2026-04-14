using AssistantEngineer.Contracts.Calculations;
using AssistantEngineer.Data;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Services.Calculations;

public class EquipmentSelectionService
{
    private readonly AppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public EquipmentSelectionService(
        AppDbContext context,
        RoomCalculationService roomCalculationService)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
    }

    public async Task<EquipmentSelectionResult?> SelectForRoomAsync(
        int roomId,
        string systemType,
        string unitType)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return null;

        var windows = await _context.Windows
            .Where(w => w.RoomId == roomId)
            .ToListAsync();

        var walls = await _context.Walls
            .Where(w => w.RoomId == roomId)
            .ToListAsync();

        var calculation = _roomCalculationService.Calculate(room, windows, walls);

        var selectedItem = await _context.EquipmentCatalogItems
            .Where(x =>
                x.IsActive &&
                x.SystemType == systemType &&
                x.UnitType == unitType &&
                x.NominalCoolingCapacityKw >= calculation.DesignCapacityKw)
            .OrderBy(x => x.NominalCoolingCapacityKw)
            .FirstOrDefaultAsync();

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
                selectedItem.NominalCoolingCapacityKw - calculation.DesignCapacityKw, 2)
        };
    }
}