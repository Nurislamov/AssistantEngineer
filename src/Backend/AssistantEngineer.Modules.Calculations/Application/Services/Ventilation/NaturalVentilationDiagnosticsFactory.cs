using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class NaturalVentilationDiagnosticsFactory
{
    public static StandardCalculationDiagnostic Create(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        StandardCalculationStage stage,
        string source) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: source,
            Source: source,
            Family: StandardCalculationFamily.EN16798,
            Stage: stage);
}
