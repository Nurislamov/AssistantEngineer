namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationLoadResult(
    double AirflowM3PerHour,
    double AirflowM3PerSecond,
    double HeatingLoadW,
    double CoolingLoadW,
    double AirChangeRatePerHour = 0.0,
    double HeatTransferCoefficientWPerK = 0.0,
    double WindComponentM3PerHour = 0.0,
    double StackComponentM3PerHour = 0.0,
    string SelectedBranch = "Closed",
    string? ClampReason = null,
    string? ControlReason = null);
