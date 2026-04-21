using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Reporting.Application.Models;

public sealed record BuildingCoolingReportData(
    Building Building,
    BuildingCalculationResult BuildingCalculation,
    IReadOnlyList<FloorCalculationResult> FloorCalculations,
    IReadOnlyList<RoomCoolingReportCalculation> RoomCalculations,
    bool EquipmentSelectionRequested,
    string RequestedSystemType,
    string RequestedUnitType);

public sealed record RoomCoolingReportCalculation(
    Floor Floor,
    Room Room,
    RoomCalculationResult Calculation,
    EquipmentSelectionResult? EquipmentSelection);

public sealed record BuildingHeatingReportData(
    Building Building,
    HeatingLoadCalculationMethod Method,
    IReadOnlyList<RoomHeatingLoadResult> RoomCalculations);
