using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillGateResultWriter : IOwnershipBackfillGateResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillGateResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Gate output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var runId = SanitizeForFileName(result.RunId);
        var jsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-evidence-gate-result-{runId}.json");
        var markdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-evidence-gate-result-{runId}.md");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(result), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillGateResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Evidence Gate Result");
        builder.AppendLine();
        builder.AppendLine($"- RunId: `{result.RunId}`");
        builder.AppendLine($"- Passed: `{result.Passed}`");
        builder.AppendLine($"- Summary: {result.Summary}");
        builder.AppendLine();
        builder.AppendLine("## Thresholds");
        builder.AppendLine();

        foreach (var threshold in result.Thresholds.OrderBy(item => item.Key, StringComparer.Ordinal))
            builder.AppendLine($"- {threshold.Key}: `{threshold.Value}`");

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
            {
                builder.AppendLine($"- [{finding.Severity}] {finding.Code}: {finding.Message}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();

        foreach (var nonClaim in result.NonClaims)
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine(result.Passed
            ? "Next action: evidence is eligible for staged review toward future apply-mode design."
            : "Next action: resolve blocking findings and rerun dry-run + validate-evidence before any apply-mode planning.");

        return builder.ToString();
    }

    private static string SanitizeForFileName(string runId)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(runId.Where(character => !invalidCharacters.Contains(character)).ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "run"
            : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Gate result file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gate result path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
