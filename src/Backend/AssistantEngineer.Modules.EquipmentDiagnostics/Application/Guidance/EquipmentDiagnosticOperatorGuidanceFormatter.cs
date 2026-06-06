using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;

public static class EquipmentDiagnosticOperatorGuidanceFormatter
{
    public static EquipmentDiagnosticOperatorGuidanceMessage Format(
        EquipmentDiagnosticCaseDto diagnosticCase)
    {
        ArgumentNullException.ThrowIfNull(diagnosticCase);

        return new EquipmentDiagnosticOperatorGuidanceMessage(
            Title: BuildTitle(diagnosticCase),
            Summary: diagnosticCase.ShortSummary,
            VerificationBanner: BuildVerificationBanner(diagnosticCase),
            SourceLine: diagnosticCase.SourceSummary,
            RecommendedChecks: diagnosticCase.RecommendedNextChecks.ToArray(),
            SafetyLine: diagnosticCase.SafetyBoundary,
            OperatorNotes: diagnosticCase.OperatorNotes.ToArray(),
            Footer: BuildFooter(diagnosticCase));
    }

    private static string BuildTitle(EquipmentDiagnosticCaseDto diagnosticCase)
    {
        var family = diagnosticCase.ErrorCode.SeriesName ?? diagnosticCase.ErrorCode.Category.ToString();

        return $"{diagnosticCase.ErrorCode.Manufacturer} {family} {diagnosticCase.ErrorCode.Code} diagnostic guidance";
    }

    private static string BuildVerificationBanner(EquipmentDiagnosticCaseDto diagnosticCase)
    {
        if (diagnosticCase.IsManualVerified && !diagnosticCase.VerificationRequired)
        {
            return $"Manual verified: {diagnosticCase.ConfidenceExplanation}";
        }

        if (diagnosticCase.IsSeedKnowledge)
        {
            return $"Verification required: seed knowledge. {diagnosticCase.ConfidenceExplanation}";
        }

        if (diagnosticCase.VerificationRequired)
        {
            return $"Verification required. {diagnosticCase.ConfidenceExplanation}";
        }

        return diagnosticCase.ConfidenceExplanation;
    }

    private static string BuildFooter(EquipmentDiagnosticCaseDto diagnosticCase) =>
        diagnosticCase.OperatorNotes.LastOrDefault()
        ?? diagnosticCase.ApplicabilitySummary
        ?? diagnosticCase.SourceSummary;
}
