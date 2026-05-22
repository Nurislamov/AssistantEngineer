using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationScenarioKind
{
    HeatingCoolingOnly,
    DomesticHotWaterOnly,
    SystemEnergyOnly,
    FullEngineeringCore,
    ValidationOnly,
    ReportOnly,
    TraceOnly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationExecutionMode
{
    ValidateOnly,
    PrepareOnly,
    ExecuteAvailableModules,
    ExecuteFullRequired,
    DryRun
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationExecutionStatus
{
    NotStarted,
    Prepared,
    PartiallyExecuted,
    Completed,
    CompletedWithWarnings,
    FailedValidation,
    FailedExecution,
    NotSupported
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationModuleExecutionStatus
{
    NotStarted,
    Executed,
    Skipped,
    Failed,
    NotSupported
}

public sealed record EngineeringCalculationScenarioRequestDto(
    string ScenarioId,
    int? ProjectId,
    int? BuildingId,
    EngineeringCalculationScenarioKind ScenarioKind,
    EngineeringCalculationExecutionMode ExecutionMode,
    EngineeringWorkflowStateDto State,
    IReadOnlyList<string>? RequestedModules = null,
    string? DetailLevel = null,
    bool IncludeTrace = true,
    bool IncludeReport = true,
    IReadOnlyList<string>? ReportFormats = null,
    DateTimeOffset? DeterministicTimestampUtc = null,
    string? DiagnosticsMode = null);

public sealed record EngineeringCalculationModuleValueDto(
    string Key,
    string Label,
    object? Value,
    string? Unit = null);

public sealed record EngineeringCalculationModuleExecutionResultDto(
    string ModuleKind,
    EngineeringCalculationModuleExecutionStatus Status,
    IReadOnlyList<EngineeringCalculationModuleValueDto> SummaryValues,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    double? DurationMilliseconds,
    string SourceServiceName);

public sealed record EngineeringCalculationModuleTimingDto(
    string ModuleKind,
    double DurationMilliseconds);

public sealed record EngineeringCalculationModuleSummariesDto(
    string? TopologySummary = null,
    string? VentilationSummary = null,
    string? GroundSummary = null,
    string? HeatingCoolingSummary = null,
    string? DomesticHotWaterSummary = null,
    string? SystemEnergySummary = null);

public sealed record EngineeringCalculationScenarioResultDto(
    string ScenarioId,
    EngineeringCalculationExecutionStatus Status,
    bool Executed,
    IReadOnlyList<string> ExecutedModules,
    IReadOnlyList<string> SkippedModules,
    IReadOnlyList<string> UnavailableModules,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidationDiagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    EngineeringCalculationModuleSummariesDto ModuleSummaries,
    IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> ModuleResults,
    IReadOnlyList<EngineeringCalculationModuleTimingDto> Timings,
    CalculationTraceDocument? CalculationTrace,
    EngineeringWorkflowTraceSummaryDto? CalculationTraceSummary,
    EngineeringReportDocument? EngineeringReport,
    EngineeringWorkflowReportPreviewDto? ReportPreview,
    string? ReportJson,
    string? ReportMarkdown,
    IReadOnlyDictionary<string, string> Metadata);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringProjectRecordStatus
{
    Active,
    Archived
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringCalculationArtifactKind
{
    TraceJson,
    ReportJson,
    ReportMarkdown,
    ValidationDiagnostics,
    ScenarioResultJson
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineeringScenarioHistoryEventKind
{
    Created,
    Prepared,
    Started,
    ModuleCompleted,
    Completed,
    Failed,
    ReportGenerated
}

public sealed record EngineeringProjectRecordDto(
    int ProjectId,
    string ProjectName,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    EngineeringProjectRecordStatus Status,
    IReadOnlyDictionary<string, string>? MetadataJson);

public sealed record EngineeringWorkflowStateRecordDto(
    string WorkflowStateId,
    int ProjectId,
    int? BuildingId,
    int Version,
    string CurrentStep,
    string WorkflowStateJson,
    string? ValidationDiagnosticsJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EngineeringCalculationScenarioRecordDto(
    string ScenarioId,
    int ProjectId,
    int? BuildingId,
    EngineeringCalculationScenarioKind ScenarioKind,
    EngineeringCalculationExecutionMode ExecutionMode,
    EngineeringCalculationExecutionStatus Status,
    string RequestJson,
    string? ResultSummaryJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    double? DurationMilliseconds,
    string? DiagnosticsJson);

public sealed record EngineeringCalculationArtifactRecordDto(
    string ArtifactId,
    string ScenarioId,
    EngineeringCalculationArtifactKind ArtifactKind,
    string ContentType,
    string Content,
    DateTimeOffset CreatedAtUtc,
    int? SizeBytes,
    string? ChecksumSha256);

public sealed record EngineeringScenarioHistoryEntryDto(
    string EventId,
    string ScenarioId,
    int ProjectId,
    EngineeringScenarioHistoryEventKind EventKind,
    string Message,
    string? DiagnosticsJson,
    DateTimeOffset CreatedAtUtc);
