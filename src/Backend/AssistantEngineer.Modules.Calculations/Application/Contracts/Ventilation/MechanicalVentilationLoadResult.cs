namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record MechanicalVentilationLoadResult(
    double AirflowM3PerHour,
    double AirflowM3PerSecond,
    double RawHeatingLoadW,
    double RawCoolingLoadW,
    double HeatRecoveryEfficiency,
    double EffectiveHeatingLoadW,
    double EffectiveCoolingLoadW);
