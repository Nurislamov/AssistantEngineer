using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class OwnershipBackfillStagingApplyPreflightValidator
{
    public OwnershipBackfillStagingApplyPreflightResult Validate(OwnershipBackfillStagingApplyPreflightOptions options)
    {
        var findings = new List<OwnershipBackfillStagingApplyPreflightFinding>();
        var metrics = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EnvironmentName"] = options.EnvironmentName ?? "missing",
            ["EnableStagingApply"] = options.EnableStagingApply.ToString(),
            ["ConfirmNoProductionConnection"] = options.ConfirmNoProductionConnection.ToString()
        };

        var environmentName = options.EnvironmentName?.Trim();
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            AddBlocking(findings, "STAGING_PREFLIGHT_ENVIRONMENT_REQUIRED", "--environment is required.");
        }
        else if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            AddBlocking(findings, "STAGING_PREFLIGHT_PRODUCTION_DENIED", "Production environment is hard-denied for staging preflight.", "Staging", environmentName);
        }
        else if (!string.Equals(environmentName, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            AddBlocking(findings, "STAGING_PREFLIGHT_UNKNOWN_ENVIRONMENT", "Unknown environment is denied.", "Staging", environmentName);
        }

        if (!options.EnableStagingApply)
        {
            AddBlocking(findings, "STAGING_PREFLIGHT_ENABLE_FLAG_REQUIRED", "--enable-staging-apply must be set for future readiness checks.");
        }

        if (!options.ConfirmNoProductionConnection)
        {
            AddBlocking(findings, "STAGING_PREFLIGHT_CONFIRMATION_REQUIRED", "--confirm-no-production-connection must be set.");
        }

        ValidateRequiredText(options.ApplyInputHash, "--apply-input-hash is required.", "STAGING_PREFLIGHT_APPLY_INPUT_HASH_REQUIRED", findings);
        ValidateRequiredText(options.BackupReference, "--backup-reference is required.", "STAGING_PREFLIGHT_BACKUP_REFERENCE_REQUIRED", findings);
        ValidateRequiredText(options.RollbackReadinessReference, "--rollback-readiness-reference is required.", "STAGING_PREFLIGHT_ROLLBACK_REFERENCE_REQUIRED", findings);
        ValidateRequiredText(options.OperatorId, "--operator is required.", "STAGING_PREFLIGHT_OPERATOR_REQUIRED", findings);
        ValidateRequiredText(options.SchemaVersion, "--schema-version is required.", "STAGING_PREFLIGHT_SCHEMA_VERSION_REQUIRED", findings);

        ValidateRequiredFile(options.ReadinessResultPath, "--readiness-result is required.", "STAGING_PREFLIGHT_READINESS_REQUIRED", findings);
        ValidateRequiredFile(options.PlanPath, "--plan is required.", "STAGING_PREFLIGHT_PLAN_REQUIRED", findings);
        ValidateRequiredFile(options.SignoffPath, "--signoff is required.", "STAGING_PREFLIGHT_SIGNOFF_REQUIRED", findings);

        return new OwnershipBackfillStagingApplyPreflightResult
        {
            Passed = findings.Count == 0,
            Findings = findings,
            Metrics = metrics,
            NonClaims =
            [
                .. OwnershipBackfillConstants.NonClaims,
                "No staging apply execution claim.",
                "No production apply enabled claim."
            ]
        };
    }

    private static void ValidateRequiredText(
        string? value,
        string message,
        string code,
        ICollection<OwnershipBackfillStagingApplyPreflightFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddBlocking(findings, code, message);
        }
    }

    private static void ValidateRequiredFile(
        string? path,
        string missingMessage,
        string missingCode,
        ICollection<OwnershipBackfillStagingApplyPreflightFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            AddBlocking(findings, missingCode, missingMessage);
            return;
        }

        if (!File.Exists(path))
        {
            AddBlocking(findings, $"{missingCode}_NOT_FOUND", "Required file was not found.", "existing file", path);
        }
    }

    private static void AddBlocking(
        ICollection<OwnershipBackfillStagingApplyPreflightFinding> findings,
        string code,
        string message,
        string? expected = null,
        string? actual = null)
    {
        findings.Add(new OwnershipBackfillStagingApplyPreflightFinding
        {
            Code = code,
            Severity = "Blocking",
            Message = message,
            Expected = expected,
            Actual = actual
        });
    }
}
