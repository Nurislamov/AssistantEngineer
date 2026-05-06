namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record Iso16798NaturalVentilationResult(
    Iso16798NaturalVentilationCalculationMode CalculationMode,
    double EffectiveOpeningAreaM2,
    double StackAirflowM3PerS,
    double WindAirflowM3PerS,
    double TotalAirflowM3PerS,
    double TotalAirflowM3PerH,
    double AirChangesPerHour,
    double ClampedAirChangesPerHour,
    double HeatTransferCoefficientWPerK,
    IReadOnlyList<Iso16798NaturalVentilationDiagnostics> Diagnostics);
