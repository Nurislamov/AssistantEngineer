using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillPlanSignoffValidatorTests
{
    [Fact]
    public async Task ValidInputs_PassValidation()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer-1",
                Ticket = "CHG-101",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.True(result.Passed);
            Assert.Equal(0, result.ExitCode);
            Assert.NotNull(result.Artifact);
            Assert.True(result.Artifact.ConfirmationPhraseAccepted);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void MissingPlan_Fails()
    {
        var validator = new OwnershipBackfillPlanSignoffValidator();

        var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
        {
            ExpectedPlanHash = "hash",
            Reviewer = "reviewer",
            Ticket = "T-1",
            OutputDirectory = "out",
            ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
        });

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_PLAN_REQUIRED");
    }

    [Fact]
    public async Task InvalidPlanJson_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, "{bad json");

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = "hash",
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_PLAN_JSON_INVALID");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task HashMismatch_FailsWithExitCodeTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = "different",
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Equal(2, result.ExitCode);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_PLAN_HASH_MISMATCH");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingReviewer_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_REVIEWER_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingTicket_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_TICKET_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WrongConfirmation_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = "WRONG"
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_CONFIRMATION_INVALID");
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
            var plan = CreatePlanResult();
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_EXPIRES_AT_PAST");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PlanWithoutNonClaims_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            plan = new OwnershipBackfillPlanResult
            {
                Succeeded = plan.Succeeded,
                RunId = plan.RunId,
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                RulesetVersion = plan.RulesetVersion,
                PlannedRecords = plan.PlannedRecords,
                SummaryDraft = plan.SummaryDraft,
                Findings = plan.Findings,
                NonClaims = []
            };
            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan));

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_PLAN_NON_CLAIMS_MISSING");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PlanWithPayloadField_Fails()
    {
        var root = CreateTempDirectory();

        try
        {
            var plan = CreatePlanResult();
            var planJson = JsonSerializer.Serialize(plan);
            var raw = planJson.Replace("\"RecordId\":\"11\"", "\"RecordId\":\"11\",\"PayloadJson\":\"x\"");

            var planPath = Path.Combine(root, "plan.json");
            await File.WriteAllTextAsync(planPath, raw);

            var validator = new OwnershipBackfillPlanSignoffValidator();
            var result = validator.Validate(new OwnershipBackfillPlanSignoffOptions
            {
                PlanPath = planPath,
                ExpectedPlanHash = plan.PlanHash,
                Reviewer = "reviewer",
                Ticket = "T-1",
                OutputDirectory = Path.Combine(root, "out"),
                ConfirmationPhrase = OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            });

            Assert.False(result.Passed);
            Assert.Contains(result.Findings, finding => finding.Code == "SIGNOFF_PLAN_FORBIDDEN_FIELDS");
        }
        finally
        {
            DeleteDirectory(root);
        }
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-signoff-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
