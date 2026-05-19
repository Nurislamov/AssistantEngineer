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

public sealed class OwnershipBackfillPlanSignoffCliTests
{
    [Fact]
    public async Task SignoffPlan_WithValidInput_ExitsZeroAndWritesArtifacts()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            var output = Path.Combine(root, "signoff");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "signoff-plan",
                "--plan", planPath,
                "--expected-plan-hash", plan.PlanHash,
                "--reviewer", "local-review",
                "--ticket", "local-test",
                "--output", output,
                "--confirm", OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-plan-signoff-*.json"));
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-plan-signoff-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SignoffPlan_HashMismatch_ExitsTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "signoff-plan",
                "--plan", planPath,
                "--expected-plan-hash", "wrong",
                "--reviewer", "local-review",
                "--ticket", "local-test",
                "--output", Path.Combine(root, "signoff"),
                "--confirm", OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
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
    public async Task SignoffPlan_MissingConfirmation_ExitsOne()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "signoff-plan",
                "--plan", planPath,
                "--expected-plan-hash", plan.PlanHash,
                "--reviewer", "local-review",
                "--ticket", "local-test",
                "--output", Path.Combine(root, "signoff")
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.Contains("SIGNOFF_CONFIRMATION_INVALID", stderr.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SignoffPlan_WrongConfirmation_ExitsOne()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "signoff-plan",
                "--plan", planPath,
                "--expected-plan-hash", plan.PlanHash,
                "--reviewer", "local-review",
                "--ticket", "local-test",
                "--output", Path.Combine(root, "signoff"),
                "--confirm", "WRONG"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SignoffPlan_DoesNotEchoSecret()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        const string fakeSecret = "Password=TOP-SECRET-SIGNOFF";

        var exitCode = await cli.ExecuteAsync(
            ["signoff-plan", "--plan", "x", "--expected-plan-hash", "y", "--reviewer", "r", "--ticket", "t", "--output", "o", "--confirm", OwnershipBackfillConstants.PlanSignoffConfirmationPhrase, "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var all = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_RemainsDisabledEvenWithSignoff()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDir = Path.Combine(root, "evidence");
            Directory.CreateDirectory(evidenceDir);
            await CreateSummaryAsync(evidenceDir, "run-1");

            var gatePath = Path.Combine(root, "gate.json");
            await File.WriteAllTextAsync(gatePath, "{\"Passed\":true}");

            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var signoff = new OwnershipBackfillPlanSignoffArtifact
            {
                SignoffId = "20260518010101-abcd1234ef01",
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                PlanPath = planPath,
                Reviewer = "local-review",
                Ticket = "local-test",
                ConfirmationPhraseAccepted = true,
                SignedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                ToolStage = "P6-06",
                Notes = null,
                NonClaims = OwnershipBackfillConstants.NonClaims
            };

            var signoffPath = Path.Combine(root, "signoff.json");
            await File.WriteAllTextAsync(signoffPath, JsonSerializer.Serialize(signoff));

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "apply",
                "--evidence", evidenceDir,
                "--gate-result", gatePath,
                "--plan", planPath,
                "--plan-signoff", signoffPath,
                "--output", Path.Combine(root, "apply-out"),
                "--database-provider", "SQLite",
                "--connection-string", "Data Source=fake.db",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
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

    private static OwnershipBackfillPlanResult CreatePlanResult()
    {
        return new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "run-001",
            PlanId = "plan-001",
            PlanHash = "hash-001",
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
                    DeterministicRecordHash = "record-hash-1"
                }
            ],
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
                    "validate-evidence gate result Passed=true",
                    "signed plan required"
                ],
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static async Task CreateSummaryAsync(string evidenceDirectory, string runId)
    {
        var summary = new
        {
            RunId = runId,
            Mode = "DryRun",
            TotalRecordsScanned = 0,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(),
            RecordTypeMetrics = Array.Empty<object>(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(
            Path.Combine(evidenceDirectory, $"ownership-backfill-dry-run-summary-{runId}.json"),
            JsonSerializer.Serialize(summary));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-signoff-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
