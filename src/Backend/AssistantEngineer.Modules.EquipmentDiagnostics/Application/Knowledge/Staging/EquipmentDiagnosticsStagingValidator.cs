using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;

public sealed class EquipmentDiagnosticsStagingValidator : IEquipmentDiagnosticsStagingValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly string[] AllowedReviewStatuses =
    [
        "Draft",
        "NeedsManualCheck",
        "ReadyForReview",
        "ApprovedForCatalog",
        "Rejected"
    ];

    private static readonly string[] AllowedSourceTypes =
    [
        "SeededEngineeringKnowledge",
        "ManufacturerDocumentation",
        "ServiceManual",
        "FieldObservation",
        "CrossCheckedManuals"
    ];

    private static readonly string[] AllowedEvidenceLevels =
    [
        "UnverifiedSeed",
        "ManualReferenced",
        "ManualPageVerified",
        "FieldObserved",
        "CrossChecked"
    ];

    private static readonly string[] UnsafeTextFragments =
    [
        "bypass",
        "disable protections",
        "disable protection",
        "force run",
        "short protection",
        "ignore protection"
    ];

    public EquipmentDiagnosticsStagingValidationResult ValidateJson(
        string json,
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> productionEntries,
        string sourceName = "staging-candidates.json")
    {
        try
        {
            var file = JsonSerializer.Deserialize<EquipmentDiagnosticsStagingCandidateFile>(json, JsonOptions);
            if (file?.Candidates is null)
            {
                var issues = new[]
                {
                    Error(
                        "MissingCandidates",
                        sourceName,
                        "Staging JSON must contain a candidates array.")
                };

                return new EquipmentDiagnosticsStagingValidationResult(
                    issues,
                    BuildReport(candidateCount: 0, [], [], issues, new Dictionary<string, string>(StringComparer.Ordinal)));
            }

            return ValidateCandidates(file.Candidates, productionEntries, sourceName);
        }
        catch (JsonException exception)
        {
            var issues = new[]
            {
                Error(
                    "InvalidJson",
                    sourceName,
                    $"Staging JSON is invalid: {exception.Message}")
            };

            return new EquipmentDiagnosticsStagingValidationResult(
                issues,
                BuildReport(candidateCount: 0, [], [], issues, new Dictionary<string, string>(StringComparer.Ordinal)));
        }
    }

    public EquipmentDiagnosticsStagingValidationResult ValidateCandidates(
        IReadOnlyCollection<EquipmentDiagnosticsStagingCandidate> candidates,
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> productionEntries,
        string sourceName = "staging-candidates")
    {
        var issues = new List<EquipmentDiagnosticsStagingValidationIssue>();

        if (candidates.Count == 0)
        {
            issues.Add(Error(
                "MissingCandidates",
                sourceName,
                "Staging candidate list must contain at least one candidate."));
            return new EquipmentDiagnosticsStagingValidationResult(
                issues,
                BuildReport(candidateCount: 0, [], [], issues, new Dictionary<string, string>(StringComparer.Ordinal)));
        }

        var productionKeys = productionEntries
            .Select(CreateProductionKey)
            .ToHashSet(StringComparer.Ordinal);

        var candidateKeys = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var candidateKeyByPath = new Dictionary<string, string>(StringComparer.Ordinal);
        var reviewStatuses = new List<string?>();

        var index = 0;
        foreach (var candidate in candidates)
        {
            var path = $"{sourceName}:candidates[{index}]";
            var reportKey = TryCreateCandidateKeyForReport(candidate) ?? $"{sourceName}:candidates[{index}]";
            candidateKeyByPath.Add(path, reportKey);
            reviewStatuses.Add(candidate.ReviewStatus);
            ValidateCandidate(candidate, path, productionKeys, candidateKeys, issues);
            index++;
        }

        foreach (var duplicate in candidateKeys
                     .Where(pair => pair.Value.Count > 1)
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            issues.Add(Error(
                "DuplicateCandidateKey",
                string.Join(", ", duplicate.Value),
                $"Staging candidates contain duplicate manufacturer/series/category/modelCode/code key '{duplicate.Key}'."));
        }

        var sortedIssues = issues
                .OrderBy(issue => issue.Severity)
                .ThenBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ToArray();

        return new EquipmentDiagnosticsStagingValidationResult(
            sortedIssues,
            BuildReport(candidates.Count, candidateKeyByPath.Values, reviewStatuses, sortedIssues, candidateKeyByPath));
    }

    private static void ValidateCandidate(
        EquipmentDiagnosticsStagingCandidate candidate,
        string path,
        IReadOnlySet<string> productionKeys,
        IDictionary<string, List<string>> candidateKeys,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        RequireText(candidate.Manufacturer, path, "manufacturer", issues);
        RequireText(candidate.Series, path, "series", issues);
        RequireText(candidate.Category, path, "category", issues);
        RequireText(candidate.Code, path, "code", issues);
        RequireText(candidate.Title, path, "title", issues);
        RequireText(candidate.Meaning, path, "meaning", issues);
        RequireText(candidate.Severity, path, "severity", issues);
        RequireText(candidate.ProposedConfidence, path, "proposedConfidence", issues);
        RequireText(candidate.ReviewStatus, path, "reviewStatus", issues);
        RequireTextArray(candidate.LikelyCauses, path, "likelyCauses", issues);
        RequireSteps(candidate.DiagnosticSteps, path, issues);
        RequireMeasurements(candidate.RequiredMeasurements, path, issues);
        RequireTextArray(candidate.SafetyNotes, path, "safetyNotes", issues);
        RequireTextArray(candidate.Tags, path, "tags", issues);
        RequireTextArray(candidate.PromotionNotes, path, "promotionNotes", issues);

        var category = ParseEnum<EquipmentCategory>(candidate.Category, path, "category", issues);
        var confidence = ParseEnum<DiagnosticConfidence>(candidate.ProposedConfidence, path, "proposedConfidence", issues);
        var reviewStatus = RequireAllowedText(candidate.ReviewStatus, path, "reviewStatus", AllowedReviewStatuses, issues);

        var source = candidate.Source;
        if (source is null)
        {
            issues.Add(Error(
                "MissingSource",
                $"{path}.source",
                "Staging candidate must include source evidence block."));
        }
        else
        {
            ValidateSource(source, path, confidence, reviewStatus, issues);
        }

        if (reviewStatus is "Draft" or "NeedsManualCheck")
        {
            issues.Add(Info(
                "CandidateNotReadyForRuntimeCatalog",
                $"{path}.reviewStatus",
                $"{reviewStatus} staging candidates are not approved runtime catalog entries."));
        }

        var key = TryCreateCandidateKey(candidate, category);
        if (key is not null)
        {
            if (!candidateKeys.TryGetValue(key, out var paths))
            {
                paths = [];
                candidateKeys.Add(key, paths);
            }

            paths.Add(path);

            if (productionKeys.Contains(key))
            {
                issues.Add(Error(
                    "CandidateConflictsWithProductionCatalog",
                    path,
                    $"Staging candidate key '{key}' already exists in the production diagnostics catalog."));
            }
        }

        ValidateUnsafeText(candidate, path, issues);
    }

    private static void ValidateSource(
        EquipmentDiagnosticsStagingSourceInfo source,
        string path,
        DiagnosticConfidence? confidence,
        string? reviewStatus,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        var sourcePath = $"{path}.source";
        var sourceType = RequireAllowedText(source.SourceType, sourcePath, "sourceType", AllowedSourceTypes, issues);
        var evidenceLevel = RequireAllowedText(source.EvidenceLevel, sourcePath, "evidenceLevel", AllowedEvidenceLevels, issues);

        RequireTextArray(source.Limitations, sourcePath, "limitations", issues);
        RequireArrayPresent(source.ApplicableModels, sourcePath, "applicableModels", issues);
        RequireArrayPresent(source.ApplicableSeries, sourcePath, "applicableSeries", issues);

        if (confidence == DiagnosticConfidence.ManualVerified &&
            evidenceLevel is not ("ManualPageVerified" or "CrossChecked"))
        {
            issues.Add(Error(
                "ManualVerifiedRequiresVerifiedEvidence",
                $"{path}.proposedConfidence",
                "ManualVerified proposed confidence requires ManualPageVerified or CrossChecked evidence."));
        }

        if (evidenceLevel == "ManualPageVerified")
        {
            if (string.IsNullOrWhiteSpace(source.ManualTitle))
            {
                issues.Add(Error(
                    "ManualPageVerifiedRequiresManualTitle",
                    $"{sourcePath}.manualTitle",
                    "ManualPageVerified evidence requires manualTitle."));
            }

            if (string.IsNullOrWhiteSpace(source.Page))
            {
                issues.Add(Error(
                    "ManualPageVerifiedRequiresPage",
                    $"{sourcePath}.page",
                    "ManualPageVerified evidence requires page."));
            }
        }

        if (evidenceLevel == "CrossChecked" &&
            string.IsNullOrWhiteSpace(source.Notes))
        {
            issues.Add(Error(
                "CrossCheckedRequiresEvidenceNotes",
                $"{sourcePath}.notes",
                "CrossChecked evidence requires source notes explaining the cross-check."));
        }

        if (reviewStatus == "ApprovedForCatalog")
        {
            if (evidenceLevel == "UnverifiedSeed")
            {
                issues.Add(Error(
                    "ApprovedForCatalogRequiresVerifiedEvidence",
                    $"{sourcePath}.evidenceLevel",
                    "ApprovedForCatalog candidates must not use UnverifiedSeed evidence."));
            }

            if (sourceType == "SeededEngineeringKnowledge")
            {
                issues.Add(Error(
                    "ApprovedForCatalogRequiresExternalSource",
                    $"{sourcePath}.sourceType",
                    "ApprovedForCatalog candidates require manual, manufacturer, field, or cross-checked source evidence."));
            }

            if (evidenceLevel == "ManualReferenced" &&
                string.IsNullOrWhiteSpace(source.ManualTitle))
            {
                issues.Add(Error(
                    "ManualReferencedRequiresManualTitle",
                    $"{sourcePath}.manualTitle",
                    "ManualReferenced approval requires manualTitle."));
            }

            if (evidenceLevel == "FieldObserved" &&
                string.IsNullOrWhiteSpace(source.Notes))
            {
                issues.Add(Error(
                    "FieldObservedRequiresEvidenceNotes",
                    $"{sourcePath}.notes",
                    "FieldObserved approval requires source notes."));
            }
        }

        if (!string.IsNullOrWhiteSpace(source.Quote) &&
            source.Quote.Length > 240)
        {
            issues.Add(Error(
                "QuoteTooLong",
                $"{sourcePath}.quote",
                "Staging quote must be short and no longer than 240 characters."));
        }
    }

    private static void RequireSteps(
        IReadOnlyList<EquipmentDiagnosticsStagingDiagnosticStep>? steps,
        string path,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        if (steps is null || steps.Count == 0)
        {
            issues.Add(Error(
                "MissingDiagnosticSteps",
                $"{path}.diagnosticSteps",
                "Staging candidate must include at least one diagnostic step."));
            return;
        }

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var stepPath = $"{path}.diagnosticSteps[{index}]";
            if (step.Order < 1)
            {
                issues.Add(Error(
                    "InvalidDiagnosticStepOrder",
                    $"{stepPath}.order",
                    "Diagnostic step order must be greater than or equal to 1."));
            }

            RequireText(step.Title, stepPath, "title", issues);
            RequireText(step.Instruction, stepPath, "instruction", issues);
            RequireText(step.ExpectedResult, stepPath, "expectedResult", issues);
            RequireText(step.IfFailedAction, stepPath, "ifFailedAction", issues);
        }
    }

    private static void RequireMeasurements(
        IReadOnlyList<EquipmentDiagnosticsStagingRequiredMeasurement>? measurements,
        string path,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        if (measurements is null || measurements.Count == 0)
        {
            issues.Add(Error(
                "MissingRequiredMeasurements",
                $"{path}.requiredMeasurements",
                "Staging candidate must include at least one required measurement."));
            return;
        }

        for (var index = 0; index < measurements.Count; index++)
        {
            var measurement = measurements[index];
            var measurementPath = $"{path}.requiredMeasurements[{index}]";
            RequireText(measurement.Name, measurementPath, "name", issues);
            RequireText(measurement.Unit, measurementPath, "unit", issues);
            RequireText(measurement.Description, measurementPath, "description", issues);
        }
    }

    private static void ValidateUnsafeText(
        EquipmentDiagnosticsStagingCandidate candidate,
        string path,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        var textValues = EnumerateCandidateText(candidate).ToArray();

        var unsafeFragments = UnsafeTextFragments
            .Where(fragment => textValues.Any(text =>
                text.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(fragment => fragment, StringComparer.Ordinal)
            .ToArray();

        if (unsafeFragments.Length > 0)
        {
            issues.Add(Error(
                "UnsafeDiagnosticWording",
                path,
                $"Staging candidate contains unsafe diagnostic wording fragments: {string.Join(", ", unsafeFragments)}."));
        }
    }

    private static IEnumerable<string> EnumerateCandidateText(EquipmentDiagnosticsStagingCandidate candidate)
    {
        yield return candidate.Manufacturer ?? string.Empty;
        yield return candidate.Series ?? string.Empty;
        yield return candidate.Category ?? string.Empty;
        yield return candidate.ModelCode ?? string.Empty;
        yield return candidate.Code ?? string.Empty;
        yield return candidate.Title ?? string.Empty;
        yield return candidate.Meaning ?? string.Empty;
        yield return candidate.Severity ?? string.Empty;
        yield return candidate.ProposedConfidence ?? string.Empty;
        yield return candidate.ReviewStatus ?? string.Empty;

        foreach (var value in candidate.LikelyCauses ?? [])
        {
            yield return value;
        }

        foreach (var step in candidate.DiagnosticSteps ?? [])
        {
            yield return step.Title ?? string.Empty;
            yield return step.Instruction ?? string.Empty;
            yield return step.ExpectedResult ?? string.Empty;
            yield return step.IfFailedAction ?? string.Empty;
        }

        foreach (var measurement in candidate.RequiredMeasurements ?? [])
        {
            yield return measurement.Name ?? string.Empty;
            yield return measurement.Unit ?? string.Empty;
            yield return measurement.Description ?? string.Empty;
        }

        foreach (var value in candidate.SafetyNotes ?? [])
        {
            yield return value;
        }

        foreach (var value in candidate.Tags ?? [])
        {
            yield return value;
        }

        foreach (var value in candidate.PromotionNotes ?? [])
        {
            yield return value;
        }

        if (candidate.Source is null)
        {
            yield break;
        }

        yield return candidate.Source.SourceType ?? string.Empty;
        yield return candidate.Source.EvidenceLevel ?? string.Empty;
        yield return candidate.Source.ManualTitle ?? string.Empty;
        yield return candidate.Source.ManualVersion ?? string.Empty;
        yield return candidate.Source.ManualDocumentCode ?? string.Empty;
        yield return candidate.Source.Page ?? string.Empty;
        yield return candidate.Source.Section ?? string.Empty;
        yield return candidate.Source.Quote ?? string.Empty;
        yield return candidate.Source.Notes ?? string.Empty;

        foreach (var value in candidate.Source.Limitations ?? [])
        {
            yield return value;
        }

        foreach (var value in candidate.Source.ApplicableModels ?? [])
        {
            yield return value;
        }

        foreach (var value in candidate.Source.ApplicableSeries ?? [])
        {
            yield return value;
        }
    }

    private static void RequireText(
        string? value,
        string path,
        string propertyName,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(
                "MissingRequiredField",
                $"{path}.{propertyName}",
                $"{propertyName} must be present and non-empty."));
        }
    }

    private static void RequireTextArray(
        IReadOnlyList<string>? values,
        string path,
        string propertyName,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        if (values is null || values.Count == 0)
        {
            issues.Add(Error(
                "MissingRequiredArray",
                $"{path}.{propertyName}",
                $"{propertyName} must contain at least one value."));
            return;
        }

        for (var index = 0; index < values.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(values[index]))
            {
                issues.Add(Error(
                    "MissingRequiredArrayValue",
                    $"{path}.{propertyName}[{index}]",
                    $"{propertyName}[{index}] must be non-empty."));
            }
        }
    }

    private static void RequireArrayPresent(
        IReadOnlyList<string>? values,
        string path,
        string propertyName,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        if (values is null)
        {
            issues.Add(Error(
                "MissingRequiredArray",
                $"{path}.{propertyName}",
                $"{propertyName} must be present."));
        }
    }

    private static string? RequireAllowedText(
        string? value,
        string path,
        string propertyName,
        IReadOnlyCollection<string> allowedValues,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
    {
        RequireText(value, path, propertyName, issues);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!allowedValues.Contains(value, StringComparer.Ordinal))
        {
            issues.Add(Error(
                "UnsupportedValue",
                $"{path}.{propertyName}",
                $"{propertyName} has unsupported value '{value}'. Allowed values: {string.Join(", ", allowedValues)}."));
            return null;
        }

        return value;
    }

    private static TEnum? ParseEnum<TEnum>(
        string? value,
        string path,
        string propertyName,
        ICollection<EquipmentDiagnosticsStagingValidationIssue> issues)
        where TEnum : struct, Enum
    {
        RequireText(value, path, propertyName, issues);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed))
        {
            issues.Add(Error(
                "UnsupportedValue",
                $"{path}.{propertyName}",
                $"{propertyName} has unsupported value '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}."));
            return null;
        }

        return parsed;
    }

    private static string? TryCreateCandidateKey(
        EquipmentDiagnosticsStagingCandidate candidate,
        EquipmentCategory? category)
    {
        if (category is null ||
            string.IsNullOrWhiteSpace(candidate.Manufacturer) ||
            string.IsNullOrWhiteSpace(candidate.Series) ||
            string.IsNullOrWhiteSpace(candidate.Code))
        {
            return null;
        }

        return string.Join(
            "/",
            NormalizeText(candidate.Manufacturer),
            NormalizeText(candidate.Series),
            category.Value,
            NormalizeText(candidate.ModelCode) ?? string.Empty,
            NormalizeCode(candidate.Code));
    }

    private static string CreateProductionKey(EquipmentDiagnosticsKnowledgeEntry entry) =>
        string.Join(
            "/",
            NormalizeText(entry.Manufacturer),
            NormalizeText(entry.SeriesName),
            entry.Category,
            NormalizeText(entry.ModelCode) ?? string.Empty,
            NormalizeCode(entry.Code));

    private static string? TryCreateCandidateKeyForReport(EquipmentDiagnosticsStagingCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Manufacturer) ||
            string.IsNullOrWhiteSpace(candidate.Series) ||
            string.IsNullOrWhiteSpace(candidate.Category) ||
            string.IsNullOrWhiteSpace(candidate.Code) ||
            !Enum.TryParse<EquipmentCategory>(candidate.Category, ignoreCase: false, out var category))
        {
            return null;
        }

        return string.Join(
            "/",
            NormalizeText(candidate.Manufacturer),
            NormalizeText(candidate.Series),
            category,
            NormalizeText(candidate.ModelCode) ?? string.Empty,
            NormalizeCode(candidate.Code));
    }

    private static EquipmentDiagnosticsStagingValidationReport BuildReport(
        int candidateCount,
        IEnumerable<string> candidateKeys,
        IReadOnlyCollection<string?> reviewStatuses,
        IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> issues,
        IReadOnlyDictionary<string, string> candidateKeyByPath)
    {
        var sortedCandidateKeys = candidateKeys
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var issueGroups = issues
            .GroupBy(issue => ResolveIssueCandidateKey(issue, candidateKeyByPath), StringComparer.Ordinal)
            .Select(group => new EquipmentDiagnosticsStagingValidationIssueGroup(
                CandidateKey: group.Key,
                Issues: group
                    .OrderBy(issue => issue.Severity)
                    .ThenBy(issue => issue.Path, StringComparer.Ordinal)
                    .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(group => group.CandidateKey, StringComparer.Ordinal)
            .ToArray();

        var errorCount = issues.Count(issue => issue.Severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Error);
        var warningCount = issues.Count(issue => issue.Severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Warning);
        var infoCount = issues.Count(issue => issue.Severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Info);

        return new EquipmentDiagnosticsStagingValidationReport(
            TotalCandidates: candidateCount,
            ErrorCount: errorCount,
            WarningCount: warningCount,
            InfoCount: infoCount,
            CandidateKeys: sortedCandidateKeys,
            IssuesByCandidateKey: issueGroups,
            PromotionReady: errorCount == 0 &&
                issues.All(issue => issue.Code != "CandidateNotReadyForRuntimeCatalog") &&
                reviewStatuses.Count > 0 &&
                reviewStatuses.All(status => status == "ApprovedForCatalog") &&
                candidateCount > 0,
            HasBlockingIssues: errorCount > 0);
    }

    private static string ResolveIssueCandidateKey(
        EquipmentDiagnosticsStagingValidationIssue issue,
        IReadOnlyDictionary<string, string> candidateKeyByPath)
    {
        foreach (var pair in candidateKeyByPath.OrderByDescending(pair => pair.Key.Length))
        {
            if (issue.Path.StartsWith(pair.Key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return "__file__";
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedCharacters = value
            .Where(character => !char.IsWhiteSpace(character))
            .Select(char.ToUpperInvariant)
            .ToArray();

        return normalizedCharacters.Length == 0 ? null : new string(normalizedCharacters);
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

        return normalizedCharacters.Length == 0 ? null : new string(normalizedCharacters);
    }

    private static EquipmentDiagnosticsStagingValidationIssue Error(
        string code,
        string path,
        string message) =>
        new(code, path, message, EquipmentDiagnosticsStagingValidationIssueSeverity.Error);

    private static EquipmentDiagnosticsStagingValidationIssue Info(
        string code,
        string path,
        string message) =>
        new(code, path, message, EquipmentDiagnosticsStagingValidationIssueSeverity.Info);
}
