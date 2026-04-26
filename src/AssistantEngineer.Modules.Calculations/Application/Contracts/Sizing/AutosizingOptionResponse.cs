namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class AutosizingOptionResponse
{
    public double RequiredCapacityKw { get; set; }

    public double UnitNominalCapacityKw { get; set; }
    public int UnitCount { get; set; }

    public double TotalNominalCapacityKw { get; set; }
    public double CoverageRatio { get; set; }
    public double OversizeRatio { get; set; }
}