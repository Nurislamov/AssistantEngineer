using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillEvidenceLoader : IOwnershipBackfillEvidenceLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<OwnershipBackfillEvidenceBundle> LoadAsync(
        OwnershipBackfillGateOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.EvidenceDirectory))
            throw new InvalidOperationException("--input evidence directory is required.");

        var fullEvidenceDirectory = Path.GetFullPath(options.EvidenceDirectory);
        if (!Directory.Exists(fullEvidenceDirectory))
            throw new InvalidOperationException("Evidence input directory does not exist.");

        var summaryPath = ResolveSummaryPath(options, fullEvidenceDirectory);
        var summary = await ReadJsonAsync<OwnershipBackfillDryRunSummary>(summaryPath, cancellationToken);

        var unresolvedPath = CombineSafe(fullEvidenceDirectory, $"ownership-backfill-unresolved-records-{SanitizeFileToken(summary.RunId)}.json");
        var previousValuesPath = CombineSafe(fullEvidenceDirectory, $"ownership-backfill-previous-values-{SanitizeFileToken(summary.RunId)}.json");

        var unresolved = await ReadOptionalJsonListAsync<OwnershipBackfillUnresolvedRecord>(unresolvedPath, cancellationToken);
        var unresolvedPropertyNames = await ReadOptionalArrayPropertyNamesAsync(unresolvedPath, cancellationToken);
        var previousValues = await ReadOptionalJsonListAsync<OwnershipBackfillPreviousValueSnapshot>(previousValuesPath, cancellationToken);

        return new OwnershipBackfillEvidenceBundle
        {
            Summary = summary,
            UnresolvedRecords = unresolved,
            PreviousValues = previousValues,
            UnresolvedRecordPropertyNames = unresolvedPropertyNames
        };
    }

    private static string ResolveSummaryPath(OwnershipBackfillGateOptions options, string evidenceDirectory)
    {
        if (!string.IsNullOrWhiteSpace(options.SummaryPath))
        {
            var resolved = Path.GetFullPath(options.SummaryPath);
            EnsureInsideRoot(evidenceDirectory, resolved);
            if (!File.Exists(resolved))
                throw new InvalidOperationException("Specified summary file was not found.");

            return resolved;
        }

        var candidates = Directory.GetFiles(evidenceDirectory, "ownership-backfill-dry-run-summary-*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidOperationException("No ownership backfill dry-run summary JSON found in input directory.");

        return candidates[0];
    }

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var value = JsonSerializer.Deserialize<T>(content, JsonOptions);

        if (value is null)
            throw new InvalidOperationException($"Failed to parse JSON: {Path.GetFileName(path)}");

        return value;
    }

    private static async Task<IReadOnlyList<T>> ReadOptionalJsonListAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return Array.Empty<T>();

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var value = JsonSerializer.Deserialize<List<T>>(content, JsonOptions);

        return value ?? [];
    }

    private static async Task<IReadOnlySet<string>> ReadOptionalArrayPropertyNamesAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        using var json = JsonDocument.Parse(content);

        if (json.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Expected JSON array in {Path.GetFileName(path)}.");

        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in json.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var property in item.EnumerateObject())
            {
                propertyNames.Add(property.Name);
            }
        }

        return propertyNames;
    }

    private static string CombineSafe(string rootDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("File name must not contain path separators.");

        var candidate = Path.GetFullPath(Path.Combine(rootDirectory, fileName));
        EnsureInsideRoot(rootDirectory, candidate);
        return candidate;
    }

    private static void EnsureInsideRoot(string rootDirectory, string candidatePath)
    {
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(rootDirectory));
        var normalizedCandidate = Path.GetFullPath(candidatePath);

        if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal outside evidence directory is not allowed.");
        }
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
        return string.IsNullOrWhiteSpace(sanitized) ? "run" : sanitized;
    }
}
