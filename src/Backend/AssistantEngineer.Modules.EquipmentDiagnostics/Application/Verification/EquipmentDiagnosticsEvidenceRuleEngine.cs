using System.Text.Json;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class EquipmentDiagnosticsEvidenceRuleEngine
{
    private static readonly HashSet<string> PrimaryUsages =
        ["PrimaryTroubleshootingSource", "IndoorTroubleshootingSource", "OutdoorTroubleshootingSource"];
    private static readonly HashSet<string> ReferenceKinds =
        ["Status", "Debugging", "Query", "Setting", "DisplayPattern", "NonFault", "Parameter", "ToolFunction", "Unknown"];
    private static readonly string[] UnsafeFragments =
        ["bypass", "disable protection", "disable protections", "force run", "short protection", "ignore protection"];

    public EquipmentDiagnosticsEvidenceAssessmentReport Assess(
        EquipmentDiagnosticsVerificationInput input,
        EquipmentDiagnosticsCodebookCoverageReport coverage)
    {
        var conflictKeys = coverage.Conflicts.Select(conflict => conflict.Key).ToHashSet(StringComparer.Ordinal);
        var coverageByKey = coverage.Entries.ToDictionary(entry => entry.Key, StringComparer.Ordinal);
        var assessments = (input.ManualCodeBookDocuments ?? [])
            .SelectMany(ReadOccurrences)
            .Select(occurrence => AssessOccurrence(occurrence, input.ManualSourceUsages ?? new Dictionary<string, string>(), coverageByKey, conflictKeys))
            .OrderBy(assessment => assessment.Key, StringComparer.Ordinal)
            .ThenBy(assessment => assessment.ManualId, StringComparer.Ordinal)
            .ToArray();
        var summary = new EquipmentDiagnosticsEvidenceAssessmentSummary(
            assessments.Length,
            Count(assessments.Select(value => value.Status.ToString())),
            Count(assessments.Select(value => value.ConfidenceBucket.ToString())),
            assessments.Count(value => value.Status == EquipmentDiagnosticsEvidenceAssessmentStatus.ReadyForStagingCandidate),
            assessments.Count(value => value.Status == EquipmentDiagnosticsEvidenceAssessmentStatus.NeedsTroubleshootingSection),
            assessments.Count(value => value.Status == EquipmentDiagnosticsEvidenceAssessmentStatus.ReferenceOnly),
            assessments.Count(value => value.Status is EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByConflict or
                EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByUnsafeText or
                EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByMissingManualEvidence));
        return new EquipmentDiagnosticsEvidenceAssessmentReport(summary, assessments);
    }

    private static EquipmentDiagnosticsEvidenceAssessment AssessOccurrence(
        Occurrence occurrence,
        IReadOnlyDictionary<string, string> manualUsages,
        IReadOnlyDictionary<string, EquipmentDiagnosticsCodeCoverageEntry> coverage,
        IReadOnlySet<string> conflictKeys)
    {
        var key = CreateKey(occurrence.Series, occurrence.EquipmentSide, occurrence.DisplayContext, occurrence.NormalizedCode);
        var reasons = new List<string>();
        var status = EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByMissingManualEvidence;
        var bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.InsufficientEvidence;
        var coverageStatus = coverage.TryGetValue(key, out var entry) ? entry.Status : EquipmentDiagnosticsCodeCoverageStatus.CodebookOnly;
        var usage = manualUsages.TryGetValue(occurrence.ManualId, out var knownUsage) ? knownUsage : string.Empty;

        if (conflictKeys.Contains(key))
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByConflict;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.ConflictedEvidence;
            reasons.Add("SameContextConflict");
        }
        else if (coverageStatus == EquipmentDiagnosticsCodeCoverageStatus.RuntimeCovered)
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyRuntimeCovered;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.StrongManualEvidence;
            reasons.Add("AlreadyRuntimeCovered");
        }
        else if (coverageStatus == EquipmentDiagnosticsCodeCoverageStatus.StagingCovered)
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyStagingCovered;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.StrongManualEvidence;
            reasons.Add("AlreadyStagingCovered");
        }
        else if (ContainsUnsafeText(occurrence))
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByUnsafeText;
            reasons.Add("UnsafeText");
        }
        else if (ReferenceKinds.Contains(occurrence.CodeKind) || occurrence.EquipmentSide is "Controller" or "CommissioningTool" or "TechnicalGuide")
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.ReferenceOnly;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.ReferenceEvidenceOnly;
            reasons.Add("ReferenceClassification");
        }
        else if (occurrence.CodeKind is not ("Fault" or "Protection") ||
                 occurrence.EquipmentSide is not ("Indoor" or "Outdoor" or "System"))
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.NotApplicable;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.ReferenceEvidenceOnly;
            reasons.Add("NotDiagnosticCandidate");
        }
        else if (occurrence.EvidenceLevel != "TroubleshootingSection")
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.NeedsTroubleshootingSection;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.PartialManualEvidence;
            reasons.Add("TroubleshootingSectionRequired");
        }
        else if (!PrimaryUsages.Contains(usage))
        {
            reasons.Add(string.IsNullOrWhiteSpace(usage) ? "UnknownManualUsage" : "PrimaryTroubleshootingSourceRequired");
        }
        else if (string.IsNullOrWhiteSpace(occurrence.Page) || string.IsNullOrWhiteSpace(occurrence.Section) ||
                 string.IsNullOrWhiteSpace(occurrence.Meaning))
        {
            reasons.Add("MissingManualAnchorOrMeaning");
        }
        else if (occurrence.SafetyNotes.Count == 0 && occurrence.Limitations.Count == 0)
        {
            reasons.Add("SafetyNotesOrLimitationsRequired");
        }
        else
        {
            status = EquipmentDiagnosticsEvidenceAssessmentStatus.ReadyForStagingCandidate;
            bucket = EquipmentDiagnosticsEvidenceConfidenceBucket.StrongManualEvidence;
            reasons.Add("PrimaryTroubleshootingEvidenceComplete");
        }

        return new EquipmentDiagnosticsEvidenceAssessment(
            key, occurrence.Code, occurrence.NormalizedCode, occurrence.CodeKind, occurrence.EquipmentSide,
            occurrence.DisplayContext, occurrence.Series, occurrence.Meaning, occurrence.ManualId, occurrence.ManualTitle,
            occurrence.Page, occurrence.Section, occurrence.ShortQuote, occurrence.RequiredMeasurements,
            occurrence.SafetyNotes, occurrence.Limitations, status, bucket, reasons, NextAction(status));
    }

    private static IReadOnlyList<Occurrence> ReadOccurrences(EquipmentDiagnosticsVerificationDocument document)
    {
        using var json = JsonDocument.Parse(document.Json);
        return json.RootElement.GetProperty("occurrences").EnumerateArray().Select(value => new Occurrence(
            Text(value, "manualId"), Text(value, "sourceTitle"), Text(value, "series"), Text(value, "equipmentSide"),
            Text(value, "displayContext"), Text(value, "code"), Text(value, "normalizedCode"), Text(value, "codeKind"),
            Text(value, "meaning"), Text(value, "page"), Text(value, "section"), Text(value, "evidenceLevel"),
            OptionalText(value, "shortQuote"), TextArray(value, "requiredMeasurements"), TextArray(value, "safetyNotes"),
            TextArray(value, "limitations"))).ToArray();
    }

    private static bool ContainsUnsafeText(Occurrence value) =>
        value.SafetyNotes.Concat(value.Limitations).Append(value.Meaning)
            .Any(text => UnsafeFragments.Any(fragment => text.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    private static string NextAction(EquipmentDiagnosticsEvidenceAssessmentStatus status) => status switch
    {
        EquipmentDiagnosticsEvidenceAssessmentStatus.ReadyForStagingCandidate => "Generate a draft preview for engineering review.",
        EquipmentDiagnosticsEvidenceAssessmentStatus.NeedsTroubleshootingSection => "Add an exact service-manual troubleshooting occurrence.",
        EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyRuntimeCovered => "No preview needed; runtime coverage already exists.",
        EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyStagingCovered => "Continue review of the existing production staging candidate.",
        EquipmentDiagnosticsEvidenceAssessmentStatus.ReferenceOnly => "Keep as non-runtime reference evidence.",
        EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByConflict => "Resolve the same-context conflict before preview generation.",
        _ => "Complete missing evidence before preview generation."
    };
    private static string CreateKey(string series, string side, string context, string code) =>
        $"{Normalize(series)}|{Normalize(side)}|{Normalize(code)}|{Normalize(context)}";
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string Text(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : string.Empty;
    private static string? OptionalText(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static IReadOnlyList<string> TextArray(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Array
            ? property.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).ToArray()
            : [];
    private static IReadOnlyDictionary<string, int> Count(IEnumerable<string> values) =>
        values.GroupBy(value => value, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    private sealed record Occurrence(
        string ManualId, string ManualTitle, string Series, string EquipmentSide, string DisplayContext, string Code,
        string NormalizedCode, string CodeKind, string Meaning, string Page, string Section, string EvidenceLevel,
        string? ShortQuote, IReadOnlyList<string> RequiredMeasurements, IReadOnlyList<string> SafetyNotes, IReadOnlyList<string> Limitations);
}
