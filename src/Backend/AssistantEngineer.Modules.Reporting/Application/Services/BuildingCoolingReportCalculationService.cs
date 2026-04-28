using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Models;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingCoolingReportCalculationService
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IEquipmentFacade _equipment;

    public BuildingCoolingReportCalculationService(
        ILoadCalculationsFacade loadCalculations,
        IEquipmentFacade equipment)
    {
        _loadCalculations = loadCalculations;
        _equipment = equipment;
    }

    public async Task<Result<BuildingCoolingReportData>> BuildCoolingDataAsync(
        Building building,
        string? systemType,
        string? unitType,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken = default)
    {
        var equipmentSelectionRequested =
            !string.IsNullOrWhiteSpace(systemType) &&
            !string.IsNullOrWhiteSpace(unitType);

        var buildingCalculation = await _loadCalculations.CalculateBuildingCoolingLoadAsync(
            building.Id,
            method,
            cancellationToken);

        if (buildingCalculation.IsFailure)
            return Result<BuildingCoolingReportData>.Failure(buildingCalculation);

        var floorCalculations = new List<FloorCalculationResult>();
        var roomCalculations = new List<RoomCoolingReportCalculation>();

        foreach (var floor in building.Floors.OrderBy(floor => floor.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var floorCalculation = await _loadCalculations.CalculateFloorCoolingLoadAsync(
                floor.Id,
                method,
                cancellationToken);

            if (floorCalculation.IsFailure)
                return Result<BuildingCoolingReportData>.Failure(floorCalculation);

            floorCalculations.Add(floorCalculation.Value);

            foreach (var room in floor.Rooms.OrderBy(room => room.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var roomCalculation = await _loadCalculations.CalculateRoomCoolingLoadAsync(
                    room.Id,
                    method,
                    cancellationToken);

                if (roomCalculation.IsFailure)
                    return Result<BuildingCoolingReportData>.Failure(roomCalculation);

                EquipmentSelectionResult? equipmentSelection = null;

                if (equipmentSelectionRequested)
                {
                    var equipmentSelectionResult = await _equipment.SelectRoomEquipmentAsync(
                        room.Id,
                        new EquipmentSelectionRequest
                        {
                            SystemType = systemType!,
                            UnitType = unitType!
                        },
                        roomCalculation.Value.TotalHeatLoadKw,
                        roomCalculation.Value.DesignCapacityKw,
                        cancellationToken);

                    if (equipmentSelectionResult.IsSuccess)
                    {
                        equipmentSelection = equipmentSelectionResult.Value;
                    }
                    else if (equipmentSelectionResult.ErrorType is
                             ResultErrorType.Validation or
                             ResultErrorType.NotFound or
                             ResultErrorType.Conflict)
                    {
                        return Result<BuildingCoolingReportData>.Failure(equipmentSelectionResult);
                    }
                }

                roomCalculations.Add(new RoomCoolingReportCalculation(
                    floor,
                    room,
                    roomCalculation.Value,
                    equipmentSelection));
            }
        }

        return Result<BuildingCoolingReportData>.Success(new BuildingCoolingReportData(
            building,
            buildingCalculation.Value,
            floorCalculations,
            roomCalculations,
            equipmentSelectionRequested,
            systemType ?? string.Empty,
            unitType ?? string.Empty));
    }
}