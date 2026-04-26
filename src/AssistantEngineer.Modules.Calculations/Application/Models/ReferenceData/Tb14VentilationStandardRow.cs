using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record Tb14VentilationStandardRow(
    string TableKey,
    string Version,
    RoomType RoomType,
    double OutdoorAirLitersPerSecondPerPerson,
    double OutdoorAirLitersPerSecondPerM2,
    double ExhaustAirChangesPerHour,
    bool RecirculationAllowed,
    string Notes);