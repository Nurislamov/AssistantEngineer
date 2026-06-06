using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;

public sealed record EquipmentDiagnosticOperatorGuidanceMessage(
    string Title,
    string Summary,
    string VerificationBanner,
    string SourceLine,
    IReadOnlyList<string> RecommendedChecks,
    string SafetyLine,
    IReadOnlyList<string> OperatorNotes,
    string Footer)
{
    public static EquipmentDiagnosticOperatorGuidanceMessage FromDiagnosticCase(
        EquipmentDiagnosticCaseDto diagnosticCase) =>
        EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);
}
