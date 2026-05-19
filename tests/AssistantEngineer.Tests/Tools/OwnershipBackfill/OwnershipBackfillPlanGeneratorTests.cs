using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillPlanGeneratorTests
{
    [Fact]
    public async Task PassedGate_WithValidEvidence_GeneratesPlan()
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518090101-test-run";
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateResultPath = Path.Combine(root, "gate-result.json");

            await WriteEvidenceAsync(
                evidenceDirectory,
                runId,
                unresolvedRecords:
                [
                    CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77)
                ],
                previousValues:
                [
                    new OwnershipBackfillPreviousValueSnapshot
                    {
                        RecordType = "Project",
                        RecordId = "11",
                        PreviousProjectId = 11,
                        PreviousBuildingId = null,
                        PreviousOrganizationId = null,
                        PreviousOwnerUserId = null
                    }
                ]);

            await WriteGateResultAsync(gateResultPath, passed: true);

            var generator = new OwnershipBackfillApplyPlanGenerator();
            var result = await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectory,
                GateResultPath = gateResultPath,
                OutputDirectory = Path.Combine(root, "out")
            });

            Assert.True(result.Succeeded);
            Assert.Single(result.PlannedRecords);
            Assert.NotEmpty(result.PlanHash);
            Assert.Equal("PlanOnly", result.SummaryDraft.Mode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailedGate_RefusesPlan()
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518090102-test-run";
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateResultPath = Path.Combine(root, "gate-result.json");

            await WriteEvidenceAsync(evidenceDirectory, runId, unresolvedRecords: [], previousValues: []);
            await WriteGateResultAsync(gateResultPath, passed: false);

            var generator = new OwnershipBackfillApplyPlanGenerator();

            await Assert.ThrowsAsync<OwnershipBackfillPlanGateFailedException>(async () =>
                await generator.GenerateAsync(new OwnershipBackfillPlanOptions
                {
                    EvidenceDirectory = evidenceDirectory,
                    GateResultPath = gateResultPath,
                    OutputDirectory = Path.Combine(root, "out")
                }));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task AmbiguousRecord_IsSkipped()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Scenario", "sc-1", OwnershipBackfillUnresolvedReasons.ScenarioOwnershipAmbiguous, 11, null, 77),
            CreatePrevious("Scenario", "sc-1", 11, null, null, null));

        Assert.Empty(result.PlannedRecords);
        Assert.Equal(1, result.SummaryDraft.SkippedByReason["AmbiguousRecord"]);
    }

    [Fact]
    public async Task MissingCandidateOrganization_IsSkipped()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, null),
            CreatePrevious("Project", "11", 11, null, null, null));

        Assert.Empty(result.PlannedRecords);
        Assert.Equal(1, result.SummaryDraft.SkippedByReason["MissingCandidateOrganization"]);
    }

    [Fact]
    public async Task CurrentValueConflict_IsSkipped()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 88),
            CreatePrevious("Project", "11", 11, null, 77, null));

        Assert.Empty(result.PlannedRecords);
        Assert.Equal(1, result.SummaryDraft.SkippedByReason["CurrentValueConflict"]);
    }

    [Fact]
    public async Task AlreadyMatchingRecord_IsSkipped()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77),
            CreatePrevious("Project", "11", 11, null, 77, null));

        Assert.Empty(result.PlannedRecords);
        Assert.Equal(1, result.SummaryDraft.SkippedByReason["AlreadyMatches"]);
    }

    [Fact]
    public async Task SameEvidence_ProducesSamePlanHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518090103-test-run";
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateResultPath = Path.Combine(root, "gate-result.json");

            await WriteEvidenceAsync(
                evidenceDirectory,
                runId,
                unresolvedRecords:
                [
                    CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77)
                ],
                previousValues:
                [
                    CreatePrevious("Project", "11", 11, null, null, null)
                ]);

            await WriteGateResultAsync(gateResultPath, passed: true);

            var generator = new OwnershipBackfillApplyPlanGenerator();
            var options = new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectory,
                GateResultPath = gateResultPath,
                OutputDirectory = Path.Combine(root, "out")
            };

            var first = await generator.GenerateAsync(options);
            var second = await generator.GenerateAsync(options);

            Assert.Equal(first.PlanHash, second.PlanHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task DifferentEvidence_ProducesDifferentPlanHash()
    {
        var first = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77),
            CreatePrevious("Project", "11", 11, null, null, null));

        var second = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 88),
            CreatePrevious("Project", "11", 11, null, null, null));

        Assert.NotEqual(first.PlanHash, second.PlanHash);
    }

    [Fact]
    public async Task PlanIncludesNonClaims()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77),
            CreatePrevious("Project", "11", 11, null, null, null));

        Assert.NotEmpty(result.NonClaims);
        Assert.Contains(result.NonClaims, claim => claim.Contains("No ownership backfill execution claim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PlanRecordHasNoPayloadOrSecretFields()
    {
        var result = await GenerateSingleRecordPlanAsync(
            CreateUnresolved("Project", "11", OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing, 11, null, 77),
            CreatePrevious("Project", "11", 11, null, null, null));

        var json = JsonSerializer.Serialize(result.PlannedRecords[0]);
        Assert.DoesNotContain("payload", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<OwnershipBackfillPlanResult> GenerateSingleRecordPlanAsync(
        OwnershipBackfillUnresolvedRecord unresolvedRecord,
        OwnershipBackfillPreviousValueSnapshot previous)
    {
        var root = CreateTempDirectory();

        try
        {
            var runId = "20260518090100-test-run";
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateResultPath = Path.Combine(root, "gate-result.json");

            await WriteEvidenceAsync(evidenceDirectory, runId, [unresolvedRecord], [previous]);
            await WriteGateResultAsync(gateResultPath, passed: true);

            var generator = new OwnershipBackfillApplyPlanGenerator();
            return await generator.GenerateAsync(new OwnershipBackfillPlanOptions
            {
                EvidenceDirectory = evidenceDirectory,
                GateResultPath = gateResultPath,
                OutputDirectory = Path.Combine(root, "out")
            });
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static async Task WriteEvidenceAsync(
        string evidenceDirectory,
        string runId,
        IReadOnlyList<OwnershipBackfillUnresolvedRecord> unresolvedRecords,
        IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> previousValues)
    {
        Directory.CreateDirectory(evidenceDirectory);

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "DryRun",
            TotalRecordsScanned = unresolvedRecords.Count,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = unresolvedRecords.Count,
            UnresolvedByReason = unresolvedRecords
                .GroupBy(record => record.Reason, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
            RecordTypeMetrics = BuildMetrics(unresolvedRecords),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var summaryPath = Path.Combine(evidenceDirectory, $"ownership-backfill-dry-run-summary-{runId}.json");
        var unresolvedPath = Path.Combine(evidenceDirectory, $"ownership-backfill-unresolved-records-{runId}.json");
        var previousPath = Path.Combine(evidenceDirectory, $"ownership-backfill-previous-values-{runId}.json");

        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary));
        await File.WriteAllTextAsync(unresolvedPath, JsonSerializer.Serialize(unresolvedRecords));
        await File.WriteAllTextAsync(previousPath, JsonSerializer.Serialize(previousValues));
    }

    private static async Task WriteGateResultAsync(string path, bool passed)
    {
        var result = new OwnershipBackfillGateResult
        {
            Passed = passed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = passed ? "Gate passed." : "Gate failed.",
            RunId = "gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["maxTotalUnresolvedRate"] = "0.05"
            },
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(result));
    }

    private static IReadOnlyList<OwnershipBackfillRecordTypeMetrics> BuildMetrics(IReadOnlyList<OwnershipBackfillUnresolvedRecord> unresolvedRecords)
    {
        return OwnershipBackfillConstants.KnownRecordTypes
            .Select(recordType =>
            {
                var unresolvedCount = unresolvedRecords.Count(record => string.Equals(record.RecordType, recordType, StringComparison.Ordinal));
                return new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = recordType,
                    TotalRecords = unresolvedCount,
                    ResolvableRecords = 0,
                    UnresolvedRecords = unresolvedCount,
                    AmbiguousRecords = unresolvedRecords.Count(record =>
                        string.Equals(record.RecordType, recordType, StringComparison.Ordinal) &&
                        record.Reason.Contains("Ambiguous", StringComparison.Ordinal)),
                    ResolvableRate = 0d,
                    UnresolvedByReason = unresolvedRecords
                        .Where(record => string.Equals(record.RecordType, recordType, StringComparison.Ordinal))
                        .GroupBy(record => record.Reason, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal)
                };
            })
            .ToArray();
    }

    private static OwnershipBackfillUnresolvedRecord CreateUnresolved(
        string recordType,
        string recordId,
        string reason,
        int? candidateProjectId,
        int? candidateBuildingId,
        int? candidateOrganizationId)
    {
        return new OwnershipBackfillUnresolvedRecord
        {
            RecordType = recordType,
            RecordId = recordId,
            Reason = reason,
            CandidateProjectId = candidateProjectId,
            CandidateBuildingId = candidateBuildingId,
            CandidateOrganizationId = candidateOrganizationId,
            Notes = "test"
        };
    }

    private static OwnershipBackfillPreviousValueSnapshot CreatePrevious(
        string recordType,
        string recordId,
        int? previousProjectId,
        int? previousBuildingId,
        int? previousOrganizationId,
        int? previousOwnerUserId)
    {
        return new OwnershipBackfillPreviousValueSnapshot
        {
            RecordType = recordType,
            RecordId = recordId,
            PreviousProjectId = previousProjectId,
            PreviousBuildingId = previousBuildingId,
            PreviousOrganizationId = previousOrganizationId,
            PreviousOwnerUserId = previousOwnerUserId
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-plan-generator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
