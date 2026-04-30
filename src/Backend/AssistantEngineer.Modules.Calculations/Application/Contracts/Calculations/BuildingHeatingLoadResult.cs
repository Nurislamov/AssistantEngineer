using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class BuildingHeatingLoadResult
{
    public int BuildingId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public int RoomsCount { get; set; }

    public double TransmissionHeatLossW { get; set; }
    public double VentilationHeatLossW { get; set; }
    public double TotalDesignHeatingLoadW { get; set; }
    public double TotalDesignHeatingLoadKw { get; set; }
    public double HeatingLoadW { get; set; }
    public double HeatingLoadWPerM2 { get; set; }

    public List<RoomHeatingLoadResult> Rooms { get; set; } = new();
    public AggregationComponentBreakdown? ComponentBreakdown { get; set; }
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new();
}
