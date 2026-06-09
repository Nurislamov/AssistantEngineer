namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public enum EquipmentDiagnosticsCodeCoverageStatus
{
    RuntimeCovered,
    StagingCovered,
    CodebookOnly,
    ReferenceOnly,
    StatusOnly,
    DebugOnly,
    QueryOrSettingOnly,
    NeedsManualEvidence,
    NeedsTroubleshootingSection,
    ConflictingManualMeaning,
    ConflictingCodeKind,
    ReadyForStagingCandidate,
    Blocked
}

public enum EquipmentDiagnosticsStagingReadinessRecommendation
{
    NotApplicable,
    ReferenceOnly,
    NeedsEvidence,
    NeedsEngineeringReview,
    ReadyForStagingCandidate,
    ReadyForCatalogPromotionLater,
    Blocked
}

public enum EquipmentDiagnosticsCoveragePriority
{
    CriticalOperatorDiagnostic,
    HighOperatorDiagnostic,
    NormalDiagnostic,
    Reference,
    Low
}

public sealed record EquipmentDiagnosticsManualCodeConflict(
    string Key,
    string Code,
    string Message,
    EquipmentDiagnosticsVerificationSeverity Severity);

public sealed record EquipmentDiagnosticsCodeCoverageEntry(
    string Key,
    string Manufacturer,
    string Series,
    string EquipmentSide,
    string DisplayContext,
    string Code,
    string NormalizedCode,
    string CodeKind,
    EquipmentDiagnosticsCodeCoverageStatus Status,
    EquipmentDiagnosticsStagingReadinessRecommendation Readiness,
    EquipmentDiagnosticsCoveragePriority Priority,
    IReadOnlyList<string> ManualIds,
    IReadOnlyList<string> Pages,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> Meanings,
    string RecommendedNextAction);

public sealed record EquipmentDiagnosticsCodebookCoverageSummary(
    string Status,
    int TotalRuntimeCodes,
    int TotalStagingCandidates,
    int TotalCodebookOccurrences,
    int UniqueNormalizedCodeCount,
    IReadOnlyDictionary<string, int> CoverageByStatus,
    IReadOnlyDictionary<string, int> CoverageByCodeKind,
    IReadOnlyDictionary<string, int> CoverageByEquipmentSide,
    int ReadyForStagingCandidateCount,
    int ReferenceOnlyCount,
    int ConflictCount,
    int TopRecommendationsCount);

public sealed record EquipmentDiagnosticsCodebookCoverageReport(
    EquipmentDiagnosticsCodebookCoverageSummary Summary,
    IReadOnlyList<EquipmentDiagnosticsCodeCoverageEntry> Entries,
    IReadOnlyList<EquipmentDiagnosticsManualCodeConflict> Conflicts,
    IReadOnlyList<EquipmentDiagnosticsCodeCoverageEntry> TopPriorityRecommendations,
    IReadOnlyList<string> NextActions)
{
    public int BlockerCount => Conflicts.Count(conflict => conflict.Severity == EquipmentDiagnosticsVerificationSeverity.Error);
    public int WarningCount => Conflicts.Count(conflict => conflict.Severity == EquipmentDiagnosticsVerificationSeverity.Warning);
    public bool Passed => BlockerCount == 0;
}
