using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class EquipmentDiagnosticsVerificationService : IEquipmentDiagnosticsVerificationService
{
    private static readonly string[] UnsafeTextFragments =
    [
        "bypass",
        "disable protections",
        "disable protection",
        "force run",
        "short protection",
        "ignore protection"
    ];

    private static readonly string[] DiagnosticCaseExampleFields =
    [
        "source",
        "confidence",
        "shortSummary",
        "recommendedNextChecks",
        "confidenceExplanation",
        "sourceSummary",
        "applicabilitySummary",
        "safetyBoundary",
        "operatorNotes",
        "verificationRequired"
    ];

    private static readonly string[] OperatorGuidanceExampleFields =
    [
        "title",
        "summary",
        "verificationBanner",
        "sourceLine",
        "recommendedChecks",
        "safetyLine",
        "operatorNotes",
        "footer"
    ];

    private readonly IEquipmentDiagnosticsStagingValidator _stagingValidator;

    public EquipmentDiagnosticsVerificationService()
        : this(new EquipmentDiagnosticsStagingValidator())
    {
    }

    public EquipmentDiagnosticsVerificationService(IEquipmentDiagnosticsStagingValidator stagingValidator)
    {
        _stagingValidator = stagingValidator;
    }

    public EquipmentDiagnosticsVerificationReport Verify(EquipmentDiagnosticsVerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var runtimeIssues = ValidateRuntimeCatalog(input);
        var (stagingIssues, stagingExampleIssues, candidateSummaries) = ValidateStaging(input);
        var (manualCodeBookIssues, manualCodeBookSummary) = ValidateManualCodeBook(input);
        var coverage = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input);
        var coverageIssues = coverage.Conflicts.Select(conflict => new EquipmentDiagnosticsVerificationIssue(
            "ManualCodebookCoverageConflict",
            "codebook-coverage",
            conflict.Key,
            conflict.Message,
            conflict.Severity)).ToArray();
        var docsIssues = ValidateDocsExamples(input.DocsExampleDocuments);

        var sections = new[]
        {
            CreateSection("runtime-catalog", input.RuntimeDocuments.Count, runtimeIssues),
            CreateSection(
                "staging-candidates",
                input.StagingDocuments.Count(document =>
                    document.Kind == EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate),
                stagingIssues),
            CreateSection(
                "staging-examples",
                input.StagingDocuments.Count(document =>
                    document.Kind is EquipmentDiagnosticsVerificationDocumentKind.StagingExample
                        or EquipmentDiagnosticsVerificationDocumentKind.StagingTemplate),
                stagingExampleIssues),
            CreateSection("manual-codebook", input.ManualCodeBookDocuments?.Count ?? 0, manualCodeBookIssues),
            CreateSection("codebook-coverage", input.ManualCodeBookDocuments?.Count ?? 0, coverageIssues),
            CreateSection("docs-examples", input.DocsExampleDocuments.Count, docsIssues)
        };
        var duplicateKeys = GetDuplicateRuntimeKeys(input.RuntimeEntries);
        var runtimeSummary = new EquipmentDiagnosticsRuntimeCatalogSummary(
            TotalEntries: input.RuntimeEntries.Count,
            SeedEntries: input.RuntimeEntries.Count(entry =>
                entry.Source.SourceType == "SeededEngineeringKnowledge"),
            ManualVerifiedEntries: input.RuntimeEntries.Count(entry =>
                entry.Confidence == DiagnosticConfidence.ManualVerified),
            DuplicateKeys: duplicateKeys);
        var hasBlockingIssues = sections
            .Where(section => section.Name != "staging-examples")
            .Any(section => section.HasBlockingIssues);

        return new EquipmentDiagnosticsVerificationReport(
            RuntimeCatalog: runtimeSummary,
            ManualCodeBookSummary: manualCodeBookSummary,
            CodebookCoverage: coverage,
            StagingCandidateFileCount: sections.Single(section => section.Name == "staging-candidates").FileCount,
            StagingExampleFileCount: sections.Single(section => section.Name == "staging-examples").FileCount,
            DocsExampleFileCount: input.DocsExampleDocuments.Count,
            CandidateSummaries: candidateSummaries,
            Sections: sections,
            IsReleaseReady: !hasBlockingIssues,
            HasBlockingIssues: hasBlockingIssues);
    }

    private static (
        IReadOnlyList<EquipmentDiagnosticsVerificationIssue> Issues,
        EquipmentDiagnosticsManualCodeBookSummary Summary)
        ValidateManualCodeBook(EquipmentDiagnosticsVerificationInput input)
    {
        var issues = new List<EquipmentDiagnosticsVerificationIssue>();
        var occurrences = new List<(string SourceName, JsonElement Value)>();

        foreach (var document in input.ManualCodeBookDocuments ?? [])
        {
            if (document.Kind != EquipmentDiagnosticsVerificationDocumentKind.ManualCodeBook)
            {
                issues.Add(Error("InvalidManualCodeBookKind", document.SourceName,
                    "Manual codebook input must use ManualCodeBook kind.", "manual-codebook"));
                continue;
            }

            try
            {
                using var json = JsonDocument.Parse(document.Json);
                if (!json.RootElement.TryGetProperty("occurrences", out var values) ||
                    values.ValueKind != JsonValueKind.Array)
                {
                    issues.Add(Error("MissingCodeOccurrences", document.SourceName,
                        "Manual codebook file must contain an occurrences array.", "manual-codebook"));
                    continue;
                }

                foreach (var value in values.EnumerateArray())
                {
                    occurrences.Add((document.SourceName, value.Clone()));
                }

                issues.AddRange(ValidateJsonDocumentSafety(document, "manual-codebook", isNonRuntimeExample: false));
            }
            catch (JsonException exception)
            {
                issues.Add(Error("InvalidJson", document.SourceName,
                    $"Manual codebook JSON is invalid: {exception.Message}", "manual-codebook"));
            }
        }

        var allowedKinds = Enum.GetNames<Application.Knowledge.ManualCodeBook.EquipmentDiagnosticsManualCodeKind>();
        var allowedSides = Enum.GetNames<Application.Knowledge.ManualCodeBook.EquipmentDiagnosticsManualCodeEquipmentSide>();
        var allowedContexts = Enum.GetNames<Application.Knowledge.ManualCodeBook.EquipmentDiagnosticsManualCodeDisplayContext>();
        var allowedEvidence = Enum.GetNames<Application.Knowledge.ManualCodeBook.EquipmentDiagnosticsManualCodeEvidenceLevel>();
        var allowedReadiness = Enum.GetNames<Application.Knowledge.ManualCodeBook.EquipmentDiagnosticsManualCodePromotionReadiness>();
        var keys = new List<string>();

        foreach (var (sourceName, value) in occurrences)
        {
            var code = RequiredCodeBookText(value, "code", sourceName, issues);
            var manualId = RequiredCodeBookText(value, "manualId", sourceName, issues);
            var page = RequiredCodeBookText(value, "page", sourceName, issues);
            var section = RequiredCodeBookText(value, "section", sourceName, issues);
            var kind = RequiredCodeBookEnum(value, "codeKind", allowedKinds, sourceName, issues);
            var side = RequiredCodeBookEnum(value, "equipmentSide", allowedSides, sourceName, issues);
            var context = RequiredCodeBookEnum(value, "displayContext", allowedContexts, sourceName, issues);
            var evidence = RequiredCodeBookEnum(value, "evidenceLevel", allowedEvidence, sourceName, issues);
            var readiness = RequiredCodeBookEnum(value, "promotionReadiness", allowedReadiness, sourceName, issues);

            RequiredCodeBookText(value, "sourceFileName", sourceName, issues);
            RequiredCodeBookText(value, "sourceTitle", sourceName, issues);
            RequiredCodeBookText(value, "normalizedCode", sourceName, issues);
            RequiredCodeBookText(value, "series", sourceName, issues);
            RequiredCodeBookText(value, "meaning", sourceName, issues);

            if (!string.IsNullOrWhiteSpace(manualId) && input.KnownManualIds is not null &&
                !input.KnownManualIds.Contains(manualId))
            {
                issues.Add(Error("UnknownManualId", $"{sourceName}:{code}",
                    $"Manual codebook occurrence references unknown manualId '{manualId}'.", "manual-codebook"));
            }

            if (value.TryGetProperty("shortQuote", out var quote) && quote.ValueKind == JsonValueKind.String &&
                quote.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 25)
            {
                issues.Add(Error("LongManualQuote", $"{sourceName}:{code}",
                    "Manual codebook shortQuote must contain fewer than 25 words.", "manual-codebook"));
            }

            keys.Add($"{manualId}|{page}|{section}|{context}|{code}");
        }

        var duplicates = keys.GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        foreach (var duplicate in duplicates)
        {
            issues.Add(Error("DuplicateManualCodeOccurrence", duplicate,
                "Manual codebook contains a duplicate manual/context/page/code occurrence.", "manual-codebook"));
        }

        var countsByKind = CountCodeBookValues(occurrences, "codeKind");
        var countsBySide = CountCodeBookValues(occurrences, "equipmentSide");
        var countsByManual = CountCodeBookValues(occurrences, "manualId");
        var summary = new EquipmentDiagnosticsManualCodeBookSummary(
            SourceCount: countsByManual.Count,
            CodeOccurrenceCount: occurrences.Count,
            CountsByCodeKind: countsByKind,
            CountsByEquipmentSide: countsBySide,
            CountsByManualId: countsByManual,
            PromotableCandidatesCount: CountCodeBookValue(occurrences, "promotionReadiness", "ReadyForStagingCandidate"),
            ReferenceOnlyCount: CountCodeBookValue(occurrences, "promotionReadiness", "ReferenceOnly"),
            BlockedOrNeedsReviewCount: CountCodeBookValue(occurrences, "promotionReadiness", "Blocked") +
                CountCodeBookValue(occurrences, "promotionReadiness", "NeedsTroubleshootingEvidence") +
                CountCodeBookValue(occurrences, "promotionReadiness", "ReadyForEngineeringReview"),
            DuplicateOrConflictCount: duplicates.Length);

        return (SortIssues(issues), summary);
    }

    private static string RequiredCodeBookText(
        JsonElement value,
        string propertyName,
        string sourceName,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        if (!value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            issues.Add(Error("MissingManualCodeBookField", $"{sourceName}:{propertyName}",
                $"Manual codebook occurrence must include non-empty '{propertyName}'.", "manual-codebook"));
            return string.Empty;
        }

        return property.GetString()!;
    }

    private static string RequiredCodeBookEnum(
        JsonElement value,
        string propertyName,
        IReadOnlyCollection<string> allowed,
        string sourceName,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        var result = RequiredCodeBookText(value, propertyName, sourceName, issues);
        if (!string.IsNullOrEmpty(result) && !allowed.Contains(result, StringComparer.Ordinal))
        {
            issues.Add(Error("InvalidManualCodeBookEnum", $"{sourceName}:{propertyName}",
                $"Manual codebook value '{result}' is not allowed for '{propertyName}'.", "manual-codebook"));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, int> CountCodeBookValues(
        IEnumerable<(string SourceName, JsonElement Value)> occurrences,
        string propertyName) =>
        occurrences
            .Where(item => item.Value.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            .GroupBy(item => item.Value.GetProperty(propertyName).GetString()!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    private static int CountCodeBookValue(
        IEnumerable<(string SourceName, JsonElement Value)> occurrences,
        string propertyName,
        string expected) =>
        occurrences.Count(item => item.Value.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            string.Equals(value.GetString(), expected, StringComparison.Ordinal));

    private static IReadOnlyList<EquipmentDiagnosticsVerificationIssue> ValidateRuntimeCatalog(
        EquipmentDiagnosticsVerificationInput input)
    {
        var issues = new List<EquipmentDiagnosticsVerificationIssue>();

        if (input.RuntimeEntries.Count < input.MinimumRuntimeCatalogCount)
        {
            issues.Add(Error(
                "RuntimeCatalogBelowMinimum",
                "runtime-catalog",
                $"Runtime catalog contains {input.RuntimeEntries.Count} entries; expected at least {input.MinimumRuntimeCatalogCount}."));
        }

        foreach (var duplicateKey in GetDuplicateRuntimeKeys(input.RuntimeEntries))
        {
            issues.Add(Error(
                "DuplicateRuntimeKey",
                duplicateKey,
                "Runtime catalog contains a duplicate normalized manufacturer/series/category/modelCode/code key."));
        }

        foreach (var document in input.RuntimeDocuments)
        {
            if (document.Kind != EquipmentDiagnosticsVerificationDocumentKind.RuntimeCatalog ||
                IsNonRuntimePath(document.SourceName))
            {
                issues.Add(Error(
                    "RuntimePollution",
                    document.SourceName,
                    "Runtime catalog input must exclude staging, docs examples, and test fixtures."));
            }
        }

        foreach (var entry in input.RuntimeEntries)
        {
            ValidateRuntimeEntry(entry, issues);
        }

        return SortIssues(issues);
    }

    private static void ValidateRuntimeEntry(
        EquipmentDiagnosticsKnowledgeEntry entry,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        var key = CreateRuntimeKey(entry);

        RequireText(entry.Manufacturer, key, "manufacturer", issues);
        RequireText(entry.Code, key, "code", issues);
        RequireText(entry.Title, key, "title", issues);
        RequireText(entry.Meaning, key, "meaning", issues);
        RequireText(entry.Severity, key, "severity", issues);

        if (entry.Source is null)
        {
            issues.Add(Error("MissingSource", key, "Runtime entry must include a source block."));
            return;
        }

        if (entry.Source.Limitations.Count == 0)
        {
            issues.Add(Error("MissingLimitations", key, "Runtime entry source must include limitations."));
        }

        if (entry.RequiredMeasurements.Count == 0)
        {
            issues.Add(Error("MissingRequiredMeasurements", key, "Runtime entry must include a required measurement."));
        }

        if (entry.DiagnosticSteps.Count < 2)
        {
            issues.Add(Error("InsufficientDiagnosticSteps", key, "Runtime entry must include at least two diagnostic steps."));
        }

        if (entry.SafetyNotes.Count == 0)
        {
            issues.Add(Error("MissingSafetyNotes", key, "Runtime entry must include at least one safety note."));
        }

        if (entry.Source.SourceType == "SeededEngineeringKnowledge" &&
            (entry.Source.EvidenceLevel != "UnverifiedSeed" || entry.Confidence != DiagnosticConfidence.Low))
        {
            issues.Add(Error(
                "InvalidSeedProvenance",
                key,
                "SeededEngineeringKnowledge runtime entries must use UnverifiedSeed evidence and Low confidence."));
        }

        if (entry.Source.EvidenceLevel == "UnverifiedSeed" &&
            HasManualEvidence(entry.Source))
        {
            issues.Add(Error(
                "UnverifiedSeedHasManualEvidence",
                key,
                "UnverifiedSeed runtime entries must not contain manual title, document code, page, section, or quote evidence."));
        }

        if (entry.Confidence == DiagnosticConfidence.ManualVerified &&
            entry.Source.EvidenceLevel is not ("ManualPageVerified" or "CrossChecked"))
        {
            issues.Add(Error(
                "ManualVerifiedRequiresVerifiedEvidence",
                key,
                "ManualVerified runtime entries require ManualPageVerified or CrossChecked evidence."));
        }

        foreach (var fragment in UnsafeTextFragments.Where(fragment =>
                     EnumerateRuntimeEntryText(entry).Any(text =>
                         text.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
        {
            issues.Add(Error("UnsafeDiagnosticWording", key, $"Runtime entry contains unsafe wording fragment '{fragment}'."));
        }
    }

    private (
        IReadOnlyList<EquipmentDiagnosticsVerificationIssue> CandidateIssues,
        IReadOnlyList<EquipmentDiagnosticsVerificationIssue> ExampleIssues,
        IReadOnlyList<EquipmentDiagnosticsCandidateValidationSummary> Summaries)
        ValidateStaging(EquipmentDiagnosticsVerificationInput input)
    {
        var candidateIssues = new List<EquipmentDiagnosticsVerificationIssue>();
        var exampleIssues = new List<EquipmentDiagnosticsVerificationIssue>();
        var summaries = new List<EquipmentDiagnosticsCandidateValidationSummary>();

        foreach (var document in input.StagingDocuments.OrderBy(document => document.SourceName, StringComparer.Ordinal))
        {
            var result = _stagingValidator.ValidateJson(document.Json, input.RuntimeEntries, document.SourceName);
            var isExample = document.Kind is EquipmentDiagnosticsVerificationDocumentKind.StagingExample
                or EquipmentDiagnosticsVerificationDocumentKind.StagingTemplate;
            var targetIssues = isExample ? exampleIssues : candidateIssues;

            targetIssues.AddRange(result.Issues.Select(issue => new EquipmentDiagnosticsVerificationIssue(
                Code: issue.Code,
                Section: isExample ? "staging-examples" : "staging-candidates",
                Path: issue.Path,
                Message: issue.Message,
                Severity: isExample
                    ? MapExampleSeverity(issue.Severity)
                    : MapSeverity(issue.Severity))));
            targetIssues.AddRange(ValidateJsonDocumentSafety(document, isExample ? "staging-examples" : "staging-candidates", isExample));
            if (!isExample && document.Json.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                targetIssues.Add(new EquipmentDiagnosticsVerificationIssue(
                    "PlaceholderEvidenceInCandidate",
                    "staging-candidates",
                    document.SourceName,
                    "Placeholder evidence is allowed only in non-runtime examples/templates and must be replaced before intake validation.",
                    EquipmentDiagnosticsVerificationSeverity.Error));
            }

            if (!isExample && input.KnownManualIds is not null)
            {
                foreach (var manualId in ReadCandidateManualIds(document.Json).Where(manualId =>
                             !input.KnownManualIds.Contains(manualId)))
                {
                    targetIssues.Add(new EquipmentDiagnosticsVerificationIssue(
                        "UnknownManualId",
                        "staging-candidates",
                        document.SourceName,
                        $"Staging candidate references unknown manualId '{manualId}'.",
                        EquipmentDiagnosticsVerificationSeverity.Error));
                }
            }

            summaries.Add(BuildCandidateSummary(document, result));
        }

        return (SortIssues(candidateIssues), SortIssues(exampleIssues), summaries
            .OrderBy(summary => summary.SourceName, StringComparer.Ordinal)
            .ToArray());
    }

    private static EquipmentDiagnosticsCandidateValidationSummary BuildCandidateSummary(
        EquipmentDiagnosticsVerificationDocument document,
        EquipmentDiagnosticsStagingValidationResult result)
    {
        var statuses = ReadCandidateStatuses(document.Json);
        var readiness = result.Errors.Count > 0
            ? EquipmentDiagnosticsPromotionReadiness.Blocked
            : result.Report?.PromotionReady == true
                ? EquipmentDiagnosticsPromotionReadiness.ReadyForCatalogPromotion
                : statuses.Contains("ReadyForReview", StringComparer.Ordinal)
                    ? EquipmentDiagnosticsPromotionReadiness.ReadyForEngineeringReview
                    : EquipmentDiagnosticsPromotionReadiness.NotReady;
        var actions = BuildSuggestedActions(document, result, statuses);

        return new EquipmentDiagnosticsCandidateValidationSummary(
            SourceName: document.SourceName,
            CandidateKeys: result.Report?.CandidateKeys ?? [],
            Readiness: readiness,
            SuggestedNextActions: actions,
            ErrorCount: result.Report?.ErrorCount ?? result.Errors.Count,
            WarningCount: result.Report?.WarningCount ?? 0,
            InfoCount: result.Report?.InfoCount ?? 0);
    }

    private static IReadOnlyList<string> BuildSuggestedActions(
        EquipmentDiagnosticsVerificationDocument document,
        EquipmentDiagnosticsStagingValidationResult result,
        IReadOnlyList<string> statuses)
    {
        var actions = new List<string>();

        if (result.Errors.Any(issue => issue.Code.Contains("Manual", StringComparison.Ordinal)))
        {
            actions.Add("Fill exact manual title/page evidence or lower the proposed confidence and evidence level.");
        }

        if (result.Errors.Any(issue => issue.Code.Contains("Unsafe", StringComparison.Ordinal)))
        {
            actions.Add("Remove unsafe wording before engineering review.");
        }

        if (document.Json.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("Replace placeholder evidence before catalog promotion.");
        }

        if (statuses.Contains("ReadyForReview", StringComparer.Ordinal))
        {
            actions.Add("Complete engineering review and verify applicable models.");
        }

        if (result.Report?.PromotionReady == true)
        {
            actions.Add("Move to production catalog only in a reviewed PR after final validation.");
        }

        return actions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<EquipmentDiagnosticsVerificationIssue> ValidateDocsExamples(
        IReadOnlyList<EquipmentDiagnosticsVerificationDocument> documents)
    {
        var issues = new List<EquipmentDiagnosticsVerificationIssue>();

        foreach (var document in documents.OrderBy(document => document.SourceName, StringComparer.Ordinal))
        {
            if (document.Kind != EquipmentDiagnosticsVerificationDocumentKind.DocsExample)
            {
                issues.Add(Error(
                    "InvalidDocsExampleKind",
                    document.SourceName,
                    "Docs example input must use DocsExample kind.",
                    "docs-examples"));
            }

            try
            {
                using var json = JsonDocument.Parse(document.Json);
                ValidateDocsExampleShape(document.SourceName, json.RootElement, issues);
                issues.AddRange(ValidateJsonDocumentSafety(document, "docs-examples", isNonRuntimeExample: true));
            }
            catch (JsonException exception)
            {
                issues.Add(Error(
                    "InvalidJson",
                    document.SourceName,
                    $"Docs example JSON is invalid: {exception.Message}",
                    "docs-examples"));
            }
        }

        return SortIssues(issues);
    }

    private static void ValidateDocsExampleShape(
        string sourceName,
        JsonElement root,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        if (sourceName.EndsWith("diagnostic-case-response.example.json", StringComparison.OrdinalIgnoreCase))
        {
            RequireProperties(root, sourceName, DiagnosticCaseExampleFields, issues);
            if (root.TryGetProperty("source", out var source))
            {
                RequireProperties(source, $"{sourceName}:source", ["sourceType", "evidenceLevel"], issues);
                ValidateNoInventedDocsManualEvidence(sourceName, source, issues);
            }
        }
        else if (sourceName.EndsWith("operator-guidance-message.example.json", StringComparison.OrdinalIgnoreCase))
        {
            RequireProperties(root, sourceName, OperatorGuidanceExampleFields, issues);
        }
    }

    private static void ValidateNoInventedDocsManualEvidence(
        string sourceName,
        JsonElement source,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        foreach (var propertyName in new[] { "manualTitle", "manualDocumentCode", "page", "section", "quote" })
        {
            if (source.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null)
            {
                issues.Add(Error(
                    "DocsExampleInventedManualEvidence",
                    $"{sourceName}:source.{propertyName}",
                    "Seed contract examples must not invent manual evidence.",
                    "docs-examples"));
            }
        }
    }

    private static IReadOnlyList<EquipmentDiagnosticsVerificationIssue> ValidateJsonDocumentSafety(
        EquipmentDiagnosticsVerificationDocument document,
        string section,
        bool isNonRuntimeExample)
    {
        var issues = new List<EquipmentDiagnosticsVerificationIssue>();

        foreach (var fragment in UnsafeTextFragments.Where(fragment =>
                     document.Json.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new EquipmentDiagnosticsVerificationIssue(
                "UnsafeDiagnosticWording",
                section,
                document.SourceName,
                $"JSON document contains unsafe wording fragment '{fragment}'.",
                isNonRuntimeExample ? EquipmentDiagnosticsVerificationSeverity.Warning : EquipmentDiagnosticsVerificationSeverity.Error));
        }

        try
        {
            using var json = JsonDocument.Parse(document.Json);
            foreach (var quote in EnumerateNamedStringValues(json.RootElement, "quote").Where(quote => quote.Length > 240))
            {
                issues.Add(new EquipmentDiagnosticsVerificationIssue(
                    "LongManualQuote",
                    section,
                    document.SourceName,
                    "Manual quote-like text exceeds the 240 character review limit.",
                    isNonRuntimeExample ? EquipmentDiagnosticsVerificationSeverity.Warning : EquipmentDiagnosticsVerificationSeverity.Error));
            }
        }
        catch (JsonException)
        {
            // The staging validator or docs JSON validator reports the parse error.
        }

        return issues;
    }

    private static void RequireProperties(
        JsonElement element,
        string path,
        IReadOnlyCollection<string> propertyNames,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out _))
            {
                issues.Add(Error(
                    "MissingContractField",
                    $"{path}:{propertyName}",
                    $"Contract example must include '{propertyName}'.",
                    "docs-examples"));
            }
        }
    }

    private static void RequireText(
        string? value,
        string path,
        string propertyName,
        ICollection<EquipmentDiagnosticsVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error("MissingRuntimeField", $"{path}:{propertyName}", $"Runtime entry must include non-empty {propertyName}."));
        }
    }

    private static EquipmentDiagnosticsVerificationSection CreateSection(
        string name,
        int fileCount,
        IReadOnlyList<EquipmentDiagnosticsVerificationIssue> issues) =>
        new(name, fileCount, issues);

    private static EquipmentDiagnosticsVerificationIssue Error(
        string code,
        string path,
        string message,
        string section = "runtime-catalog") =>
        new(code, section, path, message, EquipmentDiagnosticsVerificationSeverity.Error);

    private static EquipmentDiagnosticsVerificationSeverity MapSeverity(
        EquipmentDiagnosticsStagingValidationIssueSeverity severity) =>
        severity switch
        {
            EquipmentDiagnosticsStagingValidationIssueSeverity.Error => EquipmentDiagnosticsVerificationSeverity.Error,
            EquipmentDiagnosticsStagingValidationIssueSeverity.Warning => EquipmentDiagnosticsVerificationSeverity.Warning,
            _ => EquipmentDiagnosticsVerificationSeverity.Info
        };

    private static EquipmentDiagnosticsVerificationSeverity MapExampleSeverity(
        EquipmentDiagnosticsStagingValidationIssueSeverity severity) =>
        severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Error
            ? EquipmentDiagnosticsVerificationSeverity.Warning
            : MapSeverity(severity);

    private static IReadOnlyList<EquipmentDiagnosticsVerificationIssue> SortIssues(
        IEnumerable<EquipmentDiagnosticsVerificationIssue> issues) =>
        issues
            .OrderBy(issue => issue.Severity)
            .ThenBy(issue => issue.Section, StringComparer.Ordinal)
            .ThenBy(issue => issue.Path, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetDuplicateRuntimeKeys(
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries) =>
        entries
            .GroupBy(CreateRuntimeKey, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

    private static string CreateRuntimeKey(EquipmentDiagnosticsKnowledgeEntry entry) =>
        string.Join(
            "/",
            Normalize(entry.Manufacturer),
            Normalize(entry.SeriesName),
            entry.Category,
            Normalize(entry.ModelCode),
            Normalize(entry.Code));

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value
                .Where(character => !char.IsWhiteSpace(character) && character != '-')
                .Select(char.ToUpperInvariant)
                .ToArray());

    private static bool IsNonRuntimePath(string sourceName)
    {
        var normalized = sourceName.Replace('\\', '/');
        return normalized.Contains("/staging/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/manual-codebook/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/docs/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/fixtures/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasManualEvidence(EquipmentDiagnosticsKnowledgeSourceInfo source) =>
        source.ManualTitle is not null ||
        source.ManualDocumentCode is not null ||
        source.Page is not null ||
        source.Section is not null ||
        source.Quote is not null;

    private static IEnumerable<string> EnumerateRuntimeEntryText(EquipmentDiagnosticsKnowledgeEntry entry) =>
        entry.LikelyCauses
            .Concat(entry.SafetyNotes)
            .Concat(entry.DiagnosticSteps.SelectMany(step => new[]
            {
                step.Title,
                step.Instruction,
                step.ExpectedResult,
                step.IfFailedAction
            }))
            .Concat(entry.RequiredMeasurements.SelectMany(measurement => new[]
            {
                measurement.Name,
                measurement.Unit,
                measurement.Description
            }));

    private static IReadOnlyList<string> ReadCandidateStatuses(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return candidates
                .EnumerateArray()
                .Select(candidate =>
                    candidate.TryGetProperty("reviewStatus", out var status)
                        ? status.GetString() ?? string.Empty
                        : string.Empty)
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ReadCandidateManualIds(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return candidates
                .EnumerateArray()
                .Where(candidate => candidate.TryGetProperty("source", out _))
                .Select(candidate => candidate.GetProperty("source"))
                .Where(source => source.TryGetProperty("manualId", out var manualId) &&
                    manualId.ValueKind == JsonValueKind.String)
                .Select(source => source.GetProperty("manualId").GetString() ?? string.Empty)
                .Where(manualId => !string.IsNullOrWhiteSpace(manualId))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(manualId => manualId, StringComparer.Ordinal)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumerateNamedStringValues(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                {
                    yield return property.Value.GetString() ?? string.Empty;
                }

                foreach (var value in EnumerateNamedStringValues(property.Value, propertyName))
                {
                    yield return value;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in EnumerateNamedStringValues(item, propertyName))
                {
                    yield return value;
                }
            }
        }
    }
}
