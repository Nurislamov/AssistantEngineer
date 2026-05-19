using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingAcceptanceResultWriterTests
{
    [Fact]
    public async Task WritesJsonAndMarkdownArtifacts()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillStagingAcceptanceResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-staging-acceptance-result-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-staging-acceptance-result-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WrittenJsonIsParsable()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillStagingAcceptanceResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var path = Directory.GetFiles(root, "ownership-backfill-staging-acceptance-result-*.json").Single();
            using var _ = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task DoesNotWriteOutsideOutputDirectory()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillStagingAcceptanceResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-staging-acceptance-result-*.json", SearchOption.TopDirectoryOnly)
                .Where(path => !path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(leaked);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OutputContainsNoSecretsOrPayloads()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillStagingAcceptanceResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var output = string.Join(Environment.NewLine, Directory.GetFiles(root).Select(File.ReadAllText));
            Assert.DoesNotContain("password", output, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", output, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("payload", output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillStagingAcceptanceResult CreateResult()
    {
        return new OwnershipBackfillStagingAcceptanceResult
        {
            Accepted = true,
            AcceptanceId = "20260518170000-abcdef123456",
            StagingRunHash = "staging-run-hash-001",
            ApplyInputHash = "apply-input-hash-001",
            PlanHash = "plan-hash-001",
            SignoffId = "signoff-001",
            ReadinessId = "readiness-001",
            OperatorId = "operator-001",
            StagingChangeId = "staging-change-001",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["PostApplyUnresolvedRate"] = "0.000000"
            },
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No staging apply execution claim.",
                "No production apply enabled claim.",
                "No ownership backfill execution claim."
            ]
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-staging-acceptance-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
