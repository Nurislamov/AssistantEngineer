using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class DeterministicPlanHashTests
{
    [Fact]
    public async Task PlannedRecordOrder_DoesNotAffectPlanHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDirectoryA = Path.Combine(root, "a");
            var evidenceDirectoryB = Path.Combine(root, "b");
            var gatePath = Path.Combine(root, "gate.json");

            await WriteGateResultAsync(gatePath);

            var unresolvedA = new[]
            {
                CreateUnresolved("Project", "2", 2, 100),
                CreateUnresolved("Project", "1", 1, 100)
            };

            var unresolvedB = unresolvedA.Reverse().ToArray();

            await WriteEvidenceAsync(evidenceDirectoryA, "20260518111111-run", unresolvedA, DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow);
            await WriteEvidenceAsync(evidenceDirectoryB, "20260518111111-run", unresolvedB, DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow);

            var generator = new OwnershipBackfillApplyPlanGenerator();
            var first = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectoryA,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-a"),
                RulesetVersion = "P6-05"
            });

            var second = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectoryB,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-b"),
                RulesetVersion = "P6-05"
            });

            Assert.Equal(first.PlanHash, second.PlanHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task TimestampDifferences_DoNotAffectPlanHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var gatePath = Path.Combine(root, "gate.json");
            await WriteGateResultAsync(gatePath);

            var unresolved = new[] { CreateUnresolved("Project", "1", 1, 100) };

            var evidenceDirectoryA = Path.Combine(root, "a");
            var evidenceDirectoryB = Path.Combine(root, "b");

            await WriteEvidenceAsync(
                evidenceDirectoryA,
                "20260518111112-run",
                unresolved,
                DateTimeOffset.Parse("2026-05-18T00:00:00Z"),
                DateTimeOffset.Parse("2026-05-18T00:01:00Z"));

            await WriteEvidenceAsync(
                evidenceDirectoryB,
                "20260518111112-run",
                unresolved,
                DateTimeOffset.Parse("2026-05-18T05:00:00Z"),
                DateTimeOffset.Parse("2026-05-18T05:01:00Z"));

            var generator = new OwnershipBackfillApplyPlanGenerator();
            var first = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectoryA,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-a"),
                RulesetVersion = "P6-05"
            });

            var second = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectoryB,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-b"),
                RulesetVersion = "P6-05"
            });

            Assert.Equal(first.PlanHash, second.PlanHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task RulesetVersionChange_ChangesPlanHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var gatePath = Path.Combine(root, "gate.json");
            var evidenceDirectory = Path.Combine(root, "evidence");

            await WriteGateResultAsync(gatePath);
            await WriteEvidenceAsync(
                evidenceDirectory,
                "20260518111113-run",
                [CreateUnresolved("Project", "1", 1, 100)],
                DateTimeOffset.UtcNow.AddMinutes(-2),
                DateTimeOffset.UtcNow);

            var generator = new OwnershipBackfillApplyPlanGenerator();
            var first = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectory,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-a"),
                RulesetVersion = "P6-05"
            });

            var second = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectory,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out-b"),
                RulesetVersion = "P6-05A"
            });

            Assert.NotEqual(first.PlanHash, second.PlanHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static async Task WriteEvidenceAsync(
        string evidenceDirectory,
        string runId,
        IReadOnlyList<OwnershipBackfillUnresolvedRecord> unresolved,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        Directory.CreateDirectory(evidenceDirectory);

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Mode = "DryRun",
            TotalRecordsScanned = unresolved.Count,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = unresolved.Count,
            UnresolvedByReason = unresolved
                .GroupBy(item => item.Reason, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(recordType => new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = recordType,
                    TotalRecords = unresolved.Count(item => string.Equals(item.RecordType, recordType, StringComparison.Ordinal)),
                    ResolvableRecords = 0,
                    UnresolvedRecords = unresolved.Count(item => string.Equals(item.RecordType, recordType, StringComparison.Ordinal)),
                    AmbiguousRecords = 0,
                    ResolvableRate = 0d,
                    UnresolvedByReason = unresolved
                        .Where(item => string.Equals(item.RecordType, recordType, StringComparison.Ordinal))
                        .GroupBy(item => item.Reason, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal)
                })
                .ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var previous = unresolved
            .Select(item => new OwnershipBackfillPreviousValueSnapshot
            {
                RecordType = item.RecordType,
                RecordId = item.RecordId,
                PreviousProjectId = item.CandidateProjectId,
                PreviousBuildingId = item.CandidateBuildingId,
                PreviousOrganizationId = null,
                PreviousOwnerUserId = null
            })
            .ToArray();

        await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, $"ownership-backfill-dry-run-summary-{runId}.json"), JsonSerializer.Serialize(summary));
        await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, $"ownership-backfill-unresolved-records-{runId}.json"), JsonSerializer.Serialize(unresolved));
        await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, $"ownership-backfill-previous-values-{runId}.json"), JsonSerializer.Serialize(previous));
    }

    private static async Task WriteGateResultAsync(string gatePath)
    {
        var gate = new OwnershipBackfillGateResult
        {
            Passed = true,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = "Gate passed.",
            RunId = "gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gate));
    }

    private static OwnershipBackfillUnresolvedRecord CreateUnresolved(string recordType, string recordId, int projectId, int organizationId)
    {
        return new OwnershipBackfillUnresolvedRecord
        {
            RecordType = recordType,
            RecordId = recordId,
            Reason = OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
            CandidateProjectId = projectId,
            CandidateBuildingId = null,
            CandidateOrganizationId = organizationId,
            Notes = "test"
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-plan-hash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
