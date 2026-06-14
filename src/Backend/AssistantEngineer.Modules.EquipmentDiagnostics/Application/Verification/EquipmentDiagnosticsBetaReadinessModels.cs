namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public enum EquipmentDiagnosticsBetaReadinessStatus
{
    Pass,
    Warning,
    Blocker,
    NotApplicable
}

public sealed record EquipmentDiagnosticsBetaReadinessCheck(
    string Name,
    EquipmentDiagnosticsBetaReadinessStatus Status,
    string Summary,
    string? RelativePath = null);

public sealed record EquipmentDiagnosticsBetaReadinessSection(
    string Name,
    EquipmentDiagnosticsBetaReadinessStatus Status,
    IReadOnlyList<EquipmentDiagnosticsBetaReadinessCheck> Checks);

public sealed record EquipmentDiagnosticsBetaReadinessReport(
    DateTimeOffset GeneratedAtUtc,
    string RepositoryBaseRef,
    string? Branch,
    string? Head,
    EquipmentDiagnosticsBetaReadinessStatus OverallStatus,
    int BlockerCount,
    int WarningCount,
    IReadOnlyList<EquipmentDiagnosticsBetaReadinessSection> Sections,
    IReadOnlyList<string> KnownLimitations);

public sealed record EquipmentDiagnosticsBetaReadinessInput(
    string RepositoryRoot,
    string RepositoryBaseRef = "origin/master",
    string? Branch = null,
    string? Head = null,
    DateTimeOffset? GeneratedAtUtc = null,
    string? BranchReadinessReportPath = null,
    string? CodebookCoverageReportPath = null,
    string? StagingPreviewPath = null);
