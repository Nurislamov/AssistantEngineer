using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class EquipmentDiagnosticsCodebookCoverageAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly HashSet<string> NonDiagnosticKinds =
        ["Status", "Debugging", "Query", "Setting", "DisplayPattern", "NonFault", "Parameter", "ToolFunction", "Unknown"];

    public EquipmentDiagnosticsCodebookCoverageReport Analyze(EquipmentDiagnosticsVerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var occurrences = (input.ManualCodeBookDocuments ?? [])
            .SelectMany(ReadOccurrences)
            .OrderBy(value => CreateKey(value.Series, value.EquipmentSide, value.DisplayContext, value.NormalizedCode), StringComparer.Ordinal)
            .ToArray();
        var staging = input.StagingDocuments
            .Where(document => document.Kind == EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)
            .SelectMany(ReadStagingKeys)
            .ToHashSet(StringComparer.Ordinal);
        var runtime = input.RuntimeEntries.Select(CreateRuntimeKey).ToHashSet(StringComparer.Ordinal);
        var conflicts = DetectConflicts(occurrences, runtime, staging);
        var conflictKeys = conflicts.ToLookup(conflict => conflict.Key, StringComparer.Ordinal);

        var entries = occurrences
            .GroupBy(value => CreateKey(value.Series, value.EquipmentSide, value.DisplayContext, value.NormalizedCode), StringComparer.Ordinal)
            .Select(group => BuildEntry(group.Key, group.ToArray(), runtime, staging, conflictKeys[group.Key]))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();
        var top = entries
            .Where(entry => entry.Readiness is not (EquipmentDiagnosticsStagingReadinessRecommendation.ReferenceOnly or
                EquipmentDiagnosticsStagingReadinessRecommendation.NotApplicable))
            .OrderBy(entry => entry.Priority)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Take(5)
            .ToArray();
        var summary = new EquipmentDiagnosticsCodebookCoverageSummary(
            Status: conflicts.Any(conflict => conflict.Severity == EquipmentDiagnosticsVerificationSeverity.Error) ? "Fail" : "Pass",
            TotalRuntimeCodes: input.RuntimeEntries.Count,
            TotalStagingCandidates: staging.Count,
            TotalCodebookOccurrences: occurrences.Length,
            UniqueNormalizedCodeCount: occurrences.Select(value => value.NormalizedCode).Distinct(StringComparer.Ordinal).Count(),
            CoverageByStatus: Count(entries.Select(entry => entry.Status.ToString())),
            CoverageByCodeKind: Count(entries.Select(entry => entry.CodeKind)),
            CoverageByEquipmentSide: Count(entries.Select(entry => entry.EquipmentSide)),
            ReadyForStagingCandidateCount: entries.Count(entry => entry.Status == EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate),
            ReferenceOnlyCount: entries.Count(entry => entry.Readiness == EquipmentDiagnosticsStagingReadinessRecommendation.ReferenceOnly),
            ConflictCount: conflicts.Length,
            TopRecommendationsCount: top.Length);

        return new EquipmentDiagnosticsCodebookCoverageReport(
            summary,
            entries,
            conflicts,
            top,
            BuildNextActions(entries, conflicts));
    }

    private static EquipmentDiagnosticsCodeCoverageEntry BuildEntry(
        string key,
        IReadOnlyList<CodeOccurrence> occurrences,
        IReadOnlySet<string> runtime,
        IReadOnlySet<string> staging,
        IEnumerable<EquipmentDiagnosticsManualCodeConflict> conflicts)
    {
        var first = occurrences[0];
        var runtimeKey = CreateComparableKey(first.Series, first.EquipmentSide, first.NormalizedCode);
        var hasConflict = conflicts.Any();
        var status = hasConflict
            ? EquipmentDiagnosticsCodeCoverageStatus.ConflictingManualMeaning
            : runtime.Contains(runtimeKey)
                ? EquipmentDiagnosticsCodeCoverageStatus.RuntimeCovered
                : staging.Contains(runtimeKey)
                    ? EquipmentDiagnosticsCodeCoverageStatus.StagingCovered
                    : ClassifyCodebookOnly(first);
        var readiness = status switch
        {
            EquipmentDiagnosticsCodeCoverageStatus.RuntimeCovered => EquipmentDiagnosticsStagingReadinessRecommendation.ReadyForCatalogPromotionLater,
            EquipmentDiagnosticsCodeCoverageStatus.StagingCovered => EquipmentDiagnosticsStagingReadinessRecommendation.NeedsEngineeringReview,
            EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate => EquipmentDiagnosticsStagingReadinessRecommendation.ReadyForStagingCandidate,
            EquipmentDiagnosticsCodeCoverageStatus.NeedsTroubleshootingSection or EquipmentDiagnosticsCodeCoverageStatus.NeedsManualEvidence =>
                EquipmentDiagnosticsStagingReadinessRecommendation.NeedsEvidence,
            EquipmentDiagnosticsCodeCoverageStatus.ConflictingManualMeaning or EquipmentDiagnosticsCodeCoverageStatus.ConflictingCodeKind or
                EquipmentDiagnosticsCodeCoverageStatus.Blocked => EquipmentDiagnosticsStagingReadinessRecommendation.Blocked,
            _ => EquipmentDiagnosticsStagingReadinessRecommendation.ReferenceOnly
        };

        return new EquipmentDiagnosticsCodeCoverageEntry(
            key, "Gree", first.Series, first.EquipmentSide, first.DisplayContext, first.Code, first.NormalizedCode,
            string.Join("|", occurrences.Select(value => value.CodeKind).Distinct(StringComparer.Ordinal).Order()),
            status, readiness, GetPriority(first, status),
            occurrences.Select(value => value.ManualId).Distinct(StringComparer.Ordinal).Order().ToArray(),
            occurrences.Select(value => value.Page).Distinct(StringComparer.Ordinal).Order().ToArray(),
            occurrences.Select(value => value.Section).Distinct(StringComparer.Ordinal).Order().ToArray(),
            occurrences.Select(value => value.Meaning).Distinct(StringComparer.Ordinal).Order().ToArray(),
            GetNextAction(status));
    }

    private static EquipmentDiagnosticsCodeCoverageStatus ClassifyCodebookOnly(CodeOccurrence value)
    {
        if (value.CodeKind == "Status") return EquipmentDiagnosticsCodeCoverageStatus.StatusOnly;
        if (value.CodeKind is "Debugging" or "DisplayPattern") return EquipmentDiagnosticsCodeCoverageStatus.DebugOnly;
        if (value.CodeKind is "Query" or "Setting" or "Parameter") return EquipmentDiagnosticsCodeCoverageStatus.QueryOrSettingOnly;
        if (NonDiagnosticKinds.Contains(value.CodeKind)) return EquipmentDiagnosticsCodeCoverageStatus.ReferenceOnly;
        if (value.EvidenceLevel == "TroubleshootingSection" && value.CanBecomeDiagnosticCase)
            return EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate;
        if (value.EvidenceLevel == "ErrorIndicationTable" && value.CanBecomeDiagnosticCase)
            return EquipmentDiagnosticsCodeCoverageStatus.NeedsTroubleshootingSection;
        return EquipmentDiagnosticsCodeCoverageStatus.NeedsManualEvidence;
    }

    private static EquipmentDiagnosticsCoveragePriority GetPriority(CodeOccurrence value, EquipmentDiagnosticsCodeCoverageStatus status)
    {
        if (status is EquipmentDiagnosticsCodeCoverageStatus.StatusOnly or EquipmentDiagnosticsCodeCoverageStatus.DebugOnly or
            EquipmentDiagnosticsCodeCoverageStatus.QueryOrSettingOnly or EquipmentDiagnosticsCodeCoverageStatus.ReferenceOnly)
            return EquipmentDiagnosticsCoveragePriority.Reference;
        if (value.NormalizedCode.Length > 0 && "EFHJPUC".Contains(value.NormalizedCode[0]) &&
            value.CodeKind is "Fault" or "Protection")
            return EquipmentDiagnosticsCoveragePriority.HighOperatorDiagnostic;
        return EquipmentDiagnosticsCoveragePriority.NormalDiagnostic;
    }

    private static EquipmentDiagnosticsManualCodeConflict[] DetectConflicts(
        IReadOnlyList<CodeOccurrence> occurrences,
        IReadOnlySet<string> runtime,
        IReadOnlySet<string> staging) =>
        occurrences.GroupBy(value => CreateKey(value.Series, value.EquipmentSide, value.DisplayContext, value.NormalizedCode), StringComparer.Ordinal)
            .SelectMany(group =>
            {
                var meanings = group.Select(value => value.Meaning).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var kinds = group.Select(value => value.CodeKind).Distinct(StringComparer.Ordinal).ToArray();
                if (meanings.Length <= 1 && kinds.Length <= 1)
                    return Array.Empty<EquipmentDiagnosticsManualCodeConflict>();
                var first = group.First();
                var severity = runtime.Contains(CreateComparableKey(first.Series, first.EquipmentSide, first.NormalizedCode)) ||
                    staging.Contains(CreateComparableKey(first.Series, first.EquipmentSide, first.NormalizedCode))
                    ? EquipmentDiagnosticsVerificationSeverity.Error
                    : EquipmentDiagnosticsVerificationSeverity.Warning;
                return new[] { new EquipmentDiagnosticsManualCodeConflict(group.Key, first.Code,
                    meanings.Length > 1 ? "Same-context manual occurrences have different meanings." : "Same-context manual occurrences have different code kinds.",
                    severity) };
            })
            .OrderBy(conflict => conflict.Key, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<CodeOccurrence> ReadOccurrences(EquipmentDiagnosticsVerificationDocument document)
    {
        using var json = JsonDocument.Parse(document.Json);
        return json.RootElement.GetProperty("occurrences").EnumerateArray().Select(value => new CodeOccurrence(
            Text(value, "manualId"), Text(value, "series"), Text(value, "equipmentSide"), Text(value, "displayContext"),
            Text(value, "code"), Text(value, "normalizedCode"), Text(value, "codeKind"), Text(value, "meaning"),
            Text(value, "page"), Text(value, "section"), Text(value, "evidenceLevel"),
            value.GetProperty("canBecomeDiagnosticCase").GetBoolean())).ToArray();
    }

    private static IReadOnlyList<string> ReadStagingKeys(EquipmentDiagnosticsVerificationDocument document)
    {
        using var json = JsonDocument.Parse(document.Json);
        return json.RootElement.GetProperty("candidates").EnumerateArray()
            .Select(value => CreateComparableKey(Text(value, "series"), SideFromCategory(Text(value, "category")), Normalize(Text(value, "code"))))
            .ToArray();
    }

    private static string CreateRuntimeKey(EquipmentDiagnosticsKnowledgeEntry entry) =>
        CreateComparableKey(entry.SeriesName ?? string.Empty, SideFromCategory(entry.Category.ToString()), Normalize(entry.Code));
    private static string CreateComparableKey(string series, string side, string normalizedCode) =>
        $"{Normalize(series)}|{Normalize(side)}|{normalizedCode}";
    private static string CreateKey(string series, string side, string context, string normalizedCode) =>
        $"{CreateComparableKey(series, side, normalizedCode)}|{Normalize(context)}";
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string Text(JsonElement value, string property) => value.GetProperty(property).GetString() ?? string.Empty;
    private static string SideFromCategory(string category) => category switch
    {
        "VrfIndoorUnit" => "Indoor",
        "VrfOutdoorUnit" => "Outdoor",
        "Controller" => "Controller",
        _ => "System"
    };
    private static IReadOnlyDictionary<string, int> Count(IEnumerable<string> values) =>
        values.GroupBy(value => value, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    private static string GetNextAction(EquipmentDiagnosticsCodeCoverageStatus status) => status switch
    {
        EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate => "Create a reviewed draft staging candidate.",
        EquipmentDiagnosticsCodeCoverageStatus.NeedsTroubleshootingSection => "Locate and review a service-manual troubleshooting section.",
        EquipmentDiagnosticsCodeCoverageStatus.NeedsManualEvidence => "Add exact manual evidence before staging review.",
        EquipmentDiagnosticsCodeCoverageStatus.StagingCovered => "Complete engineering review of the existing staging candidate.",
        EquipmentDiagnosticsCodeCoverageStatus.RuntimeCovered => "Keep runtime coverage under normal review.",
        EquipmentDiagnosticsCodeCoverageStatus.ConflictingManualMeaning or EquipmentDiagnosticsCodeCoverageStatus.ConflictingCodeKind =>
            "Resolve the same-context conflict before staging.",
        _ => "Keep as non-runtime reference."
    };
    private static IReadOnlyList<string> BuildNextActions(
        IReadOnlyList<EquipmentDiagnosticsCodeCoverageEntry> entries,
        IReadOnlyList<EquipmentDiagnosticsManualCodeConflict> conflicts)
    {
        var actions = new List<string>();
        if (conflicts.Count > 0) actions.Add("Resolve codebook conflicts before creating staging candidates.");
        if (entries.Any(entry => entry.Status == EquipmentDiagnosticsCodeCoverageStatus.NeedsTroubleshootingSection))
            actions.Add("Review service-manual troubleshooting sections for high-priority error-table occurrences.");
        if (entries.Any(entry => entry.Status == EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate))
            actions.Add("Create reviewed draft staging candidates for ready occurrences.");
        actions.Add("Keep status, debugging, query, setting, controller, and tool occurrences reference-only.");
        return actions.Distinct(StringComparer.Ordinal).ToArray();
    }

    private sealed record CodeOccurrence(
        string ManualId, string Series, string EquipmentSide, string DisplayContext, string Code, string NormalizedCode,
        string CodeKind, string Meaning, string Page, string Section, string EvidenceLevel, bool CanBecomeDiagnosticCase);
}
