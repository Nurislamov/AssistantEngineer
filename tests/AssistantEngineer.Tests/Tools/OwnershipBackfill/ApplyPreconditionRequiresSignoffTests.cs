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

public sealed class ApplyPreconditionRequiresSignoffTests
{
    [Fact]
    public async Task FailsWithoutPlan()
    {
        var root = CreateTempDirectory();

        try
        {
            var (evidenceDir, gatePath) = await CreateEvidenceAndGateAsync(root);

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var result = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                PlanSignoffPath = Path.Combine(root, "signoff.json"),
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "APPLY_PLAN_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailsWithoutSignoff()
    {
        var root = CreateTempDirectory();

        try
        {
            var (evidenceDir, gatePath) = await CreateEvidenceAndGateAsync(root);
            var planPath = await CreatePlanAsync(root, "hash-1");

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var result = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                PlanPath = planPath,
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "APPLY_PLAN_SIGNOFF_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailsWhenSignoffHashMismatchesPlanHash()
    {
        var root = CreateTempDirectory();

        try
        {
            var (evidenceDir, gatePath) = await CreateEvidenceAndGateAsync(root);
            var planPath = await CreatePlanAsync(root, "hash-plan");
            var signoffPath = await CreateSignoffAsync(root, "hash-other", DateTimeOffset.UtcNow.AddHours(1));

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var result = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                PlanPath = planPath,
                PlanSignoffPath = signoffPath,
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "APPLY_PLAN_SIGNOFF_HASH_MISMATCH");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailsWhenSignoffExpired()
    {
        var root = CreateTempDirectory();

        try
        {
            var (evidenceDir, gatePath) = await CreateEvidenceAndGateAsync(root);
            var planPath = await CreatePlanAsync(root, "hash-plan");
            var signoffPath = await CreateSignoffAsync(root, "hash-plan", DateTimeOffset.UtcNow.AddMinutes(-1));

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var result = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                PlanPath = planPath,
                PlanSignoffPath = signoffPath,
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "APPLY_PLAN_SIGNOFF_EXPIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MatchingPlanAndSignoff_PassesValidator_ButApplyRemainsDisabled()
    {
        var root = CreateTempDirectory();

        try
        {
            var (evidenceDir, gatePath) = await CreateEvidenceAndGateAsync(root);
            var planPath = await CreatePlanAsync(root, "hash-plan");
            var signoffPath = await CreateSignoffAsync(root, "hash-plan", DateTimeOffset.UtcNow.AddHours(1));

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var validation = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                PlanPath = planPath,
                PlanSignoffPath = signoffPath,
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.True(validation.Passed);

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

    private static async Task<(string EvidenceDir, string GatePath)> CreateEvidenceAndGateAsync(string root)
    {
        var evidenceDir = Path.Combine(root, "evidence");
        Directory.CreateDirectory(evidenceDir);

        var summary = new
        {
            RunId = "run-1",
            Mode = "DryRun",
            TotalRecordsScanned = 0,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(),
            RecordTypeMetrics = Array.Empty<object>(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(
            Path.Combine(evidenceDir, "ownership-backfill-dry-run-summary-run-1.json"),
            JsonSerializer.Serialize(summary));

        var gatePath = Path.Combine(root, "gate.json");
        await File.WriteAllTextAsync(gatePath, "{\"Passed\":true}");
        return (evidenceDir, gatePath);
    }

    private static async Task<string> CreatePlanAsync(string root, string planHash)
    {
        var path = Path.Combine(root, "plan.json");
        var plan = new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = "run-1",
            PlanId = "plan-1",
            PlanHash = planHash,
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
                    SourceEvidence = "ownership-backfill-unresolved-records-run-1.json",
                    DeterministicRecordHash = "record-hash-1"
                }
            ],
            SummaryDraft = new OwnershipBackfillApplySummaryDraft
            {
                PlanId = "plan-1",
                PlanHash = planHash,
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

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(plan));
        return path;
    }

    private static async Task<string> CreateSignoffAsync(string root, string planHash, DateTimeOffset expiresAtUtc)
    {
        var path = Path.Combine(root, "signoff.json");
        var signoff = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "20260518121212-abcdef123456",
            PlanId = "plan-1",
            PlanHash = planHash,
            PlanPath = Path.Combine(root, "plan.json"),
            Reviewer = "local-review",
            Ticket = "local-test",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            ToolStage = "P6-06",
            Notes = null,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(signoff));
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-apply-signoff-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
