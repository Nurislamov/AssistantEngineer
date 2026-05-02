namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;

public class FloorCoolingReportSummary
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
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
}
