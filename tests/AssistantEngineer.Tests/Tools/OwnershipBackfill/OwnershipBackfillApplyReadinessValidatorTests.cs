using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyReadinessValidatorTests
{
    [Fact]
    public async Task ValidCompleteArtifactChain_Passes()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(new OwnershipBackfillApplyReadinessOptions
            {
                DryRunSummaryPath = paths.DryRunPath,
                GateResultPath = paths.GatePath,
                PlanPath = paths.PlanPath,
                SignoffPath = paths.SignoffPath,
                PreviousValuesPath = paths.PreviousValuesPath,
                OutputDirectory = Path.Combine(root, "out"),
                MaxSignoffAgeHours = 24,
                RequireRollbackReadiness = true,
                RulesetVersion = "P6-08"
            });

            Assert.True(validation.Result.Passed);
            Assert.Equal(0, validation.ExitCode);
            Assert.False(string.IsNullOrWhiteSpace(validation.Result.ApplyInputHash));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingDryRun_FailsWithExitOne()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            File.Delete(paths.DryRunPath);
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Equal(1, validation.ExitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GateFailed_FailsWithExitTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, gatePassed: false);
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_GATE_FAILED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PlanHashMismatchWithSignoff_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, signoffPlanHashOverride: "different-hash");
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_SIGNOFF_PLANHASH_MISMATCH");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ExpectedPlanHashMismatch_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var options = CreateOptions(root, paths);
            options = new OwnershipBackfillApplyReadinessOptions
            {
                DryRunSummaryPath = options.DryRunSummaryPath,
                GateResultPath = options.GateResultPath,
                PlanPath = options.PlanPath,
                SignoffPath = options.SignoffPath,
                PreviousValuesPath = options.PreviousValuesPath,
                OutputDirectory = options.OutputDirectory,
                MaxSignoffAgeHours = options.MaxSignoffAgeHours,
                RequireRollbackReadiness = options.RequireRollbackReadiness,
                RulesetVersion = options.RulesetVersion,
                ExpectedPlanHash = "not-plan-hash"
            };
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(options);

            Assert.False(validation.Result.Passed);
            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_EXPECTED_PLANHASH_MISMATCH");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ExpiredSignoff_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, signoffExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_SIGNOFF_EXPIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingReviewerOrTicket_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, signoffReviewer: "", signoffTicket: "");
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_SIGNOFF_REVIEWER_MISSING");
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_SIGNOFF_TICKET_MISSING");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingPreviousValuesSnapshotForPlannedRecord_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, omitPreviousValuesRecord: true);
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_PREVIOUS_VALUES_INCOMPLETE");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task RollbackReadinessRequiredAndIncompletePreviousValues_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root, omitPreviousValuesRecord: true);
            var options = CreateOptions(root, paths);
            options = new OwnershipBackfillApplyReadinessOptions
            {
                DryRunSummaryPath = options.DryRunSummaryPath,
                GateResultPath = options.GateResultPath,
                PlanPath = options.PlanPath,
                SignoffPath = options.SignoffPath,
                PreviousValuesPath = options.PreviousValuesPath,
                OutputDirectory = options.OutputDirectory,
                MaxSignoffAgeHours = options.MaxSignoffAgeHours,
                RequireRollbackReadiness = true,
                RulesetVersion = options.RulesetVersion,
                ExpectedPlanHash = options.ExpectedPlanHash
            };
            var validator = new OwnershipBackfillApplyReadinessValidator();

            var validation = await validator.ValidateAsync(options);

            Assert.False(validation.Result.Passed);
            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_PREVIOUS_VALUES_INCOMPLETE");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ArtifactsWithPayloadLikeFields_Fail()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var content = await File.ReadAllTextAsync(paths.PlanPath);
            var mutated = content.Replace("\"RecordId\":\"11\"", "\"RecordId\":\"11\",\"PayloadJson\":\"x\"");
            await File.WriteAllTextAsync(paths.PlanPath, mutated);

            var validator = new OwnershipBackfillApplyReadinessValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_FORBIDDEN_FIELD");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ArtifactsWithSecretLikeFields_Fail()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var content = await File.ReadAllTextAsync(paths.SignoffPath);
            var mutated = content.Replace("\"Ticket\":\"CHG-100\"", "\"Ticket\":\"CHG-100\",\"Password\":\"x\"");
            await File.WriteAllTextAsync(paths.SignoffPath, mutated);

            var validator = new OwnershipBackfillApplyReadinessValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, paths));

            Assert.False(validation.Result.Passed);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "READINESS_FORBIDDEN_FIELD");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SameArtifactSet_ProducesSameApplyInputHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var validator = new OwnershipBackfillApplyReadinessValidator();
            var options = CreateOptions(root, paths);

            var first = await validator.ValidateAsync(options);
            var second = await validator.ValidateAsync(options);

            Assert.Equal(first.Result.ApplyInputHash, second.Result.ApplyInputHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ChangedPlan_ProducesDifferentApplyInputHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidChainAsync(root);
            var validator = new OwnershipBackfillApplyReadinessValidator();
            var options = CreateOptions(root, paths);

            var first = await validator.ValidateAsync(options);

            var plan = JsonSerializer.Deserialize<OwnershipBackfillPlanResult>(await File.ReadAllTextAsync(paths.PlanPath))!;
            var modifiedPlan = new OwnershipBackfillPlanResult
            {
                Succeeded = plan.Succeeded,
                RunId = plan.RunId,
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                RulesetVersion = plan.RulesetVersion,
                PlannedRecords =
                [
                    new OwnershipBackfillPlannedRecord
                    {
                        RecordType = "Project",
                        RecordId = "11",
                        CurrentProjectId = 11,
                        CurrentBuildingId = null,
                        CurrentOrganizationId = null,
                        CurrentOwnerUserId = null,
                        ProposedProjectId = 11,
                        ProposedBuildingId = null,
                        ProposedOrganizationId = 78,
                        ProposedOwnerUserId = null,
                        Reason = OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
                        SourceEvidence = "ownership-backfill-unresolved-records-run-001.json",
                        DeterministicRecordHash = "different-record-hash"
                    }
                ],
                SummaryDraft = plan.SummaryDraft,
                Findings = plan.Findings,
                NonClaims = plan.NonClaims
            };
            await File.WriteAllTextAsync(paths.PlanPath, JsonSerializer.Serialize(modifiedPlan));

            var second = await validator.ValidateAsync(options);

            Assert.NotEqual(first.Result.ApplyInputHash, second.Result.ApplyInputHash);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillApplyReadinessOptions CreateOptions(string root, (string DryRunPath, string GatePath, string PlanPath, string SignoffPath, string PreviousValuesPath) paths)
    {
        return new OwnershipBackfillApplyReadinessOptions
        {
            DryRunSummaryPath = paths.DryRunPath,
            GateResultPath = paths.GatePath,
            PlanPath = paths.PlanPath,
            SignoffPath = paths.SignoffPath,
            PreviousValuesPath = paths.PreviousValuesPath,
            OutputDirectory = Path.Combine(root, "out"),
            MaxSignoffAgeHours = 24,
            RequireRollbackReadiness = true,
            RulesetVersion = "P6-08"
        };
    }

    private static async Task<(string DryRunPath, string GatePath, string PlanPath, string SignoffPath, string PreviousValuesPath)> WriteValidChainAsync(
        string root,
        bool gatePassed = true,
        string? signoffPlanHashOverride = null,
        DateTimeOffset? signoffExpiresAtUtc = null,
        string signoffReviewer = "reviewer-1",
        string signoffTicket = "CHG-100",
        bool omitPreviousValuesRecord = false)
    {
        var dryRunSummary = new OwnershipBackfillDryRunSummary
        {
            RunId = "run-001",
            StartedAtUtc = DateTimeOffset.Parse("2026-05-18T00:00:00Z"),
            CompletedAtUtc = DateTimeOffset.Parse("2026-05-18T00:05:00Z"),
            Mode = "DryRun",
            TotalRecordsScanned = 1,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 1,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing] = 1
            },
            RecordTypeMetrics =
            [
                new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = "Project",
                    TotalRecords = 1,
                    ResolvableRecords = 0,
                    UnresolvedRecords = 1,
                    AmbiguousRecords = 0,
                    ResolvableRate = 0d,
                    UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        [OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing] = 1
                    }
                },
                CreateZeroMetric("Building"),
                CreateZeroMetric("WorkflowState"),
                CreateZeroMetric("Scenario"),
                CreateZeroMetric("Job"),
                CreateZeroMetric("JobEvent"),
                CreateZeroMetric("ScenarioHistory")
            ],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var gateResult = new OwnershipBackfillGateResult
        {
            Passed = gatePassed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = gatePassed ? "Gate passed." : "Gate failed.",
            RunId = "run-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var plan = new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "plan-run-001",
            PlanId = "plan-001",
            PlanHash = "plan-hash-001",
            RulesetVersion = "P6-05",
            PlannedRecords =
            [
                new OwnershipBackfillPlannedRecord
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
                    SourceEvidence = "ownership-backfill-unresolved-records-run-001.json",
                    DeterministicRecordHash = "record-hash-001"
                }
            ],
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = "plan-001",
                PlanHash = "plan-hash-001",
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
                    "signed plan required"
                ],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var signoff = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "signoff-001",
            PlanId = "plan-001",
            PlanHash = signoffPlanHashOverride ?? "plan-hash-001",
            PlanPath = "ownership-backfill-apply-plan-plan-001.json",
            Reviewer = signoffReviewer,
            Ticket = signoffTicket,
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAtUtc = signoffExpiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(4),
            ToolStage = "P6-06",
            Notes = null,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var previousValues = omitPreviousValuesRecord
            ? new List<OwnershipBackfillPreviousValueSnapshot>()
            : new List<OwnershipBackfillPreviousValueSnapshot>
            {
                new()
                {
                    RecordType = "Project",
                    RecordId = "11",
                    PreviousProjectId = 11,
                    PreviousBuildingId = null,
                    PreviousOrganizationId = null,
                    PreviousOwnerUserId = null
                }
            };

        var dryRunPath = Path.Combine(root, "dry-run-summary.json");
        var gatePath = Path.Combine(root, "gate-result.json");
        var planPath = Path.Combine(root, "apply-plan.json");
        var signoffPath = Path.Combine(root, "plan-signoff.json");
        var previousValuesPath = Path.Combine(root, "previous-values.json");

        await File.WriteAllTextAsync(dryRunPath, JsonSerializer.Serialize(dryRunSummary));
        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gateResult));
        await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));
        await File.WriteAllTextAsync(signoffPath, JsonSerializer.Serialize(signoff));
        await File.WriteAllTextAsync(previousValuesPath, JsonSerializer.Serialize(previousValues));

        return (dryRunPath, gatePath, planPath, signoffPath, previousValuesPath);
    }

    private static OwnershipBackfillRecordTypeMetrics CreateZeroMetric(string recordType)
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
        var path = Path.Combine(Path.GetTempPath(), $"ae-readiness-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
