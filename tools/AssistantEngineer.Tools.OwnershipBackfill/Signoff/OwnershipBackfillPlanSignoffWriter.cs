using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public sealed class OwnershipBackfillPlanSignoffWriter : IOwnershipBackfillPlanSignoffWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        OwnershipBackfillPlanSignoffArtifact artifact,
        string outputDirectory,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Signoff output directory is required.");

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var signoffId = SanitizeFileToken(artifact.SignoffId);
        var jsonPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-plan-signoff-{signoffId}.json");
        var markdownPath = CombineSafe(fullOutputDirectory, $"ownership-backfill-plan-signoff-{signoffId}.md");

        if (!forceOverwrite)
        {
            var existing = new[] { jsonPath, markdownPath }.Where(File.Exists).ToArray();
            if (existing.Length > 0)
                throw new InvalidOperationException("Signoff artifact already exists. Use --force-overwrite true to overwrite.");
        }

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(artifact, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(artifact), cancellationToken);
    }

    private static string BuildMarkdown(OwnershipBackfillPlanSignoffArtifact artifact)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Ownership Backfill Plan Signoff");
        builder.AppendLine();
        builder.AppendLine($"- SignoffId: `{artifact.SignoffId}`");
        builder.AppendLine($"- PlanId: `{artifact.PlanId}`");
        builder.AppendLine($"- PlanHash: `{artifact.PlanHash}`");
        builder.AppendLine($"- Reviewer: `{artifact.Reviewer}`");
        builder.AppendLine($"- Ticket: `{artifact.Ticket}`");
        builder.AppendLine($"- SignedAtUtc: `{artifact.SignedAtUtc:O}`");
        builder.AppendLine($"- ExpiresAtUtc: `{FormatDate(artifact.ExpiresAtUtc)}`");
        builder.AppendLine($"- ToolStage: `{artifact.ToolStage}`");

        if (!string.IsNullOrWhiteSpace(artifact.Notes))
            builder.AppendLine($"- Notes: {artifact.Notes}");

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();

        foreach (var nonClaim in artifact.NonClaims)
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("Warning: apply execution remains disabled until a later explicit stage.");

        return builder.ToString();
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.ToString("O") : "Not set";
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("Signoff file name must not contain path separators.");

        var root = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Signoff path traversal is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static string SanitizeFileToken(string token)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(token.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "signoff" : sanitized;
    }
}
