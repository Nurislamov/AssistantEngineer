using System.Text.Json.Serialization;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationJobStatus
{
    Created,
    Queued,
    Running,
    Completed,
    CompletedWithWarnings,
    FailedValidation,
    FailedExecution,
    CancelRequested,
    Cancelled,
    RetryScheduled,
    NotSupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationJobExecutionMode
{
    Synchronous,
    Queued,
    DryRun,
    ValidateOnly
}

public sealed record EngineeringCalculationJobRequestDto(
    string? JobId,
    int ProjectId,
    string? ScenarioId,
    EngineeringCalculationScenarioRequestDto ScenarioRequest,
    EngineeringCalculationJobExecutionMode ExecutionMode,
    int? RequestedPriority = null,
    bool IncludeTrace = true,
    bool IncludeReport = true,
    IReadOnlyList<string>? RequestedReportFormats = null,
    DateTimeOffset? DeterministicTimestampUtc = null);

public sealed record EngineeringCalculationJobEventDto(
    string EventId,
    string JobId,
    string ScenarioId,
    EngineeringCalculationJobStatus Status,
    string Message,
    string? ModuleKind,
    int? ProgressPercent,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    DateTimeOffset CreatedAtUtc);

public sealed record EngineeringCalculationJobProgressDto(
    string JobId,
    EngineeringCalculationJobStatus Status,
    int ProgressPercent,
    string? CurrentModule,
    string CurrentStep,
    int DiagnosticsCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record EngineeringCalculationJobResultDto(
    string JobId,
    int ProjectId,
    string ScenarioId,
    EngineeringCalculationJobStatus Status,
    int ProgressPercent,
    string CurrentStep,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    double? DurationMilliseconds,
    EngineeringCalculationScenarioResultDto? ScenarioResultSummary,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EngineeringCalculationArtifactRecordDto> PersistedArtifactReferences,
    IReadOnlyList<EngineeringCalculationJobEventDto> HistoryEvents,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record EngineeringCalculationJobRecordDto(
    string JobId,
    int ProjectId,
    string ScenarioId,
    EngineeringCalculationJobStatus Status,
    EngineeringCalculationJobExecutionMode ExecutionMode,
    string RequestJson,
    string? ResultSummaryJson,
    string? DiagnosticsJson,
    int ProgressPercent,
    string CurrentStep,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? QueuedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    double? DurationMilliseconds,
    int RetryCount,
    bool CancellationRequested,
    string? ClaimedByWorkerId = null,
    DateTimeOffset? ClaimedAtUtc = null,
    DateTimeOffset? LeaseExpiresAtUtc = null);

public sealed record EngineeringCalculationJobEventRecordDto(
    string EventId,
    string JobId,
    string ScenarioId,
    int ProjectId,
    EngineeringCalculationJobStatus Status,
    string EventKind,
    string Message,
    string? DiagnosticsJson,
    int? ProgressPercent,
    DateTimeOffset CreatedAtUtc);
