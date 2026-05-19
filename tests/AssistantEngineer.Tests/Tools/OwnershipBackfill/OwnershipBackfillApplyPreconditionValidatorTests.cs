using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyPreconditionValidatorTests
{
    [Fact]
    public void MissingEvidenceDirectory_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions());

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_EVIDENCE_REQUIRED");
    }

    [Fact]
    public void MissingGateResult_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_GATE_RESULT_REQUIRED");
    }

    [Fact]
    public void MissingOutput_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_OUTPUT_REQUIRED");
    }

    [Fact]
    public void MissingProvider_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y",
            OutputDirectory = "z"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_DATABASE_PROVIDER_REQUIRED");
    }

    [Fact]
    public void MissingConnectionString_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y",
            OutputDirectory = "z",
            DatabaseProvider = "SQLite"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_CONNECTION_STRING_REQUIRED");
    }

    [Fact]
    public void MissingEnableApply_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y",
            OutputDirectory = "z",
            DatabaseProvider = "SQLite",
            ConnectionString = "Data Source=fake.db"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_ENABLE_FLAG_REQUIRED");
    }

    [Fact]
    public void MissingConfirmation_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y",
            OutputDirectory = "z",
            DatabaseProvider = "SQLite",
            ConnectionString = "Data Source=fake.db",
            EnableApply = true
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_CONFIRMATION_INVALID");
    }

    [Fact]
    public void WrongConfirmation_Fails()
    {
        var validator = new OwnershipBackfillApplyPreconditionValidator();

        var result = validator.Validate(new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = "x",
            GateResultPath = "y",
            OutputDirectory = "z",
            DatabaseProvider = "SQLite",
            ConnectionString = "Data Source=fake.db",
            EnableApply = true,
            ConfirmationPhrase = "WRONG"
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "APPLY_CONFIRMATION_INVALID");
    }

    [Fact]
    public async Task GateResultPassedFalse_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDir = Path.Combine(root, "evidence");
            Directory.CreateDirectory(evidenceDir);
            await CreateSummaryAsync(evidenceDir, "run-1");

            var gatePath = Path.Combine(root, "gate.json");
            await File.WriteAllTextAsync(gatePath, "{\"Passed\":false}");

            var validator = new OwnershipBackfillApplyPreconditionValidator();
            var result = validator.Validate(new OwnershipBackfillApplyOptions
            {
                EvidenceDirectory = evidenceDir,
                GateResultPath = gatePath,
                OutputDirectory = Path.Combine(root, "out"),
                DatabaseProvider = "SQLite",
                ConnectionString = "Data Source=fake.db",
                EnableApply = true,
                ConfirmationPhrase = OwnershipBackfillConstants.ApplyConfirmationPhrase,
                BatchSize = 500
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "APPLY_GATE_RESULT_NOT_PASSED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidLookingInputs_PassValidator()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDir = Path.Combine(root, "evidence");
            Directory.CreateDirectory(evidenceDir);
            await CreateSummaryAsync(evidenceDir, "run-2");

            var gatePath = Path.Combine(root, "gate.json");
            await File.WriteAllTextAsync(gatePath, "{\"Passed\":true}");
            var planPath = await CreatePlanAsync(root, "hash-1");
            var signoffPath = await CreateSignoffAsync(root, "hash-1");

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

            Assert.True(result.Passed);
        }
        finally
        {
            DeleteDirectory(root);
        }
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

    private static async Task<string> CreatePlanAsync(string root, string planHash)
    {
        var path = Path.Combine(root, "plan.json");
        var plan = new
        {
            Succeeded = true,
            RunId = "run-1",
            PlanId = "plan-1",
            PlanHash = planHash,
            RulesetVersion = "P6-05",
            PlannedRecords = new[]
            {
                new
                {
                    RecordType = "Project",
                    RecordId = "11",
                    CurrentProjectId = 11,
                    CurrentBuildingId = (int?)null,
                    CurrentOrganizationId = (int?)null,
                    CurrentOwnerUserId = (int?)null,
                    ProposedProjectId = 11,
                    ProposedBuildingId = (int?)null,
                    ProposedOrganizationId = 77,
                    ProposedOwnerUserId = (int?)null,
                    Reason = "ProjectOrganizationMissing",
                    SourceEvidence = "ownership-backfill-unresolved-records-run-1.json",
                    DeterministicRecordHash = "record-hash-1"
                }
            },
            SummaryDraft = new
            {
                PlanId = "plan-1",
                PlanHash = planHash,
                Mode = "PlanOnly",
                TotalRecordsPlanned = 1,
                TotalRecordsSkipped = 0,
                TotalRecordsUnresolved = 1,
                PlannedByRecordType = new Dictionary<string, int>(StringComparer.Ordinal) { ["Project"] = 1 },
                SkippedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
                RequiredFutureApplyPreconditions = new[] { "signed plan required" },
                NonClaims = OwnershipBackfillConstants.NonClaims
            },
            Findings = Array.Empty<object>(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(plan));
        return path;
    }

    private static async Task<string> CreateSignoffAsync(string root, string planHash)
    {
        var path = Path.Combine(root, "signoff.json");
        var signoff = new
        {
            SignoffId = "20260518101010-abc123def456",
            PlanId = "plan-1",
            PlanHash = planHash,
            PlanPath = Path.Combine(root, "plan.json"),
            Reviewer = "reviewer-1",
            Ticket = "T-1",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            ToolStage = "P6-06",
            Notes = (string?)null,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(signoff));
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-apply-preconditions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
