using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Production;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillProductionPromotionDecisionWriterTests
{
    [Fact]
    public async Task WritesJsonAndMarkdownArtifacts()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillProductionPromotionDecisionWriter();
            await writer.WriteAsync(CreateDecision(), root, CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-production-promotion-decision-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-production-promotion-decision-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WrittenJson_IsParsable()
    {
        var root = CreateTempDirectory();
        try
        {
            var writer = new OwnershipBackfillProductionPromotionDecisionWriter();
            await writer.WriteAsync(CreateDecision(), root, CancellationToken.None);

            var path = Directory.GetFiles(root, "ownership-backfill-production-promotion-decision-*.json").Single();
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
            var writer = new OwnershipBackfillProductionPromotionDecisionWriter();
            await writer.WriteAsync(CreateDecision(), root, CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-production-promotion-decision-*.json", SearchOption.TopDirectoryOnly)
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
            var writer = new OwnershipBackfillProductionPromotionDecisionWriter();
            await writer.WriteAsync(CreateDecision(), root, CancellationToken.None);

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

    private static OwnershipBackfillProductionPromotionDecision CreateDecision()
    {
        return new OwnershipBackfillProductionPromotionDecision
        {
            Ready = true,
            DecisionId = "20260518183000-abcd1234",
            DecisionStatus = "ReadyForProductionApproval",
            ProductionPromotionHash = "promotion-hash-001",
            StagingRunHash = "staging-run-hash-001",
            ProductionApplyInputHash = "production-apply-input-hash-001",
            ProductionPlanHash = "production-plan-hash-001",
            ProductionChangeRequestId = "CHG-PROD-001",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RulesetVersion"] = "P6-13"
            },
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No production apply enabled claim.",
                "No staging apply execution claim.",
                "No production ownership backfill execution claim."
            ]
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-production-promotion-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
