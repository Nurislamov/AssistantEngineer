using System.Text;
using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tools.OwnershipBackfill.Evidence;

public sealed class OwnershipBackfillEvidenceWriter : IOwnershipBackfillEvidenceWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillDryRunResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Evidence output directory is required.", nameof(outputDirectory));

        cancellationToken.ThrowIfCancellationRequested();

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var runId = SanitizeForFileName(result.Summary.RunId);
        var summaryJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-dry-run-summary-{runId}.json");
        var summaryMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-dry-run-summary-{runId}.md");
        var unresolvedRecordsPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-unresolved-records-{runId}.json");
        var previousValuesPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-previous-values-{runId}.json");

        await File.WriteAllTextAsync(summaryJsonPath, JsonSerializer.Serialize(result.Summary, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(unresolvedRecordsPath, JsonSerializer.Serialize(result.UnresolvedRecords, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(previousValuesPath, JsonSerializer.Serialize(result.PreviousValues, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(summaryMarkdownPath, BuildMarkdownSummary(result), cancellationToken);
    }

    private static string BuildMarkdownSummary(OwnershipBackfillDryRunResult result)
    {
        var summary = result.Summary;
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Dry-Run Summary");
        builder.AppendLine();
        builder.AppendLine($"- RunId: `{summary.RunId}`");
        builder.AppendLine($"- StartedAtUtc: `{summary.StartedAtUtc:O}`");
        builder.AppendLine($"- CompletedAtUtc: `{summary.CompletedAtUtc:O}`");
        builder.AppendLine($"- Mode: `{summary.Mode}`");
        builder.AppendLine($"- TotalRecordsScanned: `{summary.TotalRecordsScanned}`");
        builder.AppendLine($"- TotalRecordsResolvable: `{summary.TotalRecordsResolvable}`");
        builder.AppendLine($"- TotalRecordsUnresolved: `{summary.TotalRecordsUnresolved}`");
        builder.AppendLine();
        builder.AppendLine("## Record type metrics");
        builder.AppendLine();

        foreach (var metric in summary.RecordTypeMetrics.OrderBy(item => item.RecordType, StringComparer.Ordinal))
        {
            builder.AppendLine($"- {metric.RecordType}: total={metric.TotalRecords}, resolvable={metric.ResolvableRecords}, unresolved={metric.UnresolvedRecords}, ambiguous={metric.AmbiguousRecords}, resolvableRate={metric.ResolvableRate:0.0000}");
        }

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();

        foreach (var nonClaim in summary.NonClaims)
        {
            builder.AppendLine($"- {nonClaim}");
        }

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
            throw new InvalidOperationException("Evidence file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Evidence path traversal is not allowed.");
        }

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
