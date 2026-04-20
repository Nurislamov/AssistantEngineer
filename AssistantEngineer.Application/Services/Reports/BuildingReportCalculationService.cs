using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Reports;

public sealed class BuildingReportCalculationService
{
    private readonly IAggregateLoadCalculator _aggregateCalculator;
    private readonly IRoomCoolingLoadCalculator _roomCoolingLoadCalculator;
    private readonly ICoolingEquipmentSelector _equipmentSelector;
    private readonly IRoomHeatingLoadCalculator _heatingCalculator;

    public BuildingReportCalculationService(
        IAggregateLoadCalculator aggregateCalculator,
        IRoomCoolingLoadCalculator roomCoolingLoadCalculator,
        ICoolingEquipmentSelector equipmentSelector,
        IRoomHeatingLoadCalculator heatingCalculator)
    {
        _aggregateCalculator = aggregateCalculator;
        _roomCoolingLoadCalculator = roomCoolingLoadCalculator;
        _equipmentSelector = equipmentSelector;
        _heatingCalculator = heatingCalculator;
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

    public async Task<BuildingHeatingReportData> BuildHeatingDataAsync(
        Building building,
        CalculationPreferences? preferences,
        HeatingLoadCalculationMethod method,
        CancellationToken cancellationToken = default)
    {
        var roomCalculations = new List<RoomHeatingLoadResult>();
        foreach (var floor in building.Floors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var room in floor.Rooms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                roomCalculations.Add(await _heatingCalculator.CalculateAsync(
                    room,
                    method,
                    preferences,
                    cancellationToken));
            }
        }

        return new BuildingHeatingReportData(building, method, roomCalculations);
    }
}
