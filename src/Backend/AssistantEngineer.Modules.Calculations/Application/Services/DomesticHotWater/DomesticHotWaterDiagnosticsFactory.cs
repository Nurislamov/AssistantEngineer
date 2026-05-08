using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

internal static class DomesticHotWaterDiagnosticsFactory
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
            Family: StandardCalculationFamily.ISO12831,
            Stage: stage);
}
