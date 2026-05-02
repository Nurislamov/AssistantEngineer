namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationLoadResult(
    double AirflowM3PerHour,
    double AirflowM3PerSecond,
    double HeatingLoadW,
    double CoolingLoadW);
