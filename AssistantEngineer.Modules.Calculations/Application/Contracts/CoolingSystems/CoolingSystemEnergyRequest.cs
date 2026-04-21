namespace AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;

public sealed class CoolingSystemEnergyRequest
{
    public double SeasonalCop { get; set; } = 3.2;
    public double DistributionEfficiency { get; set; } = 0.95;
    public double EmissionEfficiency { get; set; } = 0.98;
    public double AuxiliaryEnergyKWh { get; set; }
}