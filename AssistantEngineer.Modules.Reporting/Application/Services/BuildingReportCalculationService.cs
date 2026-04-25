using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Models;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingReportCalculationService
{
    private readonly IAggregateLoadCalculator _aggregateCalculator;
    private readonly IRoomCoolingLoadCalculator _roomCoolingLoadCalculator;
    private readonly ICoolingEquipmentSelector _equipmentSelector;

    public BuildingReportCalculationService(
        IAggregateLoadCalculator aggregateCalculator,
        IRoomCoolingLoadCalculator roomCoolingLoadCalculator,
        ICoolingEquipmentSelector equipmentSelector)
    {
        _aggregateCalculator = aggregateCalculator;
        _roomCoolingLoadCalculator = roomCoolingLoadCalculator;
        _equipmentSelector = equipmentSelector;
    }

    public async Task<BuildingCoolingReportData> BuildCoolingDataAsync(
        Building building,
        CalculationPreferences? preferences,
        IReadOnlyList<CoolingEquipmentCatalogItem> catalog,
        string? systemType,
        string? unitType,
        CoolingLoadCalculationMethod method,
        CancellationToken cancellationToken = default)
    {
        var equipmentSelectionRequested =
            !string.IsNullOrWhiteSpace(systemType) &&
            !string.IsNullOrWhiteSpace(unitType);

        var buildingCalculation = await _aggregateCalculator.CalculateBuildingAsync(
            building,
            method,
            preferences,
            cancellationToken);

        var floorCalculations = new List<FloorCalculationResult>();
        var roomCalculations = new List<RoomCoolingReportCalculation>();

        foreach (var floor in building.Floors.OrderBy(floor => floor.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            floorCalculations.Add(await _aggregateCalculator.CalculateFloorAsync(
                floor,
                method,
                preferences,
                cancellationToken));

            foreach (var room in floor.Rooms.OrderBy(room => room.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var roomCalculation = await _roomCoolingLoadCalculator.CalculateAsync(
                    room,
                    method,
                    preferences,
                    cancellationToken);
                var equipmentSelection = equipmentSelectionRequested
                    ? await _equipmentSelector.SelectForRoomAsync(
                        room,
                        systemType!,
                        unitType!,
                        catalog,
                        method,
                        preferences,
                        cancellationToken)
                    : null;

                roomCalculations.Add(new RoomCoolingReportCalculation(
                    floor,
                    room,
                    roomCalculation,
                    equipmentSelection));
            }
        }

        return new BuildingCoolingReportData(
            building,
            buildingCalculation,
            floorCalculations,
            roomCalculations,
            equipmentSelectionRequested,
            systemType ?? string.Empty,
            unitType ?? string.Empty);
    }
}
