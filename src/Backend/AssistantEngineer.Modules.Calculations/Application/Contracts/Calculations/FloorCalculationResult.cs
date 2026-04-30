using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class FloorCalculationResult
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int? PeakHour { get; set; }
    public int RoomsCount { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }
    public double CoolingLoadW { get; set; }
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
