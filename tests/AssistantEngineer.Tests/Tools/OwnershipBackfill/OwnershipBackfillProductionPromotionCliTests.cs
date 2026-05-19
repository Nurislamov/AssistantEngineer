using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillProductionPromotionCliTests
{
    [Fact]
    public async Task ValidateProductionPromotion_ValidChain_ExitsZero()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteArtifactsAsync(root, gatePassed: true, readinessPassed: true);
            var output = Path.Combine(root, "promotion-out");
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-production-promotion",
                "--staging-acceptance", artifacts.StagingAcceptancePath,
                "--production-dry-run", artifacts.ProductionDryRunPath,
                "--production-gate-result", artifacts.ProductionGatePath,
                "--production-plan", artifacts.ProductionPlanPath,
                "--production-signoff", artifacts.ProductionSignoffPath,
                "--production-readiness", artifacts.ProductionReadinessPath,
                "--production-previous-values", artifacts.ProductionPreviousValuesPath,
                "--production-change-request-id", "CHG-PROD-001",
                "--output", output
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-production-promotion-decision-*.json"));
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-production-promotion-decision-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateProductionPromotion_NotReady_ExitsTwo()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteArtifactsAsync(root, gatePassed: false, readinessPassed: true);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-production-promotion",
                "--staging-acceptance", artifacts.StagingAcceptancePath,
                "--production-dry-run", artifacts.ProductionDryRunPath,
                "--production-gate-result", artifacts.ProductionGatePath,
                "--production-plan", artifacts.ProductionPlanPath,
                "--production-signoff", artifacts.ProductionSignoffPath,
                "--production-readiness", artifacts.ProductionReadinessPath,
                "--production-previous-values", artifacts.ProductionPreviousValuesPath,
                "--production-change-request-id", "CHG-PROD-001",
                "--output", Path.Combine(root, "promotion-out")
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(2, exitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateProductionPromotion_InvalidInput_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["validate-production-promotion", "--staging-acceptance", "x"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--production-dry-run", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateProductionPromotion_DoesNotEchoSecrets()
    {
        const string fakeSecret = "Server=x;Password=TOP-SECRET;";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-production-promotion", "--staging-acceptance", "x", "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var output = stdout + Environment.NewLine + stderr;
        Assert.DoesNotContain(fakeSecret, output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_RemainsDisabled()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static OwnershipBackfillCli CreateCli()
    {
        return new OwnershipBackfillCli(
            new OwnershipBackfillCommandLineParser(),
            new OwnershipBackfillEvidenceWriter(),
            new NoDataOwnershipBackfillDryRunScanner(),
            new DatabaseOwnershipBackfillDryRunScanner(),
            new OwnershipBackfillEvidenceLoader(),
            new OwnershipBackfillEvidenceGateEvaluator(),
            new OwnershipBackfillGateResultWriter(),
            new OwnershipBackfillApplyPreconditionValidator(),
            new OwnershipBackfillApplyPlanGenerator(),
            new OwnershipBackfillApplyPlanWriter(),
            new OwnershipBackfillPlanSignoffValidator(),
            new OwnershipBackfillPlanSignoffWriter());
    }

    private static async Task<ArtifactPaths> WriteArtifactsAsync(string root, bool gatePassed, bool readinessPassed)
    {
        var staging = new OwnershipBackfillStagingAcceptanceResult
        {
            Accepted = true,
            AcceptanceId = "acceptance-001",
            StagingRunHash = "staging-run-hash-001",
            ApplyInputHash = "staging-apply-hash-001",
            PlanHash = "staging-plan-hash-001",
            SignoffId = "staging-signoff-001",
            ReadinessId = "staging-readiness-001",
            OperatorId = "staging-operator-001",
            StagingChangeId = "staging-change-001",
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No staging apply execution claim.",
                "No production apply enabled claim.",
                "No ownership backfill execution claim."
            ]
        };

        var dryRun = new OwnershipBackfillDryRunSummary
        {
            RunId = "prod-dry-001",
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4),
            Mode = "DryRun",
            TotalRecordsScanned = 1,
            TotalRecordsResolvable = 1,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(recordType => new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = recordType,
                    TotalRecords = 0,
                    ResolvableRecords = 0,
                    UnresolvedRecords = 0,
                    AmbiguousRecords = 0,
                    ResolvableRate = 0d,
                    UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
                })
                .ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var gate = new OwnershipBackfillGateResult
        {
            Passed = gatePassed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = gatePassed ? "gate pass" : "gate failed",
            RunId = "prod-gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var plan = new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "plan-run-001",
            PlanId = "plan-001",
            PlanHash = "prod-plan-hash-001",
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
                    SourceEvidence = "ownership-backfill-unresolved-records-prod-dry-001.json",
                    DeterministicRecordHash = "det-hash-001"
                }
            ],
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = "plan-001",
                PlanHash = "prod-plan-hash-001",
                Mode = "PlanOnly",
                TotalRecordsPlanned = 1,
                TotalRecordsSkipped = 0,
                TotalRecordsUnresolved = 0,
                PlannedByRecordType = new Dictionary<string, int>(StringComparer.Ordinal) { ["Project"] = 1 },
                SkippedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RequiredFutureApplyPreconditions = ["Signed plan required"],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var signoff = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "prod-signoff-001",
            PlanId = "plan-001",
            PlanHash = "prod-plan-hash-001",
            PlanPath = "ownership-backfill-apply-plan-plan-001.json",
            Reviewer = "reviewer-001",
            Ticket = "CHG-PROD-001",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            ToolStage = "P6-06",
            Notes = null,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var readiness = new OwnershipBackfillApplyReadinessResult
        {
            Passed = readinessPassed,
            ReadinessId = "prod-readiness-001",
            ApplyInputHash = "production-apply-hash-001",
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

        var previousValues = new List<OwnershipBackfillPreviousValueSnapshot>
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

        var stagingPath = Path.Combine(root, "staging-acceptance.json");
        var dryRunPath = Path.Combine(root, "production-dry-run.json");
        var gatePath = Path.Combine(root, "production-gate.json");
        var planPath = Path.Combine(root, "production-plan.json");
        var signoffPath = Path.Combine(root, "production-signoff.json");
        var readinessPath = Path.Combine(root, "production-readiness.json");
        var previousValuesPath = Path.Combine(root, "production-previous-values.json");

        await File.WriteAllTextAsync(stagingPath, JsonSerializer.Serialize(staging));
        await File.WriteAllTextAsync(dryRunPath, JsonSerializer.Serialize(dryRun));
        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gate));
        await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));
        await File.WriteAllTextAsync(signoffPath, JsonSerializer.Serialize(signoff));
        await File.WriteAllTextAsync(readinessPath, JsonSerializer.Serialize(readiness));
        await File.WriteAllTextAsync(previousValuesPath, JsonSerializer.Serialize(previousValues));

        return new ArtifactPaths(stagingPath, dryRunPath, gatePath, planPath, signoffPath, readinessPath, previousValuesPath);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-production-promotion-cli-{Guid.NewGuid():N}");
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
