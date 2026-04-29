namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionHeatTransferResult(
    double TotalHeatFlowW,
    double TotalHeatLossW,
    double TotalHeatGainW,
    double TotalHeatTransferCoefficientWPerK,
    IReadOnlyList<TransmissionElementResult> Elements,
    IReadOnlyList<TransmissionDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic =>
        diagnostic.Severity == TransmissionDiagnosticSeverity.Error);
}
