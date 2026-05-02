using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common;

namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;

public class BuildingCoolingReport
{
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int? PeakHourOfYear { get; set; }

    [Obsolete("Use PeakHourOfYear.")]
    public int? PeakHour
    {
        get => PeakHourOfYear;
        set => PeakHourOfYear = value;
    }

    public DateTime GeneratedAtUtc { get; set; }

    public int FloorsCount { get; set; }
    public int RoomsCount { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public double CoolingLoadW { get; set; }
    public double CoolingLoadKw { get; set; }

    [Obsolete("Use CoolingLoadW.")]
    public double TotalHeatLoadW
    {
        get => CoolingLoadW;
        set => CoolingLoadW = value;
    }

    [Obsolete("Use CoolingLoadKw.")]
    public double TotalHeatLoadKw
    {
        get => CoolingLoadKw;
        set => CoolingLoadKw = value;
    }

    public CalculationDisclosure CalculationDisclosure { get; set; } =
        EngineeringCoreReportDisclosures.CoolingDesignPoint();

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