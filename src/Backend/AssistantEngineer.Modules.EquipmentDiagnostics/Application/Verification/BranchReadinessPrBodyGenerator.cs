using System.Text;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class BranchReadinessPrBodyGenerator
{
    public string Render(BranchReadinessReport report, string reportPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var builder = new StringBuilder();
        builder.AppendLine($"# {CreateTitleSuggestion(report)}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"Automated `{report.Scope}` readiness for branch `{report.CurrentBranch}` against `{report.BaseRef}`.");
        builder.AppendLine($"Readiness status: **{report.Status.ToUpperInvariant()}**.");
        builder.AppendLine();
        builder.AppendLine("## Changed Scope");
        builder.AppendLine();
        builder.AppendLine($"- Total changed files: `{report.ChangedFilesSummary.Total}`");
        builder.AppendLine($"- Allowed: `{report.ChangedFilesSummary.Allowed}`");
        builder.AppendLine($"- Suspicious: `{report.ChangedFilesSummary.Suspicious}`");
        builder.AppendLine($"- Forbidden: `{report.ChangedFilesSummary.Forbidden}`");
        builder.AppendLine($"- Staged: `{report.ChangedFilesSummary.Staged}`");
        builder.AppendLine($"- Unstaged: `{report.ChangedFilesSummary.Unstaged}`");
        builder.AppendLine($"- Untracked: `{report.ChangedFilesSummary.Untracked}`");
        builder.AppendLine();
        builder.AppendLine("## Readiness");
        builder.AppendLine();
        builder.AppendLine($"- Blockers: `{report.BlockersCount}`");
        builder.AppendLine($"- Warnings: `{report.WarningsCount}`");
        builder.AppendLine($"- Info: `{report.InfoCount}`");
        builder.AppendLine($"- Report: `{NormalizePath(reportPath)}`");
        builder.AppendLine();
        builder.AppendLine("## EquipmentDiagnostics Verification");
        builder.AppendLine();
        builder.AppendLine($"- Runtime catalog entries: `{report.EquipmentDiagnostics.RuntimeCatalog.TotalEntries}`");
        builder.AppendLine($"- Runtime duplicate keys: `{report.EquipmentDiagnostics.RuntimeCatalog.DuplicateKeys.Count}`");
        builder.AppendLine($"- Runtime catalog blocking issues: `{report.EquipmentDiagnostics.HasBlockingIssues}`");
        builder.AppendLine($"- Staging candidate files: `{report.EquipmentDiagnostics.StagingCandidateFileCount}`");
        builder.AppendLine($"- Staging example/template files: `{report.EquipmentDiagnostics.StagingExampleFileCount}`");
        builder.AppendLine($"- Docs example files: `{report.EquipmentDiagnostics.DocsExampleFileCount}`");
        builder.AppendLine($"- ManualVerified runtime entries: `{report.EquipmentDiagnostics.RuntimeCatalog.ManualVerifiedEntries}`");
        builder.AppendLine($"- Manual codebook occurrences: `{report.EquipmentDiagnostics.ManualCodeBookSummary.CodeOccurrenceCount}`");
        builder.AppendLine($"- Manual codebook sources: `{report.EquipmentDiagnostics.ManualCodeBookSummary.SourceCount}`");
        builder.AppendLine($"- Manual codebook duplicate/conflicts: `{report.EquipmentDiagnostics.ManualCodeBookSummary.DuplicateOrConflictCount}`");
        builder.AppendLine();
        builder.AppendLine("## Manual codebook coverage");
        builder.AppendLine();
        builder.AppendLine($"- Coverage status: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.Status}`");
        builder.AppendLine($"- Ready for staging candidates: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.ReadyForStagingCandidateCount}`");
        builder.AppendLine($"- Reference-only: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.ReferenceOnlyCount}`");
        builder.AppendLine($"- Conflicts: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.ConflictCount}`");
        builder.AppendLine($"- Top recommendations: `{report.EquipmentDiagnostics.CodebookCoverage.Summary.TopRecommendationsCount}`");
        foreach (var recommendation in report.EquipmentDiagnostics.CodebookCoverage.TopPriorityRecommendations.Take(5))
        {
            builder.AppendLine($"- `{recommendation.Code}` ({recommendation.Series}/{recommendation.EquipmentSide}): {recommendation.RecommendedNextAction}");
        }
        builder.AppendLine();
        builder.AppendLine("## Verification Commands");
        builder.AppendLine();
        foreach (var command in report.Commands.OrderBy(command => command.Name, StringComparer.Ordinal))
        {
            builder.AppendLine($"- [{(command.Passed ? "x" : " ")}] `{command.Command}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Checklist");
        builder.AppendLine();
        var scopeClean = report.ChangedFilesSummary.Forbidden == 0;
        var catalogClean = !report.EquipmentDiagnostics.HasBlockingIssues;
        AppendChecklist(builder, scopeClean, "No calculation physics changed.");
        AppendChecklist(builder, scopeClean, "No public calculation API route changed.");
        AppendChecklist(builder, scopeClean, "No EF/DB changes.");
        AppendChecklist(builder, scopeClean, "No Telegram integration.");
        AppendChecklist(builder, scopeClean, "No AI/RAG/vector search.");
        AppendChecklist(builder, catalogClean, "No staging/docs example runtime pollution.");
        AppendChecklist(builder, report.Passed, "Branch readiness report is PASS.");

        if (!report.Passed)
        {
            builder.AppendLine();
            builder.AppendLine("## Blockers");
            builder.AppendLine();
            foreach (var issue in report.Issues
                         .Where(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error)
                         .OrderBy(issue => issue.Path, StringComparer.Ordinal)
                         .ThenBy(issue => issue.Code, StringComparer.Ordinal))
            {
                builder.AppendLine($"- `{issue.Code}` at `{issue.Path}`: {issue.Message}");
            }

            foreach (var command in report.Commands.Where(command => !command.Passed))
            {
                builder.AppendLine($"- Command failed: `{command.Command}`");
            }
        }

        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string CreateTitleSuggestion(BranchReadinessReport report) =>
        report.Passed
            ? $"{report.Scope}: {CreateBranchDescription(report.CurrentBranch)}"
            : $"WIP: {report.Scope} readiness blockers";

    private static string CreateBranchDescription(string branch) =>
        (branch.StartsWith("feature/", StringComparison.Ordinal) ? branch["feature/".Length..] : branch)
        .Replace('-', ' ');

    private static void AppendChecklist(StringBuilder builder, bool complete, string text) =>
        builder.AppendLine($"- [{(complete ? "x" : " ")}] {text}");

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
