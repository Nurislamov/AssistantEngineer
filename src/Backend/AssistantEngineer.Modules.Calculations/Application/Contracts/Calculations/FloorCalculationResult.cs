using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class FloorCalculationResult
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public string RequestedMethod { get; set; } = string.Empty;
    public string ActualMethod { get; set; } = string.Empty;
    public string CalculationMethodLabel { get; set; } = string.Empty;
    public int? PeakHourOfYear { get; set; }

    [Obsolete("Use PeakHourOfYear.")]
    public int? PeakHour
    {
        get => PeakHourOfYear;
        set => PeakHourOfYear = value;
    }

    public int RoomsCount { get; set; }

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

    public double CoolingLoadWPerM2 { get; set; }
    public double HeatingLoadW { get; set; }
    public double HeatingLoadWPerM2 { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<double> HourlyHeatLoadW { get; set; } = new();
    public AggregationComponentBreakdown? ComponentBreakdown { get; set; }
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new();
}
