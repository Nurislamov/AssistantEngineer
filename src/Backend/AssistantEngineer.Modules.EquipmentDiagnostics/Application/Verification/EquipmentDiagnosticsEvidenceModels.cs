namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public enum EquipmentDiagnosticsEvidenceAssessmentStatus
{
    ReadyForStagingCandidate,
    NeedsTroubleshootingSection,
    ReferenceOnly,
    AlreadyRuntimeCovered,
    AlreadyStagingCovered,
    BlockedByConflict,
    BlockedByUnsafeText,
    BlockedByMissingManualEvidence,
    NotApplicable
}

public enum EquipmentDiagnosticsEvidenceConfidenceBucket
{
    StrongManualEvidence,
    PartialManualEvidence,
    ReferenceEvidenceOnly,
    InsufficientEvidence,
    ConflictedEvidence
}

public sealed record EquipmentDiagnosticsEvidenceAssessment(
    string Key,
    string Code,
    string NormalizedCode,
    string CodeKind,
    string EquipmentSide,
    string DisplayContext,
    string Series,
    string Meaning,
    string ManualId,
    string ManualTitle,
    string Page,
    string Section,
    string? ShortQuote,
    IReadOnlyList<string> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<string> Limitations,
    EquipmentDiagnosticsEvidenceAssessmentStatus Status,
    EquipmentDiagnosticsEvidenceConfidenceBucket ConfidenceBucket,
    IReadOnlyList<string> ReasonCodes,
    string RecommendedNextAction);

public sealed record EquipmentDiagnosticsEvidenceAssessmentSummary(
    int TotalAssessments,
    IReadOnlyDictionary<string, int> CountsByStatus,
    IReadOnlyDictionary<string, int> CountsByConfidenceBucket,
    int ReadyForStagingCandidateCount,
    int NeedsTroubleshootingSectionCount,
    int ReferenceOnlyCount,
    int BlockedCount);

public sealed record EquipmentDiagnosticsEvidenceAssessmentReport(
    EquipmentDiagnosticsEvidenceAssessmentSummary Summary,
    IReadOnlyList<EquipmentDiagnosticsEvidenceAssessment> Assessments);

public sealed record EquipmentDiagnosticsStagingPreviewSource(
    string ManualId,
    string ManualTitle,
    string Page,
    string Section,
    string? ShortQuote);

public sealed record EquipmentDiagnosticsStagingCandidatePreview(
    string Manufacturer,
    string Series,
    string Category,
    string Code,
    string Title,
    string Meaning,
    string ProposedConfidence,
    string ReviewStatus,
    EquipmentDiagnosticsStagingPreviewSource Source,
    IReadOnlyList<string> DiagnosticSteps,
    IReadOnlyList<string> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<string> Limitations);

public sealed record EquipmentDiagnosticsStagingPreviewReport(
    string Status,
    string ArtifactPolicy,
    int CandidateCount,
    IReadOnlyList<string> CandidateCodes,
    IReadOnlyList<EquipmentDiagnosticsStagingCandidatePreview> Candidates);
