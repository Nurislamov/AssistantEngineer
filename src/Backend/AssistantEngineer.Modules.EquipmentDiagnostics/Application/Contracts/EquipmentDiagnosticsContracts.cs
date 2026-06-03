using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

public sealed record SearchEquipmentErrorCodesQuery(
    string? Manufacturer = null,
    string? ErrorCode = null,
    string? Series = null,
    string? ModelCode = null,
    EquipmentCategory? Category = null);

public sealed record EquipmentErrorCodeSummaryDto(
    string Manufacturer,
    string? SeriesName,
    string? ModelCode,
    string Code,
    string Title,
    string Meaning,
    string Severity,
    EquipmentCategory Category,
    DiagnosticConfidence Confidence,
    ManualReferenceDto? SourceManual);

public sealed record EquipmentDiagnosticCaseDto(
    EquipmentErrorCodeSummaryDto ErrorCode,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<DiagnosticStepDto> DiagnosticSteps,
    IReadOnlyList<RequiredMeasurementDto> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<ManualReferenceDto> ManualReferences,
    DiagnosticConfidence Confidence);

public sealed record DiagnosticStepDto(
    int Order,
    string Title,
    string Instruction,
    string ExpectedResult,
    string IfFailedAction);

public sealed record RequiredMeasurementDto(
    string Name,
    string Unit,
    string Description,
    bool RequiredBeforeConclusion);

public sealed record ManualReferenceDto(
    string Manufacturer,
    string ManualTitle,
    string? ManualVersion,
    string? Page,
    string? Notes);
