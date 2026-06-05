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

        var manufacturer = NormalizeIdentifier(query.Manufacturer);
        var errorCode = NormalizeCode(query.ErrorCode);
        var series = NormalizeSeries(query.Series);
        var modelCode = Normalize(query.ModelCode);
        var queryTokens = NormalizeQueryTokens(query.Query);

        var results = _knowledgeSource.GetEntries()
            .Select(entry => new SearchCandidate(
                Entry: entry,
                Score: CalculateQueryScore(entry, queryTokens)))
            .Where(candidate =>
                MatchesExplicitFilters(candidate.Entry, manufacturer, errorCode, series, modelCode, query.Category) &&
                MatchesTextQuery(candidate, queryTokens))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Entry.Manufacturer, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Entry.SeriesName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Entry.Category.ToString(), StringComparer.Ordinal)
            .ThenBy(candidate => NormalizeCodeRequired(candidate.Entry.Code), StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Entry.ModelCode ?? string.Empty, StringComparer.Ordinal)
            .Select(candidate => MapSummary(ToErrorCode(candidate.Entry), candidate.Entry.Category))
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

        var normalizedManufacturer = NormalizeIdentifier(manufacturer);
        var normalizedErrorCode = NormalizeCode(errorCode);
        var normalizedSeries = NormalizeSeries(series);
        var normalizedModelCode = Normalize(modelCode);

        var result = _knowledgeSource.GetEntries()
            .Where(entry => MatchesExplicitFilters(
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
                        Series = NormalizeSeries(entry.SeriesName)
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
                    NormalizedSeriesName: NormalizeSeries(entry.SeriesName),
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

    private static bool MatchesExplicitFilters(
        EquipmentDiagnosticsKnowledgeEntry entry,
        string? manufacturer,
        string? errorCode,
        string? series,
        string? modelCode,
        EquipmentCategory? category)
    {
        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            NormalizeIdentifier(entry.Manufacturer) != manufacturer)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(errorCode) &&
            NormalizeCode(entry.Code) != errorCode)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(series) &&
            !MatchesSeries(entry, series))
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

    private static bool MatchesTextQuery(
        SearchCandidate candidate,
        IReadOnlyCollection<SearchToken> queryTokens) =>
        queryTokens.Count == 0 ||
        (candidate.Score > 0 && queryTokens.All(token => TokenMatches(candidate.Entry, token)));

    private static int CalculateQueryScore(
        EquipmentDiagnosticsKnowledgeEntry entry,
        IReadOnlyCollection<SearchToken> queryTokens)
    {
        if (queryTokens.Count == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var token in queryTokens)
        {
            if (NormalizeCode(entry.Code) == token.Code)
            {
                score += 100;
                continue;
            }

            if (token.Text.Length == 1)
            {
                if (token.Code is not null &&
                    NormalizeCode(entry.Code)?.Contains(token.Code, StringComparison.Ordinal) == true)
                {
                    score += 40;
                }

                continue;
            }

            if (FieldMatches(token, entry.Manufacturer, NormalizeIdentifier) ||
                MatchesSeriesToken(entry, token) ||
                FieldMatches(token, entry.Category.ToString(), NormalizeIdentifier) ||
                CategorySynonyms(entry.Category).Contains(token.Text, StringComparer.Ordinal))
            {
                score += 50;
                continue;
            }

            if (FieldMatches(token, entry.ModelCode, Normalize) ||
                (entry.Tags ?? []).Any(tag => FieldMatches(token, tag, NormalizeIdentifier)) ||
                FieldMatches(token, entry.Title, NormalizeContains))
            {
                score += 25;
                continue;
            }

            if (FieldMatches(token, entry.Meaning, NormalizeContains) ||
                entry.LikelyCauses.Any(cause => FieldMatches(token, cause, NormalizeContains)))
            {
                score += 10;
            }
        }

        return score;
    }

    private static bool TokenMatches(
        EquipmentDiagnosticsKnowledgeEntry entry,
        SearchToken token)
    {
        if (NormalizeCode(entry.Code) == token.Code)
        {
            return true;
        }

        if (token.Text.Length == 1)
        {
            return token.Code is not null &&
                NormalizeCode(entry.Code)?.Contains(token.Code, StringComparison.Ordinal) == true;
        }

        return FieldMatches(token, entry.Manufacturer, NormalizeIdentifier) ||
            MatchesSeriesToken(entry, token) ||
            FieldMatches(token, entry.ModelCode, Normalize) ||
            FieldMatches(token, entry.Category.ToString(), NormalizeIdentifier) ||
            CategorySynonyms(entry.Category).Contains(token.Text, StringComparer.Ordinal) ||
            (entry.Tags ?? []).Any(tag => FieldMatches(token, tag, NormalizeIdentifier)) ||
            FieldMatches(token, entry.Title, NormalizeContains) ||
            FieldMatches(token, entry.Meaning, NormalizeContains) ||
            entry.LikelyCauses.Any(cause => FieldMatches(token, cause, NormalizeContains));
    }

    private static bool FieldMatches(
        SearchToken token,
        string? value,
        Func<string?, string?> normalize)
    {
        var normalized = normalize(value);
        return normalized is not null &&
            (normalized == token.Text ||
             normalized.Contains(token.Text, StringComparison.Ordinal) ||
             (token.Code is not null && normalized.Contains(token.Code, StringComparison.Ordinal)));
    }

    private static bool MatchesSeries(
        EquipmentDiagnosticsKnowledgeEntry entry,
        string normalizedSeries) =>
        NormalizeSeries(entry.SeriesName) == normalizedSeries ||
        (normalizedSeries == "VRF" && entry.Category is EquipmentCategory.VrfOutdoorUnit or EquipmentCategory.VrfIndoorUnit);

    private static bool MatchesSeriesToken(
        EquipmentDiagnosticsKnowledgeEntry entry,
        SearchToken token) =>
        FieldMatches(token, entry.SeriesName, NormalizeSeries) ||
        (token.Text == "VRF" && entry.Category is EquipmentCategory.VrfOutdoorUnit or EquipmentCategory.VrfIndoorUnit);

    private static IReadOnlyCollection<string> CategorySynonyms(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfOutdoorUnit => ["OUTDOOR", "OUTDOORUNIT", "VRFOUTDOOR", "VRF"],
            EquipmentCategory.VrfIndoorUnit => ["INDOOR", "INDOORUNIT", "VRFINDOOR", "VRF"],
            EquipmentCategory.Chiller => ["CHILLER", "WATERCHILLER"],
            EquipmentCategory.RooftopUnit => ["ROOFTOP", "RTU", "ROOFTOPUNIT"],
            EquipmentCategory.SplitSystem => ["SPLIT", "SPLITSYSTEM"],
            EquipmentCategory.AirHandlingUnit => ["AHU", "AIRHANDLING", "AIRHANDLINGUNIT"],
            EquipmentCategory.Controller => ["CONTROLLER", "CONTROL"],
            _ => ["UNKNOWN"]
        };

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
        var orderedSteps = diagnosticCase.DiagnosticSteps
            .OrderBy(step => step.Order)
            .ToArray();
        var requiredMeasurements = diagnosticCase.RequiredMeasurements
            .ToArray();

        return new(
            ErrorCode: MapSummary(diagnosticCase.ErrorCode, entry.Category),
            LikelyCauses: diagnosticCase.LikelyCauses,
            DiagnosticSteps: orderedSteps
                .Select(step => new DiagnosticStepDto(
                    step.Order,
                    step.Title,
                    step.Instruction,
                    step.ExpectedResult,
                    step.IfFailedAction))
                .ToArray(),
            RequiredMeasurements: requiredMeasurements
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
            Confidence: diagnosticCase.Confidence,
            ShortSummary: BuildShortSummary(entry),
            RecommendedNextChecks: BuildRecommendedNextChecks(orderedSteps, requiredMeasurements),
            ConfidenceExplanation: BuildConfidenceExplanation(entry),
            SourceSummary: BuildSourceSummary(entry.Source),
            ApplicabilitySummary: BuildApplicabilitySummary(entry.Source),
            SafetyBoundary: BuildSafetyBoundary(entry),
            OperatorNotes: BuildOperatorNotes(entry),
            IsManualVerified: IsManualVerified(entry),
            IsSeedKnowledge: IsSeedKnowledge(entry),
            VerificationRequired: IsVerificationRequired(entry));
    }

    private static string BuildShortSummary(EquipmentDiagnosticsKnowledgeEntry entry) =>
        $"{entry.Manufacturer} {entry.SeriesName ?? entry.Category.ToString()} {entry.Code}: {entry.Title}. {entry.Meaning}";

    private static IReadOnlyList<string> BuildRecommendedNextChecks(
        IReadOnlyList<DiagnosticStep> orderedSteps,
        IReadOnlyList<RequiredMeasurement> requiredMeasurements)
    {
        var stepChecks = orderedSteps
            .Take(3)
            .Select(step => $"Step {step.Order}: {step.Title} - {step.Instruction}")
            .ToArray();
        var measurementChecks = requiredMeasurements
            .Where(measurement => measurement.RequiredBeforeConclusion)
            .Take(3)
            .Select(measurement => $"Record {measurement.Name} ({measurement.Unit}) before drawing a conclusion.")
            .ToArray();

        return stepChecks
            .Concat(measurementChecks)
            .DefaultIfEmpty("Record installed equipment identity and required measurements before drawing a conclusion.")
            .ToArray();
    }

    private static string BuildConfidenceExplanation(EquipmentDiagnosticsKnowledgeEntry entry) =>
        (entry.Confidence, entry.Source.EvidenceLevel) switch
        {
            (DiagnosticConfidence.Low, "UnverifiedSeed") =>
                "Low confidence seeded guidance: use as preliminary diagnostic support only and verify the exact installed model, controller, and service manual before drawing a conclusion.",
            (DiagnosticConfidence.ManualVerified, "ManualPageVerified") =>
                "Manual verified guidance: the catalog entry has page-level manual evidence. Confirm the installed model and applicability before drawing a conclusion.",
            (DiagnosticConfidence.ManualVerified, "CrossChecked") =>
                "Manual verified guidance: the catalog entry is supported by cross-checked source evidence. Confirm the installed model and applicability before drawing a conclusion.",
            (_, "ManualPageVerified" or "CrossChecked") =>
                $"{entry.Confidence} confidence source-backed guidance: verify applicability to the installed model before drawing a conclusion.",
            _ =>
                $"{entry.Confidence} confidence guidance: verify the exact installed model, controller, and service manual before drawing a conclusion."
        };

    private static string BuildSourceSummary(EquipmentDiagnosticsKnowledgeSourceInfo source)
    {
        var manualPart = source.ManualTitle is null
            ? "No manual title/page evidence is attached to this runtime entry."
            : $"Manual evidence: {source.ManualTitle}{FormatOptional(source.ManualVersion, " version ")}{FormatOptional(source.Page, " page ")}.";

        return $"{source.SourceType} / {source.EvidenceLevel}. {manualPart}";
    }

    private static string BuildApplicabilitySummary(EquipmentDiagnosticsKnowledgeSourceInfo source)
    {
        var series = source.ApplicableSeries.Count == 0
            ? "No specific applicable series listed"
            : $"Applicable series: {string.Join(", ", source.ApplicableSeries)}";
        var models = source.ApplicableModels.Count == 0
            ? "no specific applicable models listed"
            : $"applicable models: {string.Join(", ", source.ApplicableModels)}";
        var limitations = source.Limitations.Count == 0
            ? "No limitations are listed."
            : $"Limitations: {string.Join(" ", source.Limitations)}";

        return $"{series}; {models}. {limitations}";
    }

    private static string BuildSafetyBoundary(EquipmentDiagnosticsKnowledgeEntry entry)
    {
        var categoryBoundary = entry.Category switch
        {
            EquipmentCategory.Chiller =>
                "Chiller electrical, compressor, refrigerant, hydronic, and protection checks must stay within qualified-technician service scope.",
            EquipmentCategory.VrfOutdoorUnit =>
                "VRF outdoor electrical, compressor, inverter, refrigerant, and protection checks must stay within qualified-technician service scope.",
            EquipmentCategory.VrfIndoorUnit =>
                "Indoor-unit electrical and controller checks must stay within qualified-technician service scope.",
            _ =>
                "Equipment electrical, controller, refrigerant, and protection checks must stay within qualified-technician service scope."
        };

        var firstSafetyNote = entry.SafetyNotes.FirstOrDefault();
        return firstSafetyNote is null
            ? categoryBoundary
            : $"{categoryBoundary} Catalog safety note: {firstSafetyNote}";
    }

    private static IReadOnlyList<string> BuildOperatorNotes(EquipmentDiagnosticsKnowledgeEntry entry)
    {
        var notes = new List<string>
        {
            "Do not treat this response as a final diagnosis.",
            "Verify the installed model, controller, and exact service manual before final conclusion.",
            "Record required measurements before drawing a conclusion."
        };

        if (IsSeedKnowledge(entry))
        {
            notes.Add("This runtime entry is deterministic seed knowledge and is not manual page verified.");
        }

        return notes;
    }

    private static bool IsManualVerified(EquipmentDiagnosticsKnowledgeEntry entry) =>
        entry.Confidence == DiagnosticConfidence.ManualVerified &&
        entry.Source.EvidenceLevel is "ManualPageVerified" or "CrossChecked";

    private static bool IsSeedKnowledge(EquipmentDiagnosticsKnowledgeEntry entry) =>
        entry.Source.SourceType == "SeededEngineeringKnowledge" ||
        entry.Source.EvidenceLevel == "UnverifiedSeed";

    private static bool IsVerificationRequired(EquipmentDiagnosticsKnowledgeEntry entry) =>
        !IsManualVerified(entry) ||
        entry.Source.Limitations.Count > 0;

    private static string FormatOptional(string? value, string prefix) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $"{prefix}{value}";

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

    private static IReadOnlyCollection<SearchToken> NormalizeQueryTokens(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return query
            .Split([' ', '\t', '\r', '\n', ',', ';', ':', '/', '\\', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new SearchToken(
                Text: NormalizeIdentifier(token) ?? string.Empty,
                Code: NormalizeCode(token)))
            .Where(token => token.Text.Length > 0)
            .DistinctBy(token => token.Text, StringComparer.Ordinal)
            .ToArray();
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

    private static string? NormalizeIdentifier(string? value)
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

    private static string? NormalizeSeries(string? value)
    {
        var normalized = NormalizeIdentifier(value);
        return normalized switch
        {
            "GMV" or "GREEGMV" => "GMV",
            "VRF" or "VRFSYSTEM" => "VRF",
            _ => normalized
        };
    }

    private static string? NormalizeContains(string? value) =>
        NormalizeIdentifier(value);

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

    private sealed record SearchCandidate(
        EquipmentDiagnosticsKnowledgeEntry Entry,
        int Score);

    private sealed record SearchToken(
        string Text,
        string? Code);
}
