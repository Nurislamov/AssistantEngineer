using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillPlanWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesAllPlanArtifacts()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyPlanWriter();
            var result = CreatePlanResult();

            await writer.WriteAsync(result, root, cancellationToken: CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-plan-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-summary-draft-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-summary-draft-*.md"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-planned-records-*.json"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WriteAsync_WrittenJsonIsParsable()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyPlanWriter();
            var result = CreatePlanResult();

            await writer.WriteAsync(result, root, cancellationToken: CancellationToken.None);

            foreach (var path in Directory.GetFiles(root, "*.json"))
            {
                using var _ = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            }
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WriteAsync_DoesNotWriteOutsideOutputDirectory()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyPlanWriter();
            var result = CreatePlanResult();

            await writer.WriteAsync(result, root, cancellationToken: CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-apply-plan-*.json", SearchOption.TopDirectoryOnly)
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
    public async Task WriteAsync_ArtifactsContainNoSecretFields()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyPlanWriter();
            var result = CreatePlanResult();

            await writer.WriteAsync(result, root, cancellationToken: CancellationToken.None);

            var allJson = string.Join(
                Environment.NewLine,
                Directory.GetFiles(root, "*.json").Select(File.ReadAllText));

            Assert.DoesNotContain("payload", allJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", allJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", allJson, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillPlanResult CreatePlanResult()
    {
        var plannedRecord = new OwnershipBackfillPlannedRecord
        {
            RecordType = "Project",
            RecordId = "11",
            CurrentProjectId = 11,
            CurrentBuildingId = null,
            CurrentOrganizationId = null,
            CurrentOwnerUserId = null,
            ProposedProjectId = 11,
            ProposedBuildingId = null,
            ProposedOrganizationId = 77,
            ProposedOwnerUserId = null,
            Reason = OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
            SourceEvidence = "ownership-backfill-unresolved-records-20260518.json",
            DeterministicRecordHash = "hash-record"
        };

        return new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "run-001",
            PlanId = "plan-001",
            PlanHash = "hash-001",
            RulesetVersion = "P6-05",
            PlannedRecords = [plannedRecord],
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = "plan-001",
                PlanHash = "hash-001",
                Mode = "PlanOnly",
                TotalRecordsPlanned = 1,
                TotalRecordsSkipped = 0,
                TotalRecordsUnresolved = 1,
                PlannedByRecordType = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["Project"] = 1
                },
                SkippedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RequiredFutureApplyPreconditions =
                [
                    "validate-evidence gate result Passed=true"
                ],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-plan-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
