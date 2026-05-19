using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;

namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public sealed class OwnershipBackfillPlanSignoffValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ForbiddenPlanPropertyFragments =
    [
        "payload",
        "secret",
        "token"
    ];

    public OwnershipBackfillPlanSignoffValidationResult Validate(OwnershipBackfillPlanSignoffOptions options)
    {
        var findings = new List<OwnershipBackfillPlanSignoffFinding>();

        if (string.IsNullOrWhiteSpace(options.PlanPath))
            Add(findings, "SIGNOFF_PLAN_REQUIRED", "Blocking", "--plan is required.");

        if (string.IsNullOrWhiteSpace(options.ExpectedPlanHash))
            Add(findings, "SIGNOFF_EXPECTED_PLAN_HASH_REQUIRED", "Blocking", "--expected-plan-hash is required.");

        if (string.IsNullOrWhiteSpace(options.Reviewer))
            Add(findings, "SIGNOFF_REVIEWER_REQUIRED", "Blocking", "--reviewer is required.");

        if (string.IsNullOrWhiteSpace(options.Ticket))
            Add(findings, "SIGNOFF_TICKET_REQUIRED", "Blocking", "--ticket is required.");

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            Add(findings, "SIGNOFF_OUTPUT_REQUIRED", "Blocking", "--output is required.");

        if (!string.Equals(options.ConfirmationPhrase, OwnershipBackfillConstants.PlanSignoffConfirmationPhrase, StringComparison.Ordinal))
        {
            Add(findings, "SIGNOFF_CONFIRMATION_INVALID", "Blocking", $"--confirm must equal {OwnershipBackfillConstants.PlanSignoffConfirmationPhrase}.");
        }

        if (options.ExpiresAtUtc.HasValue && options.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            Add(findings, "SIGNOFF_EXPIRES_AT_PAST", "Blocking", "--expires-at must be a future UTC timestamp.");

        if (findings.Count > 0)
        {
            return new OwnershipBackfillPlanSignoffValidationResult
            {
                Passed = false,
                ExitCode = 1,
                Findings = findings,
                Artifact = null
            };
        }

        var planPath = Path.GetFullPath(options.PlanPath!);
        if (!File.Exists(planPath))
        {
            Add(findings, "SIGNOFF_PLAN_NOT_FOUND", "Blocking", "Plan JSON file was not found.");
            return Failed(findings, 1);
        }

        OwnershipBackfillPlanResult? plan;
        JsonDocument planDocument;

        try
        {
            var planJson = File.ReadAllText(planPath);
            plan = JsonSerializer.Deserialize<OwnershipBackfillPlanResult>(planJson, JsonOptions);
            planDocument = JsonDocument.Parse(planJson);
        }
        catch (JsonException)
        {
            Add(findings, "SIGNOFF_PLAN_JSON_INVALID", "Blocking", "Plan JSON is invalid.");
            return Failed(findings, 1);
        }
        catch (IOException)
        {
            Add(findings, "SIGNOFF_PLAN_READ_FAILED", "Blocking", "Plan JSON could not be read.");
            return Failed(findings, 1);
        }

        if (plan is null)
        {
            Add(findings, "SIGNOFF_PLAN_PARSE_FAILED", "Blocking", "Plan JSON could not be parsed.");
            return Failed(findings, 1);
        }

        using (planDocument)
        {
            var propertyNames = CollectPropertyNames(planDocument.RootElement);
            if (propertyNames.Any(name => ForbiddenPlanPropertyFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
            {
                Add(findings, "SIGNOFF_PLAN_FORBIDDEN_FIELDS", "Blocking", "Plan contains payload/secret-like fields and cannot be signed off.");
            }
        }

        if (!string.Equals(plan.SummaryDraft.Mode, "PlanOnly", StringComparison.Ordinal))
            Add(findings, "SIGNOFF_PLAN_MODE_INVALID", "Blocking", "Plan summary mode must be PlanOnly.");

        if (plan.NonClaims.Count == 0 || plan.SummaryDraft.NonClaims.Count == 0)
            Add(findings, "SIGNOFF_PLAN_NON_CLAIMS_MISSING", "Blocking", "Plan non-claims are required.");

        if (!string.Equals(plan.PlanHash, options.ExpectedPlanHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(findings, "SIGNOFF_PLAN_HASH_MISMATCH", "Blocking", "PlanHash does not match --expected-plan-hash.");
        }

        if (plan.SummaryDraft.TotalRecordsPlanned != plan.PlannedRecords.Count)
            Add(findings, "SIGNOFF_PLAN_TOTAL_MISMATCH", "Blocking", "SummaryDraft.TotalRecordsPlanned does not match planned records count.");

        foreach (var record in plan.PlannedRecords)
        {
            if (record.Reason.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase))
                Add(findings, "SIGNOFF_PLAN_AMBIGUOUS_RECORD", "Blocking", "Plan contains an ambiguous planned record.");

            if (!record.ProposedOrganizationId.HasValue)
                Add(findings, "SIGNOFF_PLAN_MISSING_PROPOSED_ORGANIZATION", "Blocking", "Plan contains a record with missing ProposedOrganizationId.");

            if (!record.ProposedProjectId.HasValue)
                Add(findings, "SIGNOFF_PLAN_MISSING_PROPOSED_PROJECT", "Blocking", "Plan contains a record with missing ProposedProjectId.");
        }

        if (findings.Count > 0)
        {
            var exitCode = findings.Any(finding => string.Equals(finding.Code, "SIGNOFF_PLAN_HASH_MISMATCH", StringComparison.Ordinal))
                ? 2
                : 1;

            return Failed(findings, exitCode);
        }

        var signedAtUtc = DateTimeOffset.UtcNow;
        var signoffId = BuildSignoffId(plan.PlanHash, options.Reviewer!, options.Ticket!, signedAtUtc);

        var artifact = new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = signoffId,
            PlanId = plan.PlanId,
            PlanHash = plan.PlanHash,
            PlanPath = planPath,
            Reviewer = options.Reviewer!,
            Ticket = options.Ticket!,
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = signedAtUtc,
            ExpiresAtUtc = options.ExpiresAtUtc,
            ToolStage = "P6-06",
            Notes = options.Notes,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        return new OwnershipBackfillPlanSignoffValidationResult
        {
            Passed = true,
            ExitCode = 0,
            Findings = [],
            Artifact = artifact
        };
    }

    private static OwnershipBackfillPlanSignoffValidationResult Failed(IReadOnlyList<OwnershipBackfillPlanSignoffFinding> findings, int exitCode)
    {
        return new OwnershipBackfillPlanSignoffValidationResult
        {
            Passed = false,
            ExitCode = exitCode,
            Findings = findings,
            Artifact = null
        };
    }

    private static HashSet<string> CollectPropertyNames(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPropertyNamesRecursive(element, names);
        return names;
    }

    private static void CollectPropertyNamesRecursive(JsonElement element, ISet<string> names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                names.Add(property.Name);
                CollectPropertyNamesRecursive(property.Value, names);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectPropertyNamesRecursive(item, names);
        }
    }

    private static string BuildSignoffId(string planHash, string reviewer, string ticket, DateTimeOffset signedAtUtc)
    {
        var canonical = string.Join('|', planHash.ToLowerInvariant(), reviewer, ticket, signedAtUtc.ToString("O"));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        var token = Convert.ToHexString(bytes).ToLowerInvariant()[..12];
        return $"{signedAtUtc:yyyyMMddHHmmss}-{token}";
    }

    private static void Add(
        ICollection<OwnershipBackfillPlanSignoffFinding> findings,
        string code,
        string severity,
        string message)
    {
        findings.Add(new OwnershipBackfillPlanSignoffFinding
        {
            Code = code,
            Severity = severity,
            Message = message
        });
    }
}
