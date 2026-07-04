namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public static class BranchReadinessFailureSummaryFormatter
{
    private const int MaximumDisplayedIssues = 10;

    public static IReadOnlyList<string> Format(
        BranchReadinessReport report,
        string reportPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var lines = new List<string>
        {
            $"Failure details for scope '{report.Scope}':"
        };

        AppendBranchIssues(lines, report);
        AppendEquipmentDiagnosticsBlockers(lines, report);
        AppendFailedCommands(lines, report.Commands);

        lines.Add("Suggested next actions:");
        lines.AddRange(report.NextActions.Select(action => $"- {action}"));
        lines.Add($"Full report: {reportPath}");
        return lines;
    }

    private static void AppendBranchIssues(
        ICollection<string> lines,
        BranchReadinessReport report)
    {
        if (report.Issues.Count == 0)
        {
            return;
        }

        lines.Add($"Issues ({report.Issues.Count}):");
        foreach (var issue in report.Issues
                     .OrderByDescending(issue => issue.Severity)
                     .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                     .ThenBy(issue => issue.Path, StringComparer.Ordinal)
                     .Take(MaximumDisplayedIssues))
        {
            lines.Add(
                $"- severity={issue.Severity}; code={issue.Code}; path={issue.Path}; message={issue.Message}");

            var changedFile = FindChangedFile(report.ChangedFiles, issue.Path);
            if (changedFile is not null &&
                changedFile.ScopeClassification is BranchReadinessScopeClassification.Forbidden or
                    BranchReadinessScopeClassification.Suspicious)
            {
                lines.Add(
                    $"  scopeClassification={changedFile.ScopeClassification}; scopeReason={changedFile.ScopeReason}");
            }
        }

        AppendOmittedCount(lines, report.Issues.Count);
    }

    private static void AppendEquipmentDiagnosticsBlockers(
        ICollection<string> lines,
        BranchReadinessReport report)
    {
        var blockers = report.EquipmentDiagnostics.Sections
            .SelectMany(section => section.Issues)
            .Where(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error)
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Path, StringComparer.Ordinal)
            .ToArray();
        if (blockers.Length == 0)
        {
            return;
        }

        lines.Add($"EquipmentDiagnostics blockers ({blockers.Length}):");
        foreach (var issue in blockers.Take(MaximumDisplayedIssues))
        {
            lines.Add(
                $"- severity={issue.Severity}; code={issue.Code}; path={issue.Path}; message={issue.Message}");
        }

        AppendOmittedCount(lines, blockers.Length);
    }

    private static void AppendFailedCommands(
        ICollection<string> lines,
        IReadOnlyList<BranchReadinessCommandResult> commands)
    {
        var failures = commands.Where(command => !command.Passed).ToArray();
        if (failures.Length == 0)
        {
            return;
        }

        lines.Add($"Failed commands ({failures.Length}):");
        foreach (var command in failures)
        {
            lines.Add(
                $"- name={command.Name}; exitCode={command.ExitCode}; command={command.Command}; summary={command.Summary}");
        }
    }

    private static BranchReadinessChangedFile? FindChangedFile(
        IReadOnlyList<BranchReadinessChangedFile> changedFiles,
        string issuePath) =>
        changedFiles.FirstOrDefault(file =>
            issuePath.Equals(file.Path, StringComparison.OrdinalIgnoreCase) ||
            issuePath.StartsWith($"{file.Path}:", StringComparison.OrdinalIgnoreCase));

    private static void AppendOmittedCount(
        ICollection<string> lines,
        int totalCount)
    {
        if (totalCount > MaximumDisplayedIssues)
        {
            lines.Add($"- ... {totalCount - MaximumDisplayedIssues} more; see the full report.");
        }
    }
}
