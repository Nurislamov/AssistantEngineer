namespace AssistantEngineer.Contracts;

public class BuildingReportDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }

    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }

    public double ReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public List<BuildingFloorSummaryDto> FloorSummaries { get; set; } = new();
    public List<BuildingRoomReportRowDto> Rooms { get; set; } = new();
    public List<WindowReportRowDto> Windows { get; set; } = new();
    public List<WallReportRowDto> Walls { get; set; } = new();
}
