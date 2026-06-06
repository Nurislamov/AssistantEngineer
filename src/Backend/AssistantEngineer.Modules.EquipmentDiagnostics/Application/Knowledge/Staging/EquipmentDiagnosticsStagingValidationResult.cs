namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;

public sealed record EquipmentDiagnosticsStagingValidationResult(
    IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> Issues,
    EquipmentDiagnosticsStagingValidationReport? Report = null)
{
    public bool IsValid =>
        Issues.All(issue => issue.Severity != EquipmentDiagnosticsStagingValidationIssueSeverity.Error);

    public IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> Errors =>
        Issues
            .Where(issue => issue.Severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Error)
            .ToArray();
}

public sealed record EquipmentDiagnosticsStagingValidationReport(
    int TotalCandidates,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    IReadOnlyList<string> CandidateKeys,
    IReadOnlyList<EquipmentDiagnosticsStagingValidationIssueGroup> IssuesByCandidateKey,
    bool PromotionReady,
    bool HasBlockingIssues);

public sealed record EquipmentDiagnosticsStagingValidationIssueGroup(
    string CandidateKey,
    IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> Issues);
