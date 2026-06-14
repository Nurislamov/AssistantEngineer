namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed record AssistantEngineerGoalRunReport(
    string? GoalId,
    string? Title,
    string? SourceBranch,
    string? TargetBranch,
    IReadOnlyList<string>? Scope,
    IReadOnlyList<string>? OutOfScope,
    IReadOnlyList<string>? Constraints,
    AssistantEngineerGoalRunPreflight? Preflight,
    IReadOnlyList<AssistantEngineerGoalRunPhase>? Phases,
    AssistantEngineerGoalRunFinalAudit? FinalAudit,
    IReadOnlyList<string>? Warnings,
    IReadOnlyList<string>? Blockers,
    IReadOnlyList<string>? GeneratedArtifacts);

public sealed record AssistantEngineerGoalRunPreflight(
    IReadOnlyList<AssistantEngineerGoalRunCommand>? Commands);

public sealed record AssistantEngineerGoalRunCommand(
    string? Name,
    string? Command,
    string? Status,
    string? Evidence);

public sealed record AssistantEngineerGoalRunPhase(
    int Number,
    string? Title,
    string? Status,
    IReadOnlyList<string>? Deliverables,
    IReadOnlyList<string>? AcceptanceCriteria,
    IReadOnlyList<string>? MandatoryCommands,
    IReadOnlyList<string>? Evidence);

public sealed record AssistantEngineerGoalRunFinalAudit(
    string? Status,
    string? RoadmapCoverage,
    string? PhaseCompletion,
    string? ChangedFilesReview,
    string? ForbiddenFilesReview,
    string? ForbiddenClaimsReview,
    string? SecretsScan,
    string? GeneratedArtifacts,
    string? MergeReadiness);

public sealed record AssistantEngineerGoalRunValidationResult(
    bool IsReady,
    string Status,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers);
