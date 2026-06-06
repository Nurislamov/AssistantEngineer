namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public enum BranchReadinessScopeClassification
{
    Allowed,
    Suspicious,
    Forbidden,
    GeneratedIgnoredCandidate
}

public sealed record BranchReadinessChangedFile(
    string Path,
    string ChangeType,
    bool IsBranchChange,
    bool IsStaged,
    bool IsUnstaged,
    bool IsUntracked,
    BranchReadinessScopeClassification ScopeClassification,
    string ScopeReason);

public sealed record BranchReadinessFileInput(
    string Path,
    string ChangeType,
    bool IsBranchChange,
    bool IsStaged,
    bool IsUnstaged,
    bool IsUntracked,
    string? Content);

public sealed record BranchReadinessCommandResult(
    string Name,
    string Command,
    bool Passed,
    int ExitCode,
    string Summary);

public sealed record BranchReadinessIssue(
    string Code,
    string Path,
    string Message,
    EquipmentDiagnosticsVerificationSeverity Severity);

public sealed record BranchReadinessInput(
    string CurrentBranch,
    string BaseRef,
    string Scope,
    IReadOnlyList<BranchReadinessFileInput> Files,
    EquipmentDiagnosticsVerificationReport EquipmentDiagnosticsReport,
    IReadOnlyList<BranchReadinessCommandResult> Commands);

public sealed record BranchReadinessChangedFileSummary(
    int Total,
    int BranchChanges,
    int Staged,
    int Unstaged,
    int Untracked,
    int Added,
    int Modified,
    int Deleted,
    int Allowed,
    int Suspicious,
    int Forbidden,
    int GeneratedIgnoredCandidates);

public sealed record BranchReadinessReport(
    string Status,
    string CurrentBranch,
    string BaseRef,
    string Scope,
    BranchReadinessChangedFileSummary ChangedFilesSummary,
    IReadOnlyList<BranchReadinessChangedFile> ChangedFiles,
    IReadOnlyList<BranchReadinessIssue> Issues,
    EquipmentDiagnosticsVerificationReport EquipmentDiagnostics,
    IReadOnlyList<BranchReadinessCommandResult> Commands,
    IReadOnlyList<string> NextActions)
{
    public int BlockersCount =>
        Issues.Count(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error) +
        Commands.Count(command => !command.Passed) +
        (EquipmentDiagnostics.HasBlockingIssues ? 1 : 0);

    public int WarningsCount =>
        Issues.Count(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Warning) +
        EquipmentDiagnostics.WarningCount;

    public int InfoCount =>
        Issues.Count(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Info) +
        EquipmentDiagnostics.InfoCount;

    public bool Passed => BlockersCount == 0;
}
