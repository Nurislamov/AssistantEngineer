using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Reporting.Application.Models;

internal sealed record BuildingCoolingReportData(
    Building Building,
    BuildingCalculationResult BuildingCalculation,
    IReadOnlyList<FloorCalculationResult> FloorCalculations,
    IReadOnlyList<RoomCoolingReportCalculation> RoomCalculations,
    bool EquipmentSelectionRequested,
    string RequestedSystemType,
    string RequestedUnitType);

internal sealed record RoomCoolingReportCalculation(
    Floor Floor,
    Room Room,
    RoomCalculationResult Calculation,
    EquipmentSelectionResult? EquipmentSelection);
