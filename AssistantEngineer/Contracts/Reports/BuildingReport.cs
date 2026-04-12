namespace AssistantEngineer.Contracts.Reports;

public class BuildingReport
{
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }

    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public List<FloorReportSummary> FloorSummaries { get; set; } = new();
    public List<RoomReportRow> Rooms { get; set; } = new();
    public List<WindowReportRow> Windows { get; set; } = new();
    public List<WallReportRow> Walls { get; set; } = new();
}
