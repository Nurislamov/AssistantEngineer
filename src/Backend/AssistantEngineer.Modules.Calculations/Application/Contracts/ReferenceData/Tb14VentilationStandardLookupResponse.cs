using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

public sealed class Tb14VentilationStandardLookupResponse
{
    public string TableKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public RoomTypeDto RoomType { get; set; }
    public double OutdoorAirLitersPerSecondPerPerson { get; set; }
    public double OutdoorAirLitersPerSecondPerM2 { get; set; }
    public double ExhaustAirChangesPerHour { get; set; }
    public bool RecirculationAllowed { get; set; }
    public string Notes { get; set; } = string.Empty;
}