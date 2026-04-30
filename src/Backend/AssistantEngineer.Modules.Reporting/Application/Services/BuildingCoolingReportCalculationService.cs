using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Reporting.Application.Models;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingCoolingReportCalculationService
{
    private readonly ILoadCalculationsFacade _loadCalculations;

    public BuildingCoolingReportCalculationService(ILoadCalculationsFacade loadCalculations)
    {
        _loadCalculations = loadCalculations;
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
                    var equipmentSizing = await _loadCalculations.CalculateRoomEquipmentSizingAsync(
                        room.Id,
                        systemType!,
                        unitType!,
                        method,
                        cancellationToken);

                    if (equipmentSizing.IsSuccess)
                    {
                        equipmentSelection = MapSelectionResult(
                            room.Id,
                            systemType!,
                            unitType!,
                            equipmentSizing.Value);
                    }
                    else if (equipmentSizing.ErrorType is
                             ResultErrorType.Validation or
                             ResultErrorType.NotFound or
                             ResultErrorType.Conflict)
                    {
                        return Result<BuildingCoolingReportData>.Failure(equipmentSizing);
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

    private static EquipmentSelectionResult MapSelectionResult(
        int roomId,
        string systemType,
        string unitType,
        EquipmentSizingResult sizing)
    {
        var best = sizing.BestMatch;
        var capacityWithReserveW = Math.Max(
            sizing.RequiredHeatingCapacityWithReserveW,
            sizing.RequiredCoolingCapacityWithReserveW);

        return new EquipmentSelectionResult
        {
            RoomId = roomId,
            TotalHeatLoadKw = RoundKw(Math.Max(sizing.RequiredHeatingCapacityW, sizing.RequiredCoolingCapacityW)),
            DesignCapacityKw = RoundKw(capacityWithReserveW),
            RequiredCoolingCapacityW = sizing.RequiredCoolingCapacityW,
            RequiredHeatingCapacityW = sizing.RequiredHeatingCapacityW,
            CapacityWithReserveW = capacityWithReserveW,
            SafetyFactor = sizing.SafetyFactor,
            HeatingSafetyFactor = sizing.HeatingSafetyFactor,
            CoolingSafetyFactor = sizing.CoolingSafetyFactor,
            RequestedSystemType = systemType,
            RequestedUnitType = unitType,
            SelectedCatalogItemId = best?.EquipmentId ?? 0,
            SelectedManufacturer = best?.Name ?? string.Empty,
            SelectedModelName = best?.Model ?? string.Empty,
            SelectedNominalCoolingCapacityKw = best?.CoolingCapacityW is null ? 0 : RoundKw(best.CoolingCapacityW.Value),
            CapacityReserveKw = best is null ? 0 : RoundKw(best.CoolingMarginW),
            AcceptedCandidates = sizing.RecommendedEquipment
                .Select(candidate => new EquipmentSelectionCandidateResult
                {
                    CatalogItemId = candidate.EquipmentId,
                    Manufacturer = candidate.Name,
                    ModelName = candidate.Model,
                    HeatingCapacityW = candidate.HeatingCapacityW,
                    CoolingCapacityW = candidate.CoolingCapacityW,
                    HeatingMarginW = candidate.HeatingMarginW,
                    CoolingMarginW = candidate.CoolingMarginW,
                    Score = candidate.Score,
                    Notes = candidate.Notes.ToList()
                })
                .ToList(),
            RejectedCandidates = sizing.RejectedEquipment
                .Select(candidate => new EquipmentSelectionRejectedCandidate
                {
                    CatalogItemId = candidate.EquipmentId,
                    Manufacturer = candidate.Name,
                    ModelName = candidate.Model,
                    Reasons = candidate.Reasons.ToList()
                })
                .ToList(),
            Diagnostics = sizing.Diagnostics
                .Select(diagnostic => new EquipmentSelectionDiagnostic
                {
                    Severity = diagnostic.Severity.ToString(),
                    Code = diagnostic.Code,
                    Message = diagnostic.Message,
                    Context = diagnostic.Context
                })
                .ToList()
        };
    }

    private static double RoundKw(double watts) =>
        Math.Round(watts / 1000.0, 2, MidpointRounding.AwayFromZero);
}
