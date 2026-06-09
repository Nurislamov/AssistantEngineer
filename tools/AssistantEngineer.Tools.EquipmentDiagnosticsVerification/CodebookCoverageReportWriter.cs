using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class CodebookCoverageReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static (string JsonPath, string MarkdownPath) Write(
        string repoRoot,
        EquipmentDiagnosticsCodebookCoverageReport report)
    {
        var directory = Path.Combine(repoRoot, "artifacts", "verification", "equipment-diagnostics");
        Directory.CreateDirectory(directory);
        var jsonPath = Path.Combine(directory, "codebook-coverage-report.json");
        var markdownPath = Path.Combine(directory, "codebook-coverage-report.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
        File.WriteAllText(markdownPath, CreateMarkdown(report));
        return (jsonPath, markdownPath);
    }

    private static string CreateMarkdown(EquipmentDiagnosticsCodebookCoverageReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# EquipmentDiagnostics Codebook Coverage");
        builder.AppendLine();
        builder.AppendLine($"- Status: **{report.Summary.Status}**");
        builder.AppendLine($"- Runtime codes: `{report.Summary.TotalRuntimeCodes}`");
        builder.AppendLine($"- Staging candidates: `{report.Summary.TotalStagingCandidates}`");
        builder.AppendLine($"- Codebook occurrences: `{report.Summary.TotalCodebookOccurrences}`");
        builder.AppendLine($"- Unique normalized codes: `{report.Summary.UniqueNormalizedCodeCount}`");
        builder.AppendLine($"- Ready for staging: `{report.Summary.ReadyForStagingCandidateCount}`");
        builder.AppendLine($"- Reference-only: `{report.Summary.ReferenceOnlyCount}`");
        builder.AppendLine($"- Conflicts: `{report.Summary.ConflictCount}`");
        builder.AppendLine();
        builder.AppendLine("## Top Recommendations");
        builder.AppendLine();
        foreach (var entry in report.TopPriorityRecommendations)
            builder.AppendLine($"- `{entry.Code}` ({entry.Series}/{entry.EquipmentSide}): {entry.RecommendedNextAction}");
        builder.AppendLine();
        builder.AppendLine("## Next Actions");
        builder.AppendLine();
        foreach (var action in report.NextActions) builder.AppendLine($"- {action}");
        return builder.ToString();
    }
}
