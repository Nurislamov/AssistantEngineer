using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal static class ThermalZoneBoundaryDiagnosticsBuilder
{
    private const string Context = "ThermalZoneBoundaryCalculator";
    private const string Source = "ThermalZoneBoundaryCalculator";

    public static StandardCalculationDiagnostic Create(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        StandardCalculationStage stage) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: Context,
            Source: Source,
            Family: StandardCalculationFamily.InternalEngineering,
            Stage: stage);
}
