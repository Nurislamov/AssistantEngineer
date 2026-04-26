namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;

public class BuildingCoolingReport
{
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int? PeakHour { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public List<FloorCoolingReportSummary> FloorSummaries { get; set; } = [];
    public List<RoomCoolingReportRow> Rooms { get; set; } = [];
    public List<WindowCoolingReportRow> Windows { get; set; } = [];
    public List<WallCoolingReportRow> Walls { get; set; } = [];

    public bool EquipmentSelectionRequested { get; set; }
    public string RequestedSystemType { get; set; } = string.Empty;
    public string RequestedUnitType { get; set; } = string.Empty;

    public int RoomsWithSelectionCount { get; set; }
    public int RoomsWithoutSelectionCount { get; set; }

    public double? TotalSelectedCapacityKw { get; set; }
}