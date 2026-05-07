using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal static class ThermalTopologyDiagnosticsFactory
{
    public static StandardCalculationDiagnostic Create(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        string context,
        StandardCalculationStage stage) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: context,
            Source: "ThermalTopology",
            Family: StandardCalculationFamily.InternalEngineering,
            Stage: stage);
}
