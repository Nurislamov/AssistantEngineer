namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316SystemEnergyModuleResult(
    double DownstreamEnergyKWh,
    double UpstreamEnergyKWh,
    double LossesKWh,
    string MethodUsed);
