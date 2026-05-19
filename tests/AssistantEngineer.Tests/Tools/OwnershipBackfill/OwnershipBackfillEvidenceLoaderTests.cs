using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillEvidenceLoaderTests
{
    [Fact]
    public async Task LoadsSummaryUnresolvedAndPreviousValues()
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518020202-test-run";
            await WriteSummaryAsync(root, runId, OwnershipBackfillConstants.NonClaims);
            await WriteUnresolvedAsync(root, runId, "reason");
            await WritePreviousValuesAsync(root, runId);

            var loader = new OwnershipBackfillEvidenceLoader();
            var result = await loader.LoadAsync(CreateOptions(root, root));

            Assert.Equal(runId, result.Summary.RunId);
            Assert.Single(result.UnresolvedRecords);
            Assert.Single(result.PreviousValues);
            Assert.Contains("recordType", result.UnresolvedRecordPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingOptionalFiles_ReturnsEmptyLists()
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518020203-test-run";
            await WriteSummaryAsync(root, runId, OwnershipBackfillConstants.NonClaims);

            var loader = new OwnershipBackfillEvidenceLoader();
            var result = await loader.LoadAsync(CreateOptions(root, root));

            Assert.Empty(result.UnresolvedRecords);
            Assert.Empty(result.PreviousValues);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task RefusesSummaryPathTraversalOutsideInputDirectory()
    {
        var root = CreateTempDirectory();

        try
        {
            var outside = Path.Combine(Path.GetTempPath(), $"ae-outside-{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(outside, "{}");

            var loader = new OwnershipBackfillEvidenceLoader();
            var options = new OwnershipBackfillGateOptions
            {
                EvidenceDirectory = root,
                OutputDirectory = root,
                SummaryPath = outside
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync(options));

            File.Delete(outside);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task InvalidSummaryJson_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var summaryPath = Path.Combine(root, "ownership-backfill-dry-run-summary-invalid.json");
            await File.WriteAllTextAsync(summaryPath, "{ bad json");

            var loader = new OwnershipBackfillEvidenceLoader();

            await Assert.ThrowsAsync<JsonException>(() => loader.LoadAsync(CreateOptions(root, root)));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillGateOptions CreateOptions(string input, string output)
    {
        return new OwnershipBackfillGateOptions
        {
            EvidenceDirectory = input,
            OutputDirectory = output
        };
    }

    private static async Task WriteSummaryAsync(string directory, string runId, IReadOnlyList<string> nonClaims)
    {
        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "DryRun",
            TotalRecordsScanned = 0,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics =
            [
                CreateMetric("Project"),
                CreateMetric("Building"),
                CreateMetric("WorkflowState"),
                CreateMetric("Scenario"),
                CreateMetric("Job"),
                CreateMetric("JobEvent"),
                CreateMetric("ScenarioHistory")
            ],
            NonClaims = nonClaims
        };

        var path = Path.Combine(directory, $"ownership-backfill-dry-run-summary-{runId}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task WriteUnresolvedAsync(string directory, string runId, string reason)
    {
        var records = new[]
        {
            new OwnershipBackfillUnresolvedRecord
            {
                RecordType = "Project",
                RecordId = "1",
                Reason = reason,
                CandidateProjectId = 1,
                CandidateBuildingId = null,
                CandidateOrganizationId = null,
                Notes = "note"
            }
        };

        var path = Path.Combine(directory, $"ownership-backfill-unresolved-records-{runId}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task WritePreviousValuesAsync(string directory, string runId)
    {
        var values = new[]
        {
            new OwnershipBackfillPreviousValueSnapshot
            {
                RecordType = "Project",
                RecordId = "1",
                PreviousProjectId = 1,
                PreviousBuildingId = null,
                PreviousOrganizationId = null,
                PreviousOwnerUserId = null
            }
        };

        var path = Path.Combine(directory, $"ownership-backfill-previous-values-{runId}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static OwnershipBackfillRecordTypeMetrics CreateMetric(string recordType)
    {
        return new OwnershipBackfillRecordTypeMetrics
        {
            RecordType = recordType,
            TotalRecords = 0,
            ResolvableRecords = 0,
            UnresolvedRecords = 0,
            AmbiguousRecords = 0,
            ResolvableRate = 0d,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-evidence-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
