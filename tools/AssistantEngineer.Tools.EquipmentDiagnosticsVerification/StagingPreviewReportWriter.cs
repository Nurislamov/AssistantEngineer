using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class StagingPreviewReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static (string JsonPath, string MarkdownPath) Write(string repoRoot, EquipmentDiagnosticsStagingPreviewReport report)
    {
        var directory = Path.Combine(repoRoot, "artifacts", "verification", "equipment-diagnostics");
        Directory.CreateDirectory(directory);
        var jsonPath = Path.Combine(directory, "staging-candidate-preview.json");
        var markdownPath = Path.Combine(directory, "staging-candidate-preview.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
        var markdown = new StringBuilder()
            .AppendLine("# EquipmentDiagnostics Staging Candidate Preview")
            .AppendLine()
            .AppendLine($"- Status: **{report.Status}**")
            .AppendLine($"- Candidate count: `{report.CandidateCount}`")
            .AppendLine($"- Policy: {report.ArtifactPolicy}")
            .AppendLine()
            .AppendLine("## Candidates")
            .AppendLine();
        foreach (var candidate in report.Candidates)
            markdown.AppendLine($"- `{candidate.Code}` ({candidate.Series}/{candidate.Category})");
        File.WriteAllText(markdownPath, markdown.ToString());
        return (jsonPath, markdownPath);
    }
}
