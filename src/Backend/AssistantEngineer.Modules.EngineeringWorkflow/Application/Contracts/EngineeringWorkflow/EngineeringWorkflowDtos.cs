using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

public sealed record EngineeringWorkflowStateDto(
    int ProjectId,
    string ProjectName,
    int? BuildingId,
    string CurrentStep,
    IReadOnlyList<EngineeringWorkflowStepDto> Steps,
    IReadOnlyList<string> AvailableModules,
    EngineeringWorkflowBuildingMetadataDto BuildingMetadata,
    IReadOnlyList<EngineeringWorkflowZoneDto> Zones,
    IReadOnlyList<EngineeringWorkflowBoundaryDto> Boundaries,
    EngineeringWorkflowWeatherSolarSettingsDto WeatherSolarSettings,
    EngineeringWorkflowVentilationSettingsDto VentilationSettings,
    EngineeringWorkflowGroundSettingsDto GroundSettings,
    EngineeringWorkflowDomesticHotWaterSettingsDto DomesticHotWaterSettings,
    EngineeringWorkflowSystemEnergySettingsDto SystemEnergySettings,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Links,
    EngineeringWorkflowTraceSummaryDto? CalculationTraceSummary,
    EngineeringWorkflowReportPreviewDto? ReportSummary,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record EngineeringWorkflowStepDto(
    string Kind,
    string Status,
    bool IsComplete);

public sealed record EngineeringWorkflowBuildingMetadataDto(
    string? BuildingName,
    string? LocationText,
    double? FloorAreaM2,
    double? VolumeM3,
    int? NumberOfZones,
    string? Notes);

public sealed record EngineeringWorkflowZoneDto(
    string ZoneId,
    string Name,
    string ZoneKind,
    double? FloorAreaM2,
    double? AirVolumeM3,
    string Status);

public sealed record EngineeringWorkflowBoundaryDto(
    string BoundaryId,
    string ZoneOrRoomName,
    string ExposureKind,
    double? AreaM2,
    double? UValue,
    string? AdjacentZoneReference,
    string Indicator,
    string ValidationStatus);

public sealed record EngineeringWorkflowWeatherSolarSettingsDto(
    string WeatherSourceStatus,
    string LocationTimezoneSummary,
    string SolarChainReadinessSummary);

public sealed record EngineeringWorkflowVentilationSettingsDto(
    int OpeningCount,
    string ControlModeSummary,
    string AirflowSummary,
    IReadOnlyList<string> Warnings);

public sealed record EngineeringWorkflowGroundSettingsDto(
    int GroundBoundaryCount,
    string GroundProfileMode,
    string SummaryStatus);

public sealed record EngineeringWorkflowDomesticHotWaterSettingsDto(
    string DemandBasis,
    string UsefulDemandSummary,
    string LossesSummary,
    string OwnershipPolicy);

public sealed record EngineeringWorkflowSystemEnergySettingsDto(
    string UsesSummary,
    string CarriersSummary,
    string FinalPrimaryCarbonSummary);

public sealed record EngineeringWorkflowDiagnosticDto(
    string Severity,
    string Code,
    string Message,
    string SourceStep,
    string? SourceModule = null,
    string? SuggestedCorrection = null,
    string? TargetField = null);

public sealed record EngineeringWorkflowValidationRequestDto(
    EngineeringWorkflowStateDto State);

public sealed record EngineeringWorkflowValidationResponseDto(
    bool IsValid,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyList<EngineeringWorkflowStepDto> Steps);

public sealed record EngineeringWorkflowCalculationPreparationRequestDto(
    EngineeringWorkflowStateDto State,
    bool ExecuteCalculation = false);

public sealed record EngineeringWorkflowCalculationPreparationResponseDto(
    string RequestId,
    string Status,
    bool Executed,
    IReadOnlyDictionary<string, string> RequestPreview,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record EngineeringWorkflowTracePreviewRequestDto(
    EngineeringWorkflowStateDto State,
    string DetailLevel = "Standard");

public sealed record EngineeringWorkflowTracePreviewResponseDto(
    CalculationTraceDocument TraceDocument,
    EngineeringWorkflowTraceSummaryDto TraceSummary,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);

public sealed record EngineeringWorkflowTraceSummaryDto(
    string TraceId,
    string? CalculationId,
    string DetailLevel,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EngineeringWorkflowTraceStepSummaryDto> Steps);

public sealed record EngineeringWorkflowTraceStepSummaryDto(
    string StepId,
    string ModuleKind,
    string StepName,
    int Sequence,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    int DiagnosticsCount);

public sealed record EngineeringWorkflowReportRequestDto(
    EngineeringWorkflowStateDto State,
    string ReportKind = "FullEngineeringCore",
    string RequestedFormat = "Json",
    string DetailLevel = "Standard",
    bool IncludeTraceAppendix = true,
    bool IncludeLimitations = true);

public sealed record EngineeringWorkflowReportResponseDto(
    EngineeringReportDocument ReportDocument,
    EngineeringWorkflowReportPreviewDto Preview,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);

public sealed record EngineeringWorkflowReportPreviewDto(
    string ReportKind,
    string Title,
    IReadOnlyList<string> Sections,
    int WarningsCount,
    int DiagnosticsCount,
    IReadOnlyList<string> ExportFormatsAvailable,
    DateTimeOffset GeneratedTimestampUtc,
    IReadOnlyList<string> Limitations);

public sealed record EngineeringWorkflowReportExportRequestDto(
    EngineeringWorkflowReportRequestDto Request);

public sealed record EngineeringWorkflowReportExportResponseDto(
    string Format,
    string Content,
    string SchemaVersion,
    string ReportId,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);
