using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record En16798RoomUsageProfile(
    RoomType RoomType,
    En16798ProfileCategory Category,
    double HeatingSetpointOccupiedC,
    double CoolingSetpointOccupiedC,
    double OutdoorAirLPerPerson,
    IReadOnlyList<double> OccupancyFactors,
    IReadOnlyList<double> EquipmentFactors,
    IReadOnlyList<double> LightingFactors,
    IReadOnlyList<double> VentilationFactors);