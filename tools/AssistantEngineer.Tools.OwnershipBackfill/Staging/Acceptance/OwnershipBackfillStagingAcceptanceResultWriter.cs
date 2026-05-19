using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingAcceptanceResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillStagingAcceptanceResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Staging acceptance output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var acceptanceId = SanitizeForFileName(result.AcceptanceId);
        var resultJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-staging-acceptance-result-{acceptanceId}.json");
        var resultMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-staging-acceptance-result-{acceptanceId}.md");

        await File.WriteAllTextAsync(resultJsonPath, JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(resultMarkdownPath, BuildMarkdown(result), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillStagingAcceptanceResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Staging Acceptance Result");
        builder.AppendLine();
        builder.AppendLine($"- Accepted: `{result.Accepted}`");
        builder.AppendLine($"- AcceptanceId: `{result.AcceptanceId}`");
        builder.AppendLine($"- StagingRunHash: `{result.StagingRunHash}`");
        builder.AppendLine($"- ApplyInputHash: `{result.ApplyInputHash}`");
        builder.AppendLine($"- PlanHash: `{result.PlanHash}`");
        builder.AppendLine($"- SignoffId: `{result.SignoffId}`");
        builder.AppendLine($"- ReadinessId: `{result.ReadinessId}`");
        builder.AppendLine($"- OperatorId: `{result.OperatorId}`");
        builder.AppendLine($"- StagingChangeId: `{result.StagingChangeId}`");
        builder.AppendLine();
        builder.AppendLine("## Metrics");
        builder.AppendLine();

        foreach (var metric in result.Metrics.OrderBy(item => item.Key, StringComparer.Ordinal))
            builder.AppendLine($"- {metric.Key}: `{metric.Value}`");

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
        builder.AppendLine("Warning: production apply remains disabled.");

        return builder.ToString();
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidCharacters.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "acceptance" : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Staging acceptance output file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Staging acceptance output path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
