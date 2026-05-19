using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class TestOnlyOwnershipBackfillApplyExecutorTests
{
    [Fact]
    public async Task RefusesWhenTestOnlyExecutionFalse()
    {
        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var request = CreateRequest(testOnlyExecution: false);

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_EXECUTION_REQUIRED");
    }

    [Fact]
    public async Task RefusesProductionProvider()
    {
        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var request = CreateRequest(executionProvider: "PostgreSQL");

        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_PROVIDER_NOT_ALLOWED");
    }

    [Fact]
    public async Task AppliesPlannedProjectOwnershipToTestData()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11,
                BuildingId = null,
                OrganizationId = null,
                OwnerUserId = null,
                SimulateFailure = false
            }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, null, null, 11, null, 77, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.TotalRecordsUpdated);
        Assert.True(store.TryGetRecord("Project", "11", out var updated));
        Assert.Equal(77, updated.OrganizationId);
    }

    [Fact]
    public async Task CapturesPreviousValuesBeforeUpdate()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11,
                BuildingId = null,
                OrganizationId = null,
                OwnerUserId = 5,
                SimulateFailure = false
            }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, null, 5, 11, null, 77, 5, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.Single(result.PreviousValues);
        Assert.Equal(11, result.PreviousValues[0].PreviousProjectId);
        Assert.Null(result.PreviousValues[0].PreviousOrganizationId);
        Assert.Equal(5, result.PreviousValues[0].PreviousOwnerUserId);
    }

    [Fact]
    public async Task AlreadyMatchingRecordSkipped()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11,
                OrganizationId = 77
            }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, 77, null, 11, null, 77, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.Equal(0, result.TotalRecordsUpdated);
        Assert.Equal(1, result.TotalRecordsSkipped);
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_ALREADY_MATCHES");
    }

    [Fact]
    public async Task CurrentValueConflictSkipped()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11,
                OrganizationId = 88
            }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, null, null, 11, null, 77, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.Equal(0, result.TotalRecordsUpdated);
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_CURRENT_VALUE_CONFLICT");
    }

    [Fact]
    public async Task AmbiguousOrUnresolvedPlannedRecordSkipped()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Scenario",
                RecordId = "s-1",
                ProjectId = 10
            },
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11
            }
        ]);

        var ambiguous = CreatePlannedRecord("Scenario", "s-1", 10, null, null, null, 10, null, 77, null, OwnershipBackfillUnresolvedReasons.ScenarioOwnershipAmbiguous);
        var unresolved = new OwnershipBackfillPlannedRecord
        {
            RecordType = "Project",
            RecordId = "11",
            CurrentProjectId = 11,
            CurrentBuildingId = null,
            CurrentOrganizationId = null,
            CurrentOwnerUserId = null,
            ProposedProjectId = 11,
            ProposedBuildingId = null,
            ProposedOrganizationId = null,
            ProposedOwnerUserId = null,
            Reason = OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
            SourceEvidence = "ownership-backfill-unresolved-records-run.json",
            DeterministicRecordHash = "placeholder"
        };

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: CreatePlan([ambiguous, unresolved]), store: store), CancellationToken.None);

        Assert.Equal(0, result.TotalRecordsUpdated);
        Assert.Equal(2, result.TotalRecordsSkipped);
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_AMBIGUOUS_RECORD_SKIPPED");
        Assert.Contains(result.Findings, finding => finding.Code == "TEST_ONLY_UNRESOLVED_RECORD_SKIPPED");
    }

    [Fact]
    public async Task RepeatedExecutionIsIdempotent()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState
            {
                RecordType = "Project",
                RecordId = "11",
                ProjectId = 11,
                OrganizationId = null
            }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, null, null, 11, null, 77, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var first = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);
        var second = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.Equal(1, first.TotalRecordsUpdated);
        Assert.Equal(0, second.TotalRecordsUpdated);
        Assert.Equal(1, second.TotalRecordsSkipped);
    }

    [Fact]
    public async Task BatchSizeRespectedAcrossMultipleBatches()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState { RecordType = "Project", RecordId = "1", ProjectId = 1, OrganizationId = null },
            new OwnershipBackfillTestRecordState { RecordType = "Project", RecordId = "2", ProjectId = 2, OrganizationId = null },
            new OwnershipBackfillTestRecordState { RecordType = "Project", RecordId = "3", ProjectId = 3, OrganizationId = null }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "1", 1, null, null, null, 1, null, 71, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing),
            CreatePlannedRecord("Project", "2", 2, null, null, null, 2, null, 72, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing),
            CreatePlannedRecord("Project", "3", 3, null, null, null, 3, null, 73, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var request = CreateRequest(
            plan: plan,
            store: store,
            batchSize: 1);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(3, result.TotalRecordsUpdated);
    }

    [Fact]
    public async Task FailureInOneRecordDoesNotUpdateThatRecord()
    {
        var store = new InMemoryOwnershipBackfillTestRecordStore(
        [
            new OwnershipBackfillTestRecordState { RecordType = "Project", RecordId = "1", ProjectId = 1, OrganizationId = null, SimulateFailure = false },
            new OwnershipBackfillTestRecordState { RecordType = "Project", RecordId = "2", ProjectId = 2, OrganizationId = null, SimulateFailure = true }
        ]);

        var plan = CreatePlan(
        [
            CreatePlannedRecord("Project", "1", 1, null, null, null, 1, null, 71, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing),
            CreatePlannedRecord("Project", "2", 2, null, null, null, 2, null, 72, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(plan: plan, store: store), CancellationToken.None);

        Assert.Equal(1, result.TotalRecordsUpdated);
        Assert.Equal(1, result.TotalRecordsFailed);
        Assert.True(store.TryGetRecord("Project", "2", out var failedRecord));
        Assert.Null(failedRecord.OrganizationId);
    }

    [Fact]
    public async Task ResultIncludesNonClaims()
    {
        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.NotEmpty(result.NonClaims);
        Assert.Contains("No ownership backfill execution claim.", result.NonClaims);
    }

    [Fact]
    public async Task ResultSerializationContainsNoPayloadOrSecretFields()
    {
        var executor = new TestOnlyOwnershipBackfillApplyExecutor();
        var result = await executor.ExecuteAsync(CreateRequest(), CancellationToken.None);
        var json = JsonSerializer.Serialize(result);

        Assert.DoesNotContain("payload", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
    }

    private static OwnershipBackfillApplyExecutionRequest CreateRequest(
        bool testOnlyExecution = true,
        string executionProvider = "InMemory",
        OwnershipBackfillPlanResult? plan = null,
        InMemoryOwnershipBackfillTestRecordStore? store = null,
        int batchSize = 500)
    {
        var effectivePlan = plan ?? CreatePlan(
        [
            CreatePlannedRecord("Project", "11", 11, null, null, null, 11, null, 77, null, OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing)
        ]);

        return new OwnershipBackfillApplyExecutionRequest
        {
            Options = new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = "test",
                GateResultPath = "test",
                PlanPath = "test",
                PlanSignoffPath = "test",
                OutputDirectory = "test",
                DatabaseProvider = "None",
                ConnectionString = "not-used",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = batchSize
            },
            Plan = effectivePlan,
            Signoff = new OwnershipBackfillPlanSignoffArtifact
            {
                SignoffId = "signoff-001",
                PlanId = effectivePlan.PlanId,
                PlanHash = effectivePlan.PlanHash,
                PlanPath = "plan.json",
                Reviewer = "reviewer",
                Ticket = "CHG-1",
                ConfirmationPhraseAccepted = true,
                SignedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                ToolStage = "P6-06",
                Notes = null,
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            TestOnlyExecution = testOnlyExecution,
            ExecutionProvider = executionProvider,
            TestRecordStore = store ?? new InMemoryOwnershipBackfillTestRecordStore(
            [
                new OwnershipBackfillTestRecordState
                {
                    RecordType = "Project",
                    RecordId = "11",
                    ProjectId = 11,
                    BuildingId = null,
                    OrganizationId = null,
                    OwnerUserId = null,
                    SimulateFailure = false
                }
            ])
        };
    }

    private static OwnershipBackfillPlanResult CreatePlan(IReadOnlyList<OwnershipBackfillPlannedRecord> records)
    {
        var ordered = records
            .OrderBy(record => record.RecordType, StringComparer.Ordinal)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ToArray();

        var hashInput = string.Join("|", ordered.Select(record => $"{record.RecordType}:{record.RecordId}:{record.DeterministicRecordHash}"));
        var planHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput))).ToLowerInvariant();

        return new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "run-001",
            PlanId = planHash[..16],
            PlanHash = planHash,
            RulesetVersion = "P6-05",
            PlannedRecords = ordered,
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = planHash[..16],
                PlanHash = planHash,
                Mode = "PlanOnly",
                TotalRecordsPlanned = ordered.Length,
                TotalRecordsSkipped = 0,
                TotalRecordsUnresolved = 0,
                PlannedByRecordType = ordered.GroupBy(record => record.RecordType, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
                SkippedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RequiredFutureApplyPreconditions =
                [
                    "signed plan required"
                ],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static OwnershipBackfillPlannedRecord CreatePlannedRecord(
        string recordType,
        string recordId,
        int? currentProjectId,
        int? currentBuildingId,
        int? currentOrganizationId,
        int? currentOwnerUserId,
        int? proposedProjectId,
        int? proposedBuildingId,
        int? proposedOrganizationId,
        int? proposedOwnerUserId,
        string reason)
    {
        var hashInput = string.Join(
            '|',
            recordType,
            recordId,
            ToToken(proposedProjectId),
            ToToken(proposedBuildingId),
            ToToken(proposedOrganizationId),
            ToToken(proposedOwnerUserId),
            reason);

        var recordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput))).ToLowerInvariant();

        return new OwnershipBackfillPlannedRecord
        {
            RecordType = recordType,
            RecordId = recordId,
            CurrentProjectId = currentProjectId,
            CurrentBuildingId = currentBuildingId,
            CurrentOrganizationId = currentOrganizationId,
            CurrentOwnerUserId = currentOwnerUserId,
            ProposedProjectId = proposedProjectId,
            ProposedBuildingId = proposedBuildingId,
            ProposedOrganizationId = proposedOrganizationId,
            ProposedOwnerUserId = proposedOwnerUserId,
            Reason = reason,
            SourceEvidence = "ownership-backfill-unresolved-records-run-001.json",
            DeterministicRecordHash = recordHash
        };
    }

    private static string ToToken(int? value) => value?.ToString() ?? "null";
}
