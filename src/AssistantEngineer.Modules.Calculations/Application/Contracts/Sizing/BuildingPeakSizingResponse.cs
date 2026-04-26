namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingPeakSizingResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public bool GeneratedFromAnnualHourlyResults { get; set; } = true;

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

    public int HeatingSeasonStartMonth { get; set; }
    public int HeatingSeasonEndMonth { get; set; }

    public PeakLoadSummaryResponse? BuildingCoolingPeak { get; set; }
    public PeakLoadSummaryResponse? BuildingHeatingPeak { get; set; }

    public List<PeakLoadSummaryResponse> ZoneCoolingPeaks { get; set; } = new();
    public List<PeakLoadSummaryResponse> ZoneHeatingPeaks { get; set; } = new();

    public List<PeakLoadSummaryResponse> RoomCoolingPeaks { get; set; } = new();
    public List<PeakLoadSummaryResponse> RoomHeatingPeaks { get; set; } = new();
}