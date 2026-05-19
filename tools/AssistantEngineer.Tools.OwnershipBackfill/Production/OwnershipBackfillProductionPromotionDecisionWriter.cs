using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionDecisionWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillProductionPromotionDecision decision,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Production promotion output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var decisionId = SanitizeForFileName(decision.DecisionId);
        var decisionJsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-production-promotion-decision-{decisionId}.json");
        var decisionMarkdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-production-promotion-decision-{decisionId}.md");

        await File.WriteAllTextAsync(decisionJsonPath, JsonSerializer.Serialize(decision, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(decisionMarkdownPath, BuildMarkdown(decision), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillProductionPromotionDecision decision)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Production Promotion Decision");
        builder.AppendLine();
        builder.AppendLine($"- Ready: `{decision.Ready}`");
        builder.AppendLine($"- DecisionStatus: `{decision.DecisionStatus}`");
        builder.AppendLine($"- DecisionId: `{decision.DecisionId}`");
        builder.AppendLine($"- ProductionPromotionHash: `{decision.ProductionPromotionHash}`");
        builder.AppendLine($"- StagingRunHash: `{decision.StagingRunHash}`");
        builder.AppendLine($"- ProductionApplyInputHash: `{decision.ProductionApplyInputHash}`");
        builder.AppendLine($"- ProductionPlanHash: `{decision.ProductionPlanHash}`");
        builder.AppendLine($"- ProductionChangeRequestId: `{decision.ProductionChangeRequestId}`");
        builder.AppendLine();
        builder.AppendLine("## Metrics");
        builder.AppendLine();

        foreach (var metric in decision.Metrics.OrderBy(item => item.Key, StringComparer.Ordinal))
            builder.AppendLine($"- {metric.Key}: `{metric.Value}`");

        builder.AppendLine();
        builder.AppendLine("## Findings");
        builder.AppendLine();

        if (decision.Findings.Count == 0)
        {
            builder.AppendLine("- No findings.");
        }
        else
        {
            foreach (var finding in decision.Findings)
                builder.AppendLine($"- [{finding.Severity}] {finding.Code}: {finding.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();
        foreach (var nonClaim in decision.NonClaims)
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("Warning: production apply remains disabled until a later explicit enablement stage.");
        return builder.ToString();
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidCharacters.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "decision" : sanitized;
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Production promotion output file name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production promotion output path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }
}
