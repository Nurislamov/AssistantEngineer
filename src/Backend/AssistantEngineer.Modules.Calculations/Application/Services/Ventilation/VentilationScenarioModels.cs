using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal sealed record VentilationAirflowResult(
    double AirflowM3PerHour,
    double AirflowM3PerSecond,
    double AirChangesPerHour);

internal sealed record NaturalVentilationAirflowResolution(
    VentilationAirflowResult Airflow,
    Iso16798NaturalVentilationResult? EnhancedResult);

internal sealed record VentilationInputNormalization(
    double AirDensityKgPerM3,
    double AirSpecificHeatJPerKgK);
