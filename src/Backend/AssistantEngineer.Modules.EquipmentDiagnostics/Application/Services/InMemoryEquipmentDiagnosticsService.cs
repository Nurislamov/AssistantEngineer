using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;

public sealed class InMemoryEquipmentDiagnosticsService : IEquipmentDiagnosticsService
{
    private readonly IEquipmentDiagnosticsKnowledgeSource _knowledgeSource;

    public InMemoryEquipmentDiagnosticsService(IEquipmentDiagnosticsKnowledgeSource knowledgeSource)
    {
        _knowledgeSource = knowledgeSource;
    }

    public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
        SearchEquipmentErrorCodesQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manufacturer = Normalize(query.Manufacturer);
        var errorCode = Normalize(query.ErrorCode);
        var series = Normalize(query.Series);
        var modelCode = Normalize(query.ModelCode);

        var results = _knowledgeSource.GetEntries()
            .Where(entry => Matches(entry, manufacturer, errorCode, series, modelCode, query.Category))
            .Select(entry => MapSummary(ToErrorCode(entry), entry.Category))
            .ToArray();

        return Task.FromResult<IReadOnlyList<EquipmentErrorCodeSummaryDto>>(results);
    }

    public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
        string manufacturer,
        string errorCode,
        string? series,
        string? modelCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedManufacturer = Normalize(manufacturer);
        var normalizedErrorCode = Normalize(errorCode);
        var normalizedSeries = Normalize(series);
        var normalizedModelCode = Normalize(modelCode);

        var result = _knowledgeSource.GetEntries()
            .Where(entry => Matches(
                entry,
                normalizedManufacturer,
                normalizedErrorCode,
                normalizedSeries,
                normalizedModelCode,
                category: null))
            .Select(MapCase)
            .FirstOrDefault();

        return Task.FromResult(result);
    }

    private static bool Matches(
        EquipmentDiagnosticsKnowledgeEntry entry,
        string? manufacturer,
        string? errorCode,
        string? series,
        string? modelCode,
        EquipmentCategory? category)
    {
        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            Normalize(entry.Manufacturer) != manufacturer)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(errorCode) &&
            Normalize(entry.Code) != errorCode)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(series) &&
            Normalize(entry.SeriesName) != series)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(modelCode) &&
            Normalize(entry.ModelCode) != modelCode)
        {
            return false;
        }

        return category is null || entry.Category == category;
    }

    private static DiagnosticCase ToDiagnosticCase(EquipmentDiagnosticsKnowledgeEntry entry) =>
        new(
            ErrorCode: ToErrorCode(entry),
            LikelyCauses: entry.LikelyCauses,
            DiagnosticSteps: entry.DiagnosticSteps,
            RequiredMeasurements: entry.RequiredMeasurements,
            SafetyNotes: entry.SafetyNotes,
            ManualReferences: entry.ManualReferences,
            Confidence: entry.Confidence);

    private static EquipmentErrorCode ToErrorCode(EquipmentDiagnosticsKnowledgeEntry entry) =>
        new(
            Manufacturer: new EquipmentManufacturer(
                Id: NormalizeRequired(entry.Manufacturer).ToLowerInvariant(),
                Name: entry.Manufacturer,
                NormalizedName: NormalizeRequired(entry.Manufacturer)),
            SeriesName: entry.SeriesName,
            ModelCode: entry.ModelCode,
            Code: entry.Code,
            NormalizedCode: NormalizeRequired(entry.Code),
            Title: entry.Title,
            Meaning: entry.Meaning,
            Severity: entry.Severity,
            Confidence: entry.Confidence,
            SourceManual: entry.ManualReferences.FirstOrDefault());

    private static EquipmentDiagnosticCaseDto MapCase(EquipmentDiagnosticsKnowledgeEntry entry)
    {
        var diagnosticCase = ToDiagnosticCase(entry);

        return new(
            ErrorCode: MapSummary(diagnosticCase.ErrorCode, entry.Category),
            LikelyCauses: diagnosticCase.LikelyCauses,
            DiagnosticSteps: diagnosticCase.DiagnosticSteps
                .OrderBy(step => step.Order)
                .Select(step => new DiagnosticStepDto(
                    step.Order,
                    step.Title,
                    step.Instruction,
                    step.ExpectedResult,
                    step.IfFailedAction))
                .ToArray(),
            RequiredMeasurements: diagnosticCase.RequiredMeasurements
                .Select(measurement => new RequiredMeasurementDto(
                    measurement.Name,
                    measurement.Unit,
                    measurement.Description,
                    measurement.RequiredBeforeConclusion))
                .ToArray(),
            SafetyNotes: diagnosticCase.SafetyNotes,
            ManualReferences: diagnosticCase.ManualReferences
                .Select(reference => MapManualReference(reference)!)
                .ToArray(),
            Source: MapSource(entry.Source),
            Confidence: diagnosticCase.Confidence);
    }

    private static EquipmentErrorCodeSummaryDto MapSummary(
        EquipmentErrorCode errorCode,
        EquipmentCategory category) =>
        new(
            Manufacturer: errorCode.Manufacturer.Name,
            SeriesName: errorCode.SeriesName,
            ModelCode: errorCode.ModelCode,
            Code: errorCode.Code,
            Title: errorCode.Title,
            Meaning: errorCode.Meaning,
            Severity: errorCode.Severity,
            Category: category,
            Confidence: errorCode.Confidence,
            SourceManual: MapManualReference(errorCode.SourceManual));

    private static EquipmentDiagnosticSourceDto MapSource(EquipmentDiagnosticsKnowledgeSourceInfo source) =>
        new(
            SourceType: source.SourceType,
            EvidenceLevel: source.EvidenceLevel,
            ManualTitle: source.ManualTitle,
            ManualVersion: source.ManualVersion,
            ManualDocumentCode: source.ManualDocumentCode,
            Page: source.Page,
            Section: source.Section,
            Quote: source.Quote,
            Notes: source.Notes,
            Limitations: source.Limitations,
            ApplicableModels: source.ApplicableModels,
            ApplicableSeries: source.ApplicableSeries);

    private static ManualReferenceDto? MapManualReference(ManualReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return new(
            Manufacturer: reference.Manufacturer,
            ManualTitle: reference.ManualTitle,
            ManualVersion: reference.ManualVersion,
            Page: reference.Page,
            Notes: reference.Notes);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedCharacters = value
            .Where(character => !char.IsWhiteSpace(character))
            .Select(char.ToUpperInvariant)
            .ToArray();

        return new string(normalizedCharacters);
    }

    private static string NormalizeRequired(string value) =>
        Normalize(value) ?? throw new ArgumentException("Value must contain at least one non-whitespace character.", nameof(value));
}
