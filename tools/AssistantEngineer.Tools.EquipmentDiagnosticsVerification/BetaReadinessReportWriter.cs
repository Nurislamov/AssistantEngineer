using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class BetaReadinessReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static (string JsonPath, string MarkdownPath) Write(
        string repoRoot,
        string? outputPath,
        string? markdownOutputPath,
        EquipmentDiagnosticsBetaReadinessReport report)
    {
        var jsonPath = Resolve(repoRoot, outputPath, "beta-readiness-report.json");
        var markdownPath = Resolve(repoRoot, markdownOutputPath, "beta-readiness-summary.md");
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(markdownPath)!);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
        File.WriteAllText(markdownPath, RenderMarkdown(report));
        return (jsonPath, markdownPath);
    }

    internal static string RenderMarkdown(EquipmentDiagnosticsBetaReadinessReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# EquipmentDiagnostics Closed Beta Readiness");
        builder.AppendLine();
        builder.AppendLine($"- Overall status: **{report.OverallStatus}**");
        builder.AppendLine($"- Blockers: `{report.BlockerCount}`");
        builder.AppendLine($"- Warnings: `{report.WarningCount}`");
        builder.AppendLine($"- Base ref: `{report.RepositoryBaseRef}`");
        builder.AppendLine("- Scope: closed beta only; not production or public release.");
        builder.AppendLine();
        builder.AppendLine("## Readiness Matrix");
        builder.AppendLine();
        builder.AppendLine("| Section | Status |");
        builder.AppendLine("|---|---|");
        foreach (var section in report.Sections)
        {
            builder.AppendLine($"| {section.Name} | {section.Status} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Known Limitations");
        builder.AppendLine();
        foreach (var limitation in report.KnownLimitations)
        {
            builder.AppendLine($"- {limitation}");
        }

        return builder.ToString();
    }

    private static string Resolve(string repoRoot, string? path, string defaultName) =>
        string.IsNullOrWhiteSpace(path)
            ? Path.Combine(repoRoot, "artifacts", "verification", "equipment-diagnostics", defaultName)
            : Path.GetFullPath(path, repoRoot);
}
