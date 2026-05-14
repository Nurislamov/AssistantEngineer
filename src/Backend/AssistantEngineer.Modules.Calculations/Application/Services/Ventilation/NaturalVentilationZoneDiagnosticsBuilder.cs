using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class NaturalVentilationZoneDiagnosticsBuilder
{
    private const string Source = "NaturalVentilationZoneLoadCalculator";

    public static StandardCalculationDiagnostic Info(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.Aggregation,
            Source);

    public static StandardCalculationDiagnostic Warning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.Aggregation,
            Source);
}
