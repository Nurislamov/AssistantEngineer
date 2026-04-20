using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Reports;

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
