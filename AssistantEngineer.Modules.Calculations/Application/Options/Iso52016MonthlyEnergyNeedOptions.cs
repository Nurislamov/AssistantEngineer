namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso52016MonthlyEnergyNeedOptions
{
    public double HeatingGainUtilizationFactor { get; init; } = 0.90;
    public double CoolingGainUtilizationFactor { get; init; } = 1.00;
    public double MinimumMonthlyDemandKWh { get; init; } = 0.05;
}