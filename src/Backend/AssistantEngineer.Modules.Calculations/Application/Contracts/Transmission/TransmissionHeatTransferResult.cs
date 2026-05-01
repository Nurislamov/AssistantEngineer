using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionHeatTransferResult(
    double TotalHeatFlowW,
    double TotalHeatLossW,
    double TotalHeatGainW,
    double TotalHeatTransferCoefficientWPerK,
    IReadOnlyList<TransmissionElementResult> Elements,
    IReadOnlyList<CalculationDiagnostic> Diagnostics)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}