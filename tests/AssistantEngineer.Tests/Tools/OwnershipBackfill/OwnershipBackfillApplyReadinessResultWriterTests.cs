using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyReadinessResultWriterTests
{
    [Fact]
    public async Task WritesJsonAndMarkdownArtifacts()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyReadinessResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-readiness-result-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-readiness-result-*.md"));
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
            var writer = new OwnershipBackfillApplyReadinessResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var jsonPath = Directory.GetFiles(root, "ownership-backfill-apply-readiness-result-*.json").Single();
            using var _ = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
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
            var writer = new OwnershipBackfillApplyReadinessResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-apply-readiness-result-*.json", SearchOption.TopDirectoryOnly)
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
            var writer = new OwnershipBackfillApplyReadinessResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var text = string.Join(Environment.NewLine, Directory.GetFiles(root).Select(File.ReadAllText));
            Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("payload", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillApplyReadinessResult CreateResult()
    {
        return new OwnershipBackfillApplyReadinessResult
        {
            Passed = true,
            ReadinessId = "20260518142000-abcdef123456",
            ApplyInputHash = "apply-hash-001",
            PlanHash = "plan-hash-001",
            SignoffPlanHash = "plan-hash-001",
            RulesetVersion = "P6-08",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["SignoffReviewer"] = "reviewer-1",
                ["SignoffTicket"] = "CHG-100"
            },
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-readiness-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}

