using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyPreconditionValidator
{
    public OwnershipBackfillApplyPreconditionResult Validate(OwnershipBackfillApplyOptions options)
    {
        var findings = new List<OwnershipBackfillApplyPreconditionFinding>();

        if (string.IsNullOrWhiteSpace(options.EvidenceDirectory))
        {
            Add(findings, "APPLY_EVIDENCE_REQUIRED", "Blocking", "--evidence is required.");
        }
        else if (!Directory.Exists(Path.GetFullPath(options.EvidenceDirectory)))
        {
            Add(findings, "APPLY_EVIDENCE_NOT_FOUND", "Blocking", "Evidence directory was not found.");
        }
        else
        {
            var evidenceDir = Path.GetFullPath(options.EvidenceDirectory);
            var summaryExists = Directory.GetFiles(evidenceDir, "ownership-backfill-dry-run-summary-*.json", SearchOption.TopDirectoryOnly).Length > 0;
            if (!summaryExists)
                Add(findings, "APPLY_DRY_RUN_SUMMARY_MISSING", "Blocking", "Dry-run summary evidence is missing from --evidence directory.");
        }

        if (string.IsNullOrWhiteSpace(options.GateResultPath))
        {
            Add(findings, "APPLY_GATE_RESULT_REQUIRED", "Blocking", "--gate-result is required.");
        }
        else
        {
            var gatePath = Path.GetFullPath(options.GateResultPath);
            if (!File.Exists(gatePath))
            {
                Add(findings, "APPLY_GATE_RESULT_NOT_FOUND", "Blocking", "Gate result JSON file was not found.");
            }
            else
            {
                ValidateGateResultFile(gatePath, findings);
            }
        }

        var plan = ValidatePlanFile(options.PlanPath, findings);
        var signoff = ValidateSignoffFile(options.PlanSignoffPath, findings);

        if (plan is not null && signoff is not null)
            ValidatePlanSignoffMatch(plan, signoff, findings);

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            Add(findings, "APPLY_OUTPUT_REQUIRED", "Blocking", "--output is required.");

        if (string.IsNullOrWhiteSpace(options.DatabaseProvider))
            Add(findings, "APPLY_DATABASE_PROVIDER_REQUIRED", "Blocking", "--database-provider is required.");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            Add(findings, "APPLY_CONNECTION_STRING_REQUIRED", "Blocking", "--connection-string is required.");

        if (!options.EnableApply)
            Add(findings, "APPLY_ENABLE_FLAG_REQUIRED", "Blocking", "--enable-apply must be supplied.");

        if (!string.Equals(options.ConfirmationPhrase, OwnershipBackfillConstants.ApplyConfirmationPhrase, StringComparison.Ordinal))
        {
            Add(findings, "APPLY_CONFIRMATION_INVALID", "Blocking", $"--confirm must equal {OwnershipBackfillConstants.ApplyConfirmationPhrase}.");
        }

        if (options.BatchSize <= 0)
            Add(findings, "APPLY_BATCH_SIZE_INVALID", "Blocking", "--batch-size must be a positive integer.");

        return new OwnershipBackfillApplyPreconditionResult
        {
            Passed = findings.Count == 0,
            Findings = findings
        };
    }

    private static void ValidateGateResultFile(
        string gatePath,
        ICollection<OwnershipBackfillApplyPreconditionFinding> findings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(gatePath));
            if (!document.RootElement.TryGetProperty("Passed", out var passedProperty) &&
                !document.RootElement.TryGetProperty("passed", out passedProperty))
            {
                Add(findings, "APPLY_GATE_RESULT_FORMAT_INVALID", "Blocking", "Gate result does not contain Passed property.");
                return;
            }

            if (passedProperty.ValueKind != JsonValueKind.True)
                Add(findings, "APPLY_GATE_RESULT_NOT_PASSED", "Blocking", "Gate result must have Passed=true.");
        }
        catch (JsonException)
        {
            Add(findings, "APPLY_GATE_RESULT_JSON_INVALID", "Blocking", "Gate result JSON is invalid.");
        }
        catch (IOException)
        {
            Add(findings, "APPLY_GATE_RESULT_READ_FAILED", "Blocking", "Gate result file could not be read.");
        }
    }

    private static OwnershipBackfillPlanResult? ValidatePlanFile(
        string? planPath,
        ICollection<OwnershipBackfillApplyPreconditionFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(planPath))
        {
            Add(findings, "APPLY_PLAN_REQUIRED", "Blocking", "--plan is required.");
            return null;
        }

        var resolvedPath = Path.GetFullPath(planPath);
        if (!File.Exists(resolvedPath))
        {
            Add(findings, "APPLY_PLAN_NOT_FOUND", "Blocking", "Plan JSON file was not found.");
            return null;
        }

        try
        {
            var plan = JsonSerializer.Deserialize<OwnershipBackfillPlanResult>(File.ReadAllText(resolvedPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (plan is null)
            {
                Add(findings, "APPLY_PLAN_PARSE_FAILED", "Blocking", "Plan JSON could not be parsed.");
                return null;
            }

            if (!string.Equals(plan.SummaryDraft.Mode, "PlanOnly", StringComparison.Ordinal))
                Add(findings, "APPLY_PLAN_MODE_INVALID", "Blocking", "Plan summary mode must be PlanOnly.");

            return plan;
        }
        catch (JsonException)
        {
            Add(findings, "APPLY_PLAN_JSON_INVALID", "Blocking", "Plan JSON is invalid.");
            return null;
        }
        catch (IOException)
        {
            Add(findings, "APPLY_PLAN_READ_FAILED", "Blocking", "Plan JSON could not be read.");
            return null;
        }
    }

    private static OwnershipBackfillPlanSignoffArtifact? ValidateSignoffFile(
        string? signoffPath,
        ICollection<OwnershipBackfillApplyPreconditionFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(signoffPath))
        {
            Add(findings, "APPLY_PLAN_SIGNOFF_REQUIRED", "Blocking", "--plan-signoff is required.");
            return null;
        }

        var resolvedPath = Path.GetFullPath(signoffPath);
        if (!File.Exists(resolvedPath))
        {
            Add(findings, "APPLY_PLAN_SIGNOFF_NOT_FOUND", "Blocking", "Plan signoff JSON file was not found.");
            return null;
        }

        try
        {
            var signoff = JsonSerializer.Deserialize<OwnershipBackfillPlanSignoffArtifact>(File.ReadAllText(resolvedPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (signoff is null)
            {
                Add(findings, "APPLY_PLAN_SIGNOFF_PARSE_FAILED", "Blocking", "Plan signoff JSON could not be parsed.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(signoff.Reviewer))
                Add(findings, "APPLY_PLAN_SIGNOFF_REVIEWER_MISSING", "Blocking", "Plan signoff reviewer is required.");

            if (string.IsNullOrWhiteSpace(signoff.Ticket))
                Add(findings, "APPLY_PLAN_SIGNOFF_TICKET_MISSING", "Blocking", "Plan signoff ticket is required.");

            if (signoff.ExpiresAtUtc.HasValue && signoff.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
                Add(findings, "APPLY_PLAN_SIGNOFF_EXPIRED", "Blocking", "Plan signoff has expired.");

            return signoff;
        }
        catch (JsonException)
        {
            Add(findings, "APPLY_PLAN_SIGNOFF_JSON_INVALID", "Blocking", "Plan signoff JSON is invalid.");
            return null;
        }
        catch (IOException)
        {
            Add(findings, "APPLY_PLAN_SIGNOFF_READ_FAILED", "Blocking", "Plan signoff JSON could not be read.");
            return null;
        }
    }

    private static void ValidatePlanSignoffMatch(
        OwnershipBackfillPlanResult plan,
        OwnershipBackfillPlanSignoffArtifact signoff,
        ICollection<OwnershipBackfillApplyPreconditionFinding> findings)
    {
        if (!string.Equals(plan.PlanHash, signoff.PlanHash, StringComparison.OrdinalIgnoreCase))
            Add(findings, "APPLY_PLAN_SIGNOFF_HASH_MISMATCH", "Blocking", "Plan signoff hash must match plan hash.");

        if (!string.Equals(plan.PlanId, signoff.PlanId, StringComparison.Ordinal))
            Add(findings, "APPLY_PLAN_SIGNOFF_PLANID_MISMATCH", "Blocking", "Plan signoff plan id must match plan id.");

        if (!signoff.ConfirmationPhraseAccepted)
            Add(findings, "APPLY_PLAN_SIGNOFF_CONFIRMATION_NOT_ACCEPTED", "Blocking", "Plan signoff confirmation acceptance is required.");
    }

    private static void Add(
        ICollection<OwnershipBackfillApplyPreconditionFinding> findings,
        string code,
        string severity,
        string message)
    {
        findings.Add(new OwnershipBackfillApplyPreconditionFinding
        {
            Code = code,
            Severity = severity,
            Message = message
        });
    }
}
