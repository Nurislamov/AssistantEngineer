namespace AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;

public sealed class HeatingSystemEnergyRequest
{
    public double GenerationEfficiency { get; set; } = 0.92;
    public double DistributionEfficiency { get; set; } = 0.95;
    public double EmissionEfficiency { get; set; } = 0.97;
}