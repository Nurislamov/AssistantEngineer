namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record InfiltrationLoadResult(
    double InfiltrationAirChangesPerHour,
    double InfiltrationAirflowM3PerHour,
    double InfiltrationAirflowM3PerSecond,
    double HeatingLoadW,
    double CoolingLoadW);
