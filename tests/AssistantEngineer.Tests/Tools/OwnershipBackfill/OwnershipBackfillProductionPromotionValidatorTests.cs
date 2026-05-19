using System.Text.Json;
using System.Text.Json.Nodes;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Production;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillProductionPromotionValidatorTests
{
    [Fact]
    public async Task CompleteValidChain_ReturnsReadyForProductionApproval()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(0, validation.ExitCode);
            Assert.True(validation.Decision.Ready);
            Assert.Equal("ReadyForProductionApproval", validation.Decision.DecisionStatus);
            Assert.False(string.IsNullOrWhiteSpace(validation.Decision.ProductionPromotionHash));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingStagingAcceptance_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillProductionPromotionValidator();
            var options = CreateOptions(root, artifacts, stagingAcceptancePath: Path.Combine(root, "missing.json"));

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.False(validation.Decision.Ready);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StagingAcceptanceNotAccepted_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, stagingAccepted: false);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_STAGING_ACCEPTANCE_NOT_ACCEPTED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ExpiredStagingAcceptance_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var node = JsonNode.Parse(await File.ReadAllTextAsync(artifacts.StagingAcceptancePath))!.AsObject();
            node["ExpiresAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O");
            await File.WriteAllTextAsync(artifacts.StagingAcceptancePath, node.ToJsonString());

            var validator = new OwnershipBackfillProductionPromotionValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionGateFailed_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, productionGatePassed: false);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_PRODUCTION_GATE_FAILED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionPlanNotPlanOnly_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, planMode: "ApplyMode");
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_PRODUCTION_PLAN_MODE_INVALID");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionSignoffPlanHashMismatch_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, signoffPlanHash: "different-plan-hash");
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_PRODUCTION_SIGNOFF_PLANHASH_MISMATCH");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionReadinessFailed_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, readinessPassed: false);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_PRODUCTION_READINESS_FAILED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionPreviousValuesIncomplete_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, includePreviousValueSnapshot: false);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_PRODUCTION_PREVIOUS_VALUES_INCOMPLETE");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ProductionApplyInputHashSameAsStaging_IsRejectedByDefault()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, sameStagingAndProductionApplyHash: true);
            var validator = new OwnershipBackfillProductionPromotionValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_CROSS_ENV_APPLY_INPUT_HASH_REUSE");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingProductionChangeRequestId_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillProductionPromotionValidator();
            var options = CreateOptions(root, artifacts, productionChangeRequestId: " ");

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_CHANGE_REQUEST_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PayloadOrSecretLikeFields_AreRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var node = JsonNode.Parse(await File.ReadAllTextAsync(artifacts.ProductionPlanPath))!.AsObject();
            node["payloadData"] = new JsonObject { ["x"] = 1 };
            await File.WriteAllTextAsync(artifacts.ProductionPlanPath, node.ToJsonString());

            var validator = new OwnershipBackfillProductionPromotionValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_FORBIDDEN_FIELD");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task EvidenceClaimingProductionApplyExecuted_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var node = JsonNode.Parse(await File.ReadAllTextAsync(artifacts.ProductionPlanPath))!.AsObject();
            node["applyExecuted"] = true;
            await File.WriteAllTextAsync(artifacts.ProductionPlanPath, node.ToJsonString());

            var validator = new OwnershipBackfillProductionPromotionValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Decision.Findings, finding => finding.Code == "PROMOTION_ARTIFACT_CLAIMS_EXECUTION");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillProductionPromotionOptions CreateOptions(
        string root,
        ArtifactPaths artifacts,
        string? stagingAcceptancePath = null,
        string? productionChangeRequestId = "chg-prod-001")
    {
        return new OwnershipBackfillProductionPromotionOptions
        {
            StagingAcceptancePath = stagingAcceptancePath ?? artifacts.StagingAcceptancePath,
            ProductionDryRunSummaryPath = artifacts.ProductionDryRunPath,
            ProductionGateResultPath = artifacts.ProductionGatePath,
            ProductionPlanPath = artifacts.ProductionPlanPath,
            ProductionSignoffPath = artifacts.ProductionSignoffPath,
            ProductionReadinessPath = artifacts.ProductionReadinessPath,
            ProductionPreviousValuesPath = artifacts.ProductionPreviousValuesPath,
            ProductionChangeRequestId = productionChangeRequestId,
            OutputDirectory = Path.Combine(root, "promotion-out"),
            RulesetVersion = "P6-13",
            MaxStagingAcceptanceAgeHours = 72,
            MaxProductionSignoffAgeHours = 24,
            RequireSeparateProductionEvidence = true,
            RequireBackupReference = true,
            RequireRollbackReadiness = true
        };
    }

    private static async Task<ArtifactPaths> WriteValidArtifactsAsync(
        string root,
        bool stagingAccepted = true,
        bool productionGatePassed = true,
        string planMode = "PlanOnly",
        string signoffPlanHash = "prod-plan-hash-001",
        bool readinessPassed = true,
        bool includePreviousValueSnapshot = true,
        bool sameStagingAndProductionApplyHash = false)
    {
        var stagingApplyHash = "staging-apply-hash-001";
        var productionApplyHash = sameStagingAndProductionApplyHash ? stagingApplyHash : "production-apply-hash-001";

        var stagingAcceptance = new OwnershipBackfillStagingAcceptanceResult
        {
            Accepted = stagingAccepted,
            AcceptanceId = "acceptance-001",
            StagingRunHash = "staging-run-hash-001",
            ApplyInputHash = stagingApplyHash,
            PlanHash = "staging-plan-hash-001",
            SignoffId = "staging-signoff-001",
            ReadinessId = "staging-readiness-001",
            OperatorId = "staging-operator-001",
            StagingChangeId = "staging-change-001",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RollbackReference"] = "rollback-staging-001"
            },
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No staging apply execution claim.",
                "No production apply enabled claim.",
                "No ownership backfill execution claim."
            ]
        };

        var productionDryRun = new OwnershipBackfillDryRunSummary
        {
            RunId = "prod-dry-001",
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-9),
            Mode = "DryRun",
            TotalRecordsScanned = 1,
            TotalRecordsResolvable = 1,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(CreateZeroMetric)
                .ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var productionGate = new OwnershipBackfillGateResult
        {
            Passed = productionGatePassed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = productionGatePassed ? "gate-pass" : "gate-failed",
            RunId = "prod-gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var planRecord = new OwnershipBackfillPlannedRecord
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
            SourceEvidence = "ownership-backfill-unresolved-records-prod-dry-001.json",
            DeterministicRecordHash = "det-hash-001"
        };

        var productionPlan = new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "plan-run-001",
            PlanId = "plan-001",
            PlanHash = "prod-plan-hash-001",
            RulesetVersion = "P6-05",
            PlannedRecords = [planRecord],
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = "plan-001",
                PlanHash = "prod-plan-hash-001",
                Mode = planMode,
                TotalRecordsPlanned = 1,
                TotalRecordsSkipped = 0,
                TotalRecordsUnresolved = 0,
                PlannedByRecordType = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["Project"] = 1
                },
                SkippedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RequiredFutureApplyPreconditions = ["Signed plan required"],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var productionSignoff = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "prod-signoff-001",
            PlanId = "plan-001",
            PlanHash = signoffPlanHash,
            PlanPath = "ownership-backfill-apply-plan-plan-001.json",
            Reviewer = "prod-reviewer-001",
            Ticket = "CHG-PROD-001",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            ToolStage = "P6-06",
            Notes = null,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var productionReadiness = new OwnershipBackfillApplyReadinessResult
        {
            Passed = readinessPassed,
            ReadinessId = "prod-readiness-001",
            ApplyInputHash = productionApplyHash,
            PlanHash = "prod-plan-hash-001",
            SignoffPlanHash = "prod-plan-hash-001",
            RulesetVersion = "P6-08",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["BackupReference"] = "backup-prod-001",
                ["RollbackReadinessReference"] = "rollback-prod-001"
            },
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var previousValues = includePreviousValueSnapshot
            ? new List<OwnershipBackfillPreviousValueSnapshot>
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
            }
            : new List<OwnershipBackfillPreviousValueSnapshot>();

        var stagingAcceptancePath = Path.Combine(root, "staging-acceptance.json");
        var productionDryRunPath = Path.Combine(root, "production-dry-run.json");
        var productionGatePath = Path.Combine(root, "production-gate.json");
        var productionPlanPath = Path.Combine(root, "production-plan.json");
        var productionSignoffPath = Path.Combine(root, "production-signoff.json");
        var productionReadinessPath = Path.Combine(root, "production-readiness.json");
        var productionPreviousValuesPath = Path.Combine(root, "production-previous-values.json");

        await File.WriteAllTextAsync(stagingAcceptancePath, JsonSerializer.Serialize(stagingAcceptance));
        await File.WriteAllTextAsync(productionDryRunPath, JsonSerializer.Serialize(productionDryRun));
        await File.WriteAllTextAsync(productionGatePath, JsonSerializer.Serialize(productionGate));
        await File.WriteAllTextAsync(productionPlanPath, JsonSerializer.Serialize(productionPlan));
        await File.WriteAllTextAsync(productionSignoffPath, JsonSerializer.Serialize(productionSignoff));
        await File.WriteAllTextAsync(productionReadinessPath, JsonSerializer.Serialize(productionReadiness));
        await File.WriteAllTextAsync(productionPreviousValuesPath, JsonSerializer.Serialize(previousValues));

        var stagingNode = JsonNode.Parse(await File.ReadAllTextAsync(stagingAcceptancePath))!.AsObject();
        stagingNode["SignedAtUtc"] = DateTimeOffset.UtcNow.AddHours(-2).ToString("O");
        await File.WriteAllTextAsync(stagingAcceptancePath, stagingNode.ToJsonString());

        return new ArtifactPaths(
            stagingAcceptancePath,
            productionDryRunPath,
            productionGatePath,
            productionPlanPath,
            productionSignoffPath,
            productionReadinessPath,
            productionPreviousValuesPath);
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
        var path = Path.Combine(Path.GetTempPath(), $"ae-production-promotion-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed record ArtifactPaths(
        string StagingAcceptancePath,
        string ProductionDryRunPath,
        string ProductionGatePath,
        string ProductionPlanPath,
        string ProductionSignoffPath,
        string ProductionReadinessPath,
        string ProductionPreviousValuesPath);
}
