using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

public sealed class InternalLoadStandardLookupResponse
{
    public string TableKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public RoomTypeDto RoomType { get; set; }
    public double SensibleHeatGainPerPersonW { get; set; }
    public double LatentHeatGainPerPersonW { get; set; }
    public double EquipmentGainWPerM2 { get; set; }
    public double LightingGainWPerM2 { get; set; }
    public double MinimumVentilationLitersPerSecondM2 { get; set; }
    public double OccupantDensityPeoplePer100M2 { get; set; }
    public string Notes { get; set; } = string.Empty;
}