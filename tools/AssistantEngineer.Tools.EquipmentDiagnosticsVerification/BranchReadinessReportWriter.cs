using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class BranchReadinessReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static (string JsonPath, string MarkdownPath) Write(
        string repoRoot,
        string? outputDirectory,
        BranchReadinessReport report)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(repoRoot, "artifacts", "verification", "branch-readiness")
            : Path.GetFullPath(outputDirectory, repoRoot);
        Directory.CreateDirectory(directory);

        var jsonPath = Path.Combine(directory, "branch-readiness-report.json");
        var markdownPath = Path.Combine(directory, "branch-readiness-report.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
        File.WriteAllText(markdownPath, CreateMarkdown(report));

        return (jsonPath, markdownPath);
    }

    private static string CreateMarkdown(BranchReadinessReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Branch Readiness Report");
        builder.AppendLine();
        builder.AppendLine($"- Status: **{report.Status}**");
        builder.AppendLine($"- Branch: `{report.CurrentBranch}`");
        builder.AppendLine($"- Base ref: `{report.BaseRef}`");
        builder.AppendLine($"- Scope: `{report.Scope}`");
        builder.AppendLine($"- Blockers: `{report.BlockersCount}`");
        builder.AppendLine($"- Warnings: `{report.WarningsCount}`");
        builder.AppendLine($"- Changed files: `{report.ChangedFilesSummary.Total}`");
        builder.AppendLine();
        builder.AppendLine("## Scope Summary");
        builder.AppendLine();
        builder.AppendLine($"- Allowed: `{report.ChangedFilesSummary.Allowed}`");
        builder.AppendLine($"- Suspicious: `{report.ChangedFilesSummary.Suspicious}`");
        builder.AppendLine($"- Forbidden: `{report.ChangedFilesSummary.Forbidden}`");
        builder.AppendLine($"- Generated/ignored candidates: `{report.ChangedFilesSummary.GeneratedIgnoredCandidates}`");
        builder.AppendLine();
        builder.AppendLine("## EquipmentDiagnostics QA");
        builder.AppendLine();
        builder.AppendLine($"- Runtime entries: `{report.EquipmentDiagnostics.RuntimeCatalog.TotalEntries}`");
        builder.AppendLine($"- Runtime duplicate keys: `{report.EquipmentDiagnostics.RuntimeCatalog.DuplicateKeys.Count}`");
        builder.AppendLine($"- Staging candidate files: `{report.EquipmentDiagnostics.StagingCandidateFileCount}`");
        builder.AppendLine($"- Docs example files: `{report.EquipmentDiagnostics.DocsExampleFileCount}`");
        builder.AppendLine($"- Manual codebook sources: `{report.EquipmentDiagnostics.ManualCodeBookSummary.SourceCount}`");
        builder.AppendLine($"- Manual codebook occurrences: `{report.EquipmentDiagnostics.ManualCodeBookSummary.CodeOccurrenceCount}`");
        builder.AppendLine($"- Manual codebook promotable candidates: `{report.EquipmentDiagnostics.ManualCodeBookSummary.PromotableCandidatesCount}`");
        builder.AppendLine($"- Manual codebook reference-only: `{report.EquipmentDiagnostics.ManualCodeBookSummary.ReferenceOnlyCount}`");
        builder.AppendLine($"- Manual codebook blocked/needs-review: `{report.EquipmentDiagnostics.ManualCodeBookSummary.BlockedOrNeedsReviewCount}`");
        builder.AppendLine($"- Manual codebook duplicate/conflicts: `{report.EquipmentDiagnostics.ManualCodeBookSummary.DuplicateOrConflictCount}`");
        builder.AppendLine($"- Codebook coverage status: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.Status}`");
        builder.AppendLine($"- Ready for staging candidates: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.ReadyForStagingCandidateCount}`");
        builder.AppendLine($"- Coverage conflicts: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.ConflictCount}`");
        builder.AppendLine($"- Blocking issues: `{report.EquipmentDiagnostics.HasBlockingIssues}`");
        builder.AppendLine();
        builder.AppendLine("## Command Checks");
        builder.AppendLine();
        foreach (var command in report.Commands)
        {
            builder.AppendLine($"- {(command.Passed ? "PASS" : "FAIL")} `{command.Command}`: {command.Summary}");
        }

        builder.AppendLine();
        builder.AppendLine("## Issues");
        builder.AppendLine();
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("- None.");
        }
        else
        {
            foreach (var issue in report.Issues)
            {
                builder.AppendLine($"- {issue.Severity} `{issue.Code}` at `{issue.Path}`: {issue.Message}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Next Actions");
        builder.AppendLine();
        foreach (var action in report.NextActions)
        {
            builder.AppendLine($"- {action}");
        }

        return builder.ToString();
    }
}
