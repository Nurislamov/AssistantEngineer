using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

public sealed record SearchEquipmentErrorCodesQuery(
    string? Manufacturer = null,
    string? ErrorCode = null,
    string? Series = null,
    string? ModelCode = null,
    EquipmentCategory? Category = null,
    string? Query = null);

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
    EquipmentDiagnosticSourceDto Source,
    DiagnosticConfidence Confidence);

public sealed record EquipmentDiagnosticSourceDto(
    string SourceType,
    string EvidenceLevel,
    string? ManualTitle,
    string? ManualVersion,
    string? ManualDocumentCode,
    string? Page,
    string? Section,
    string? Quote,
    string? Notes,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> ApplicableModels,
    IReadOnlyList<string> ApplicableSeries);

public sealed record EquipmentDiagnosticsCatalogIndexDto(
    int TotalEntries,
    IReadOnlyList<EquipmentDiagnosticsManufacturerFacetDto> Manufacturers,
    IReadOnlyList<EquipmentDiagnosticsSeriesFacetDto> Series,
    IReadOnlyList<EquipmentDiagnosticsCategoryFacetDto> Categories,
    IReadOnlyList<EquipmentDiagnosticsCodeFacetDto> Codes,
    IReadOnlyList<string> SourceTypes,
    IReadOnlyList<string> EvidenceLevels);

public sealed record EquipmentDiagnosticsManufacturerFacetDto(
    string Manufacturer,
    string NormalizedManufacturer,
    int Count);

public sealed record EquipmentDiagnosticsSeriesFacetDto(
    string Manufacturer,
    string NormalizedManufacturer,
    string? SeriesName,
    string? NormalizedSeriesName,
    int Count);

public sealed record EquipmentDiagnosticsCategoryFacetDto(
    EquipmentCategory Category,
    int Count);

public sealed record EquipmentDiagnosticsCodeFacetDto(
    string Manufacturer,
    string NormalizedManufacturer,
    string? SeriesName,
    string? NormalizedSeriesName,
    string? ModelCode,
    string? NormalizedModelCode,
    EquipmentCategory Category,
    string Code,
    string NormalizedCode,
    string Title,
    DiagnosticConfidence Confidence,
    string SourceType,
    string EvidenceLevel,
    int Count);

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
