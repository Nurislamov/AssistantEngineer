using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public class VerificationReport
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public DateTime ExecutedAtUtc { get; set; }

    public BuildingCalculationResult OurCalculation { get; set; } = null!;
    public EnergyPlusCalculationSummary EnergyPlusCalculation { get; set; } = null!;

    public VerificationMetrics CoolingMetrics { get; set; } = null!;
    public VerificationMetrics HeatingMetrics { get; set; } = null!;

    public bool Passed { get; set; }
    public string Conclusion { get; set; } = string.Empty;
}