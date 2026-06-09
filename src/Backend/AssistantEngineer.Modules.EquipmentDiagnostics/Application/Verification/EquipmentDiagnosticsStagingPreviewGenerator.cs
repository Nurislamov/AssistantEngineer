namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class EquipmentDiagnosticsStagingPreviewGenerator
{
    public EquipmentDiagnosticsStagingPreviewReport Generate(EquipmentDiagnosticsEvidenceAssessmentReport evidence)
    {
        var candidates = evidence.Assessments
            .Where(value => value.Status == EquipmentDiagnosticsEvidenceAssessmentStatus.ReadyForStagingCandidate)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .ThenBy(value => value.ManualId, StringComparer.Ordinal)
            .Select(value => new EquipmentDiagnosticsStagingCandidatePreview(
                "Gree", value.Series, Category(value.EquipmentSide), value.Code,
                $"{value.Code} manual-backed diagnostic preview", value.Meaning, "Medium", "DraftPreview",
                new EquipmentDiagnosticsStagingPreviewSource(value.ManualId, value.ManualTitle, value.Page, value.Section, value.ShortQuote),
                ["Confirm the exact installed equipment family and displayed code.",
                 "A qualified technician should follow the referenced troubleshooting section and record observations."],
                value.RequiredMeasurements,
                value.SafetyNotes.Count > 0 ? value.SafetyNotes : ["Qualified technician review is required; keep safety protections active."],
                value.Limitations.Count > 0 ? value.Limitations : ["Generated preview only; verify against the exact installed equipment manual."]))
            .ToArray();
        return new EquipmentDiagnosticsStagingPreviewReport(
            "PreviewOnly",
            "Generated artifact only; never write to runtime or production staging catalogs.",
            candidates.Length,
            candidates.Select(candidate => candidate.Code).Distinct(StringComparer.Ordinal).Order().ToArray(),
            candidates);
    }

    private static string Category(string side) => side switch
    {
        "Indoor" => "VrfIndoorUnit",
        "Outdoor" => "VrfOutdoorUnit",
        _ => "Unknown"
    };
}
