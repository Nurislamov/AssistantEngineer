using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Readiness;

public sealed class OwnershipBackfillApplyReadinessResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillApplyReadinessResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Readiness output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var readinessId = SanitizeForFileName(result.ReadinessId);
        var resultJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-readiness-result-{readinessId}.json");
        var resultMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-readiness-result-{readinessId}.md");

        await File.WriteAllTextAsync(resultJsonPath, JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(resultMarkdownPath, BuildMarkdown(result), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillApplyReadinessResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Apply Enablement Readiness");
        builder.AppendLine();
        builder.AppendLine($"- Passed: `{result.Passed}`");
        builder.AppendLine($"- ReadinessId: `{result.ReadinessId}`");
        builder.AppendLine($"- PlanHash: `{result.PlanHash}`");
        builder.AppendLine($"- SignoffPlanHash: `{result.SignoffPlanHash}`");
        builder.AppendLine($"- ApplyInputHash: `{result.ApplyInputHash}`");
        builder.AppendLine($"- RulesetVersion: `{result.RulesetVersion}`");
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
        builder.AppendLine("Warning: apply remains disabled in this stage.");

        return builder.ToString();
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidCharacters.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "readiness" : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Readiness output file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Readiness output path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}

