using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationPressureResult(
    double? PressureDifferencePa,
    double AirDensityKgPerCubicMeter,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
