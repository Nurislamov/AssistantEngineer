using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

public sealed class DomesticHotWaterStandardLookupResponse
{
    public string TableKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public RoomTypeDto RoomType { get; set; }
    public double LitersPerPersonDay { get; set; }
    public double ColdWaterTemperatureC { get; set; }
    public double HotWaterTemperatureC { get; set; }
    public double DistributionLossFactor { get; set; }
    public double StorageLossKWhPerDay { get; set; }
    public double CirculationLossKWhPerDay { get; set; }
    public string Notes { get; set; } = string.Empty;
}