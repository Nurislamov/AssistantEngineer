using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public enum EquipmentDiagnosticsVerificationSeverity
{
    Info,
    Warning,
    Error
}

public enum EquipmentDiagnosticsVerificationDocumentKind
{
    RuntimeCatalog,
    StagingCandidate,
    StagingTemplate,
    StagingExample,
    DocsExample
}

public enum EquipmentDiagnosticsPromotionReadiness
{
    NotReady,
    ReadyForEngineeringReview,
    ReadyForCatalogPromotion,
    Blocked
}

public sealed record EquipmentDiagnosticsVerificationDocument(
    string SourceName,
    string Json,
    EquipmentDiagnosticsVerificationDocumentKind Kind);

public sealed record EquipmentDiagnosticsVerificationInput(
    IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> RuntimeEntries,
    IReadOnlyList<EquipmentDiagnosticsVerificationDocument> RuntimeDocuments,
    IReadOnlyList<EquipmentDiagnosticsVerificationDocument> StagingDocuments,
    IReadOnlyList<EquipmentDiagnosticsVerificationDocument> DocsExampleDocuments,
    int MinimumRuntimeCatalogCount = 23);

public sealed record EquipmentDiagnosticsVerificationIssue(
    string Code,
    string Section,
    string Path,
    string Message,
    EquipmentDiagnosticsVerificationSeverity Severity);

public sealed record EquipmentDiagnosticsCandidateValidationSummary(
    string SourceName,
    IReadOnlyList<string> CandidateKeys,
    EquipmentDiagnosticsPromotionReadiness Readiness,
    IReadOnlyList<string> SuggestedNextActions,
    int ErrorCount,
    int WarningCount,
    int InfoCount);

public sealed record EquipmentDiagnosticsRuntimeCatalogSummary(
    int TotalEntries,
    int SeedEntries,
    int ManualVerifiedEntries,
    IReadOnlyList<string> DuplicateKeys);

public sealed record EquipmentDiagnosticsVerificationSection(
    string Name,
    int FileCount,
    IReadOnlyList<EquipmentDiagnosticsVerificationIssue> Issues)
{
    public bool HasBlockingIssues =>
        Issues.Any(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error);
}

public sealed record EquipmentDiagnosticsVerificationReport(
    EquipmentDiagnosticsRuntimeCatalogSummary RuntimeCatalog,
    int StagingCandidateFileCount,
    int StagingExampleFileCount,
    int DocsExampleFileCount,
    IReadOnlyList<EquipmentDiagnosticsCandidateValidationSummary> CandidateSummaries,
    IReadOnlyList<EquipmentDiagnosticsVerificationSection> Sections,
    bool IsReleaseReady,
    bool HasBlockingIssues)
{
    public int ErrorCount =>
        Sections.Sum(section => section.Issues.Count(issue =>
            issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error));

    public int WarningCount =>
        Sections.Sum(section => section.Issues.Count(issue =>
            issue.Severity == EquipmentDiagnosticsVerificationSeverity.Warning));

    public int InfoCount =>
        Sections.Sum(section => section.Issues.Count(issue =>
            issue.Severity == EquipmentDiagnosticsVerificationSeverity.Info));
}
