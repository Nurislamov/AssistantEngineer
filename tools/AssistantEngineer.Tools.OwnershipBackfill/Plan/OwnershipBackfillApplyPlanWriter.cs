using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillApplyPlanWriter : IOwnershipBackfillApplyPlanWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillPlanResult result,
        string outputDirectory,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Plan output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var planId = SanitizeForFileName(result.PlanId);

        var planJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-plan-{planId}.json");
        var summaryJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-summary-draft-{planId}.json");
        var summaryMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-summary-draft-{planId}.md");
        var plannedRecordsPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-planned-records-{planId}.json");

        if (!forceOverwrite)
        {
            var existingPaths = new[] { planJsonPath, summaryJsonPath, summaryMarkdownPath, plannedRecordsPath }
                .Where(File.Exists)
                .ToArray();

            if (existingPaths.Length > 0)
                throw new InvalidOperationException("Plan artifacts already exist for the computed plan id. Use --force-overwrite true to overwrite.");
        }

        await File.WriteAllTextAsync(planJsonPath, JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(summaryJsonPath, JsonSerializer.Serialize(result.SummaryDraft, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(summaryMarkdownPath, BuildSummaryMarkdown(result), cancellationToken);
        await File.WriteAllTextAsync(plannedRecordsPath, JsonSerializer.Serialize(result.PlannedRecords, JsonOptions), cancellationToken);
    }

    private static string BuildSummaryMarkdown(OwnershipBackfillPlanResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Apply Plan Draft");
        builder.AppendLine();
        builder.AppendLine($"- RunId: `{result.RunId}`");
        builder.AppendLine($"- PlanId: `{result.PlanId}`");
        builder.AppendLine($"- PlanHash: `{result.PlanHash}`");
        builder.AppendLine($"- RulesetVersion: `{result.RulesetVersion}`");
        builder.AppendLine($"- Mode: `{result.SummaryDraft.Mode}`");
        builder.AppendLine($"- TotalRecordsPlanned: `{result.SummaryDraft.TotalRecordsPlanned}`");
        builder.AppendLine($"- TotalRecordsSkipped: `{result.SummaryDraft.TotalRecordsSkipped}`");
        builder.AppendLine($"- TotalRecordsUnresolved: `{result.SummaryDraft.TotalRecordsUnresolved}`");
        builder.AppendLine();
        builder.AppendLine("## Planned By Record Type");
        builder.AppendLine();

        if (result.SummaryDraft.PlannedByRecordType.Count == 0)
        {
            builder.AppendLine("- No records planned.");
        }
        else
        {
            foreach (var metric in result.SummaryDraft.PlannedByRecordType.OrderBy(item => item.Key, StringComparer.Ordinal))
                builder.AppendLine($"- {metric.Key}: `{metric.Value}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Skipped By Reason");
        builder.AppendLine();

        if (result.SummaryDraft.SkippedByReason.Count == 0)
        {
            builder.AppendLine("- No skipped records.");
        }
        else
        {
            foreach (var skipped in result.SummaryDraft.SkippedByReason.OrderBy(item => item.Key, StringComparer.Ordinal))
                builder.AppendLine($"- {skipped.Key}: `{skipped.Value}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Findings");
        builder.AppendLine();

        if (result.Findings.Count == 0)
        {
            builder.AppendLine("- No findings.");
        }
        else
        {
            foreach (var finding in result.Findings)
                builder.AppendLine($"- [{finding.Severity}] {finding.Code}: {finding.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();

        foreach (var nonClaim in result.NonClaims)
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("Warning: apply mode remains disabled until a separate enablement stage.");

        return builder.ToString();
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidCharacters.Contains(character)).ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "plan"
            : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Plan file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Plan output path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
