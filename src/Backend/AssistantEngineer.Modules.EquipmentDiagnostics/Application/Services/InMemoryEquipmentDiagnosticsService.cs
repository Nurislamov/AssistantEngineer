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
        var errorCode = NormalizeCode(query.ErrorCode);
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
        var normalizedErrorCode = NormalizeCode(errorCode);
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

    public Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = _knowledgeSource.GetEntries()
            .OrderBy(entry => entry.Manufacturer, StringComparer.Ordinal)
            .ThenBy(entry => entry.SeriesName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Category.ToString(), StringComparer.Ordinal)
            .ThenBy(entry => NormalizeCodeRequired(entry.Code), StringComparer.Ordinal)
            .ThenBy(entry => entry.ModelCode ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        EnsureNoDuplicateCatalogKeys(entries);

        var index = new EquipmentDiagnosticsCatalogIndexDto(
            TotalEntries: entries.Length,
            Manufacturers: entries
                .GroupBy(
                    entry => NormalizeRequired(entry.Manufacturer),
                    StringComparer.Ordinal)
                .Select(group =>
                {
                    var first = group
                        .OrderBy(entry => entry.Manufacturer, StringComparer.Ordinal)
                        .First();

                    return new EquipmentDiagnosticsManufacturerFacetDto(
                        Manufacturer: first.Manufacturer,
                        NormalizedManufacturer: group.Key,
                        Count: group.Count());
                })
                .OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal)
                .ToArray(),
            Series: entries
                .GroupBy(
                    entry => new
                    {
                        Manufacturer = NormalizeRequired(entry.Manufacturer),
                        Series = Normalize(entry.SeriesName)
                    })
                .Select(group =>
                {
                    var first = group
                        .OrderBy(entry => entry.Manufacturer, StringComparer.Ordinal)
                        .ThenBy(entry => entry.SeriesName ?? string.Empty, StringComparer.Ordinal)
                        .First();

                    return new EquipmentDiagnosticsSeriesFacetDto(
                        Manufacturer: first.Manufacturer,
                        NormalizedManufacturer: group.Key.Manufacturer,
                        SeriesName: first.SeriesName,
                        NormalizedSeriesName: group.Key.Series,
                        Count: group.Count());
                })
                .OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal)
                .ThenBy(facet => facet.SeriesName ?? string.Empty, StringComparer.Ordinal)
                .ToArray(),
            Categories: entries
                .GroupBy(entry => entry.Category)
                .Select(group => new EquipmentDiagnosticsCategoryFacetDto(
                    Category: group.Key,
                    Count: group.Count()))
                .OrderBy(facet => facet.Category.ToString(), StringComparer.Ordinal)
                .ToArray(),
            Codes: entries
                .Select(entry => new EquipmentDiagnosticsCodeFacetDto(
                    Manufacturer: entry.Manufacturer,
                    NormalizedManufacturer: NormalizeRequired(entry.Manufacturer),
                    SeriesName: entry.SeriesName,
                    NormalizedSeriesName: Normalize(entry.SeriesName),
                    ModelCode: entry.ModelCode,
                    NormalizedModelCode: Normalize(entry.ModelCode),
                    Category: entry.Category,
                    Code: entry.Code,
                    NormalizedCode: NormalizeCodeRequired(entry.Code),
                    Title: entry.Title,
                    Confidence: entry.Confidence,
                    SourceType: entry.Source.SourceType,
                    EvidenceLevel: entry.Source.EvidenceLevel,
                    Count: 1))
                .OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal)
                .ThenBy(facet => facet.SeriesName ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(facet => facet.Category.ToString(), StringComparer.Ordinal)
                .ThenBy(facet => facet.NormalizedCode, StringComparer.Ordinal)
                .ThenBy(facet => facet.ModelCode ?? string.Empty, StringComparer.Ordinal)
                .ToArray(),
            SourceTypes: entries
                .Select(entry => entry.Source.SourceType)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(sourceType => sourceType, StringComparer.Ordinal)
                .ToArray(),
            EvidenceLevels: entries
                .Select(entry => entry.Source.EvidenceLevel)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(evidenceLevel => evidenceLevel, StringComparer.Ordinal)
                .ToArray());

        return Task.FromResult(index);
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
            NormalizeCode(entry.Code) != errorCode)
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
            NormalizedCode: NormalizeCodeRequired(entry.Code),
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

    private static string? NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedCharacters = value
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray();

        if (normalizedCharacters.Length == 0)
        {
            return null;
        }

        return new string(normalizedCharacters);
    }

    private static string NormalizeRequired(string value) =>
        Normalize(value) ?? throw new ArgumentException("Value must contain at least one non-whitespace character.", nameof(value));

    private static string NormalizeCodeRequired(string value) =>
        NormalizeCode(value) ?? throw new ArgumentException("Code must contain at least one searchable character.", nameof(value));

    private static void EnsureNoDuplicateCatalogKeys(
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries)
    {
        var duplicates = entries
            .GroupBy(
                entry => string.Join(
                    "/",
                    NormalizeRequired(entry.Manufacturer),
                    Normalize(entry.SeriesName) ?? string.Empty,
                    entry.Category,
                    NormalizeCodeRequired(entry.Code)),
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Equipment diagnostics catalog contains duplicate manufacturer/series/category/code combinations: {string.Join(", ", duplicates)}.");
        }
    }
}
