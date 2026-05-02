namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyInput(
    double UsefulHeatingEnergyKWh = 0,
    double UsefulCoolingEnergyKWh = 0,
    double UsefulDhwEnergyKWh = 0,
    double? HeatingEfficiency = null,
    double? HeatingCop = null,
    double? CoolingCop = null,
    double? DhwEfficiency = null,
    double? DhwCop = null,
    double FanEnergyKWh = 0,
    double? PrimaryEnergyFactor = null,
    string? DiagnosticsContext = null);
