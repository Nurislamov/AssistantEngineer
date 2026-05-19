using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillEvidenceWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesExpectedEvidenceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-backfill-evidence-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(root);

        try
        {
            var writer = new OwnershipBackfillEvidenceWriter();
            var result = CreateSampleResult();

            await writer.WriteAsync(result, output, CancellationToken.None);

            var summaryJson = Directory.GetFiles(output, "ownership-backfill-dry-run-summary-*.json");
            var summaryMarkdown = Directory.GetFiles(output, "ownership-backfill-dry-run-summary-*.md");
            var unresolvedJson = Directory.GetFiles(output, "ownership-backfill-unresolved-records-*.json");
            var previousValuesJson = Directory.GetFiles(output, "ownership-backfill-previous-values-*.json");

            Assert.Single(summaryJson);
            Assert.Single(summaryMarkdown);
            Assert.Single(unresolvedJson);
            Assert.Single(previousValuesJson);

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryJson[0]));
            Assert.True(summaryDocument.RootElement.TryGetProperty("RunId", out _));
            Assert.True(summaryDocument.RootElement.TryGetProperty("TotalRecordsScanned", out _));

            using var unresolvedDocument = JsonDocument.Parse(File.ReadAllText(unresolvedJson[0]));
            Assert.Equal(JsonValueKind.Array, unresolvedDocument.RootElement.ValueKind);

            using var previousValuesDocument = JsonDocument.Parse(File.ReadAllText(previousValuesJson[0]));
            Assert.Equal(JsonValueKind.Array, previousValuesDocument.RootElement.ValueKind);

            var markdownContent = File.ReadAllText(summaryMarkdown[0]);
            Assert.Contains("# Ownership Backfill Dry-Run Summary", markdownContent, StringComparison.Ordinal);
            Assert.Contains("## Non-claims", markdownContent, StringComparison.Ordinal);
            Assert.DoesNotContain("payload", markdownContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", markdownContent, StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain("payload", File.ReadAllText(summaryJson[0]), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", File.ReadAllText(summaryJson[0]), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_DoesNotWriteOutsideRequestedDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-backfill-evidence-boundary-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(root, "nested", "out");
        Directory.CreateDirectory(root);

        try
        {
            var writer = new OwnershipBackfillEvidenceWriter();
            var result = CreateSampleResult();

            await writer.WriteAsync(result, output, CancellationToken.None);

            var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Assert.All(allFiles, file => Assert.StartsWith(Path.GetFullPath(output), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static OwnershipBackfillDryRunResult CreateSampleResult()
    {
        return new OwnershipBackfillDryRunResult
        {
            Summary = new OwnershipBackfillDryRunSummary
            {
                RunId = "20260517T000000Z-sample",
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Mode = "DryRun",
                TotalRecordsScanned = 0,
                TotalRecordsResolvable = 0,
                TotalRecordsUnresolved = 0,
                UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RecordTypeMetrics =
                [
                    new OwnershipBackfillRecordTypeMetrics
                    {
                        RecordType = "Project",
                        TotalRecords = 0,
                        ResolvableRecords = 0,
                        UnresolvedRecords = 0,
                        AmbiguousRecords = 0,
                        ResolvableRate = 0d,
                        UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
                    }
                ],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            UnresolvedRecords = [],
            PreviousValues = []
        };
    }
}
