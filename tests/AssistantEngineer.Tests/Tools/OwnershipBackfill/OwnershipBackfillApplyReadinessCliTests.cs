using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyReadinessCliTests
{
    [Fact]
    public async Task ValidateApplyReadiness_WithValidArtifacts_ExitsZero()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidArtifactsAsync(root, gatePassed: true);
            var output = Path.Combine(root, "readiness-out");
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-apply-readiness",
                "--dry-run", paths.DryRunPath,
                "--gate-result", paths.GatePath,
                "--plan", paths.PlanPath,
                "--signoff", paths.SignoffPath,
                "--previous-values", paths.PreviousValuesPath,
                "--output", output
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-apply-readiness-result-*.json"));
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-apply-readiness-result-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateApplyReadiness_FailedGate_ExitsTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidArtifactsAsync(root, gatePassed: false);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-apply-readiness",
                "--dry-run", paths.DryRunPath,
                "--gate-result", paths.GatePath,
                "--plan", paths.PlanPath,
                "--signoff", paths.SignoffPath,
                "--previous-values", paths.PreviousValuesPath,
                "--output", Path.Combine(root, "readiness-out")
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
    public async Task ValidateApplyReadiness_PlanHashMismatch_ExitsTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var paths = await WriteValidArtifactsAsync(root, gatePassed: true);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-apply-readiness",
                "--dry-run", paths.DryRunPath,
                "--gate-result", paths.GatePath,
                "--plan", paths.PlanPath,
                "--signoff", paths.SignoffPath,
                "--previous-values", paths.PreviousValuesPath,
                "--expected-plan-hash", "not-plan-hash",
                "--output", Path.Combine(root, "readiness-out")
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
    public async Task ValidateApplyReadiness_InvalidInput_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["validate-apply-readiness", "--dry-run", "x"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--gate-result", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateApplyReadiness_DoesNotEchoSecrets()
    {
        const string fakeSecret = "TOP-SECRET-VALUE";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-apply-readiness", "--dry-run", "x", "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var all = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_StillDisabled()
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

    private static async Task<(string DryRunPath, string GatePath, string PlanPath, string SignoffPath, string PreviousValuesPath)> WriteValidArtifactsAsync(
        string root,
        bool gatePassed)
    {
        var dryRun = new OwnershipBackfillDryRunSummary
        {
            RunId = "run-001",
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
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

        var gate = new OwnershipBackfillGateResult
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
                RequiredFutureApplyPreconditions = ["signed plan required"],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var signoff = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "signoff-001",
            PlanId = "plan-001",
            PlanHash = "plan-hash-001",
            PlanPath = "ownership-backfill-apply-plan-plan-001.json",
            Reviewer = "reviewer-1",
            Ticket = "CHG-100",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            ToolStage = "P6-06",
            Notes = null,
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

        var dryRunPath = Path.Combine(root, "dry-run-summary.json");
        var gatePath = Path.Combine(root, "gate-result.json");
        var planPath = Path.Combine(root, "plan.json");
        var signoffPath = Path.Combine(root, "signoff.json");
        var previousValuesPath = Path.Combine(root, "previous-values.json");

        await File.WriteAllTextAsync(dryRunPath, JsonSerializer.Serialize(dryRun));
        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gate));
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
        var path = Path.Combine(Path.GetTempPath(), $"ae-readiness-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}

