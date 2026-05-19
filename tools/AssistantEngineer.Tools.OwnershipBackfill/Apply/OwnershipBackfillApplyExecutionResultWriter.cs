using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyExecutionResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillApplyExecutionResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Rehearsal output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var executionId = SanitizeForFileName(result.ExecutionId);

        var resultJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-rehearsal-result-{executionId}.json");
        var resultMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-apply-rehearsal-result-{executionId}.md");
        var previousValuesPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-rehearsal-previous-values-{executionId}.json");

        await File.WriteAllTextAsync(resultJsonPath, JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(resultMarkdownPath, BuildMarkdown(result), cancellationToken);
        await File.WriteAllTextAsync(previousValuesPath, JsonSerializer.Serialize(result.PreviousValues, JsonOptions), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillApplyExecutionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Ownership Backfill Apply Rehearsal Result");
        builder.AppendLine();
        builder.AppendLine($"- ExecutionId: `{result.ExecutionId}`");
        builder.AppendLine($"- Mode: `{result.Mode}`");
        builder.AppendLine($"- Succeeded: `{result.Succeeded}`");
        builder.AppendLine($"- TotalRecordsPlanned: `{result.TotalRecordsPlanned}`");
        builder.AppendLine($"- TotalRecordsUpdated: `{result.TotalRecordsUpdated}`");
        builder.AppendLine($"- TotalRecordsSkipped: `{result.TotalRecordsSkipped}`");
        builder.AppendLine($"- TotalRecordsFailed: `{result.TotalRecordsFailed}`");
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
        builder.AppendLine("Warning: this is a test-only rehearsal artifact. Apply execution remains disabled.");

        return builder.ToString();
    }

    private static string SanitizeForFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "rehearsal" : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Rehearsal output file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rehearsal output path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}

