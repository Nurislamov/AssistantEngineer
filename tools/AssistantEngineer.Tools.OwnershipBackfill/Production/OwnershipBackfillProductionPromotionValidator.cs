using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] ForbiddenPropertyFragments =
    [
        "payload",
        "secret",
        "token",
        "password",
        "connectionstring",
        "apikey"
    ];

    private static readonly string[] SecretLikeValueFragments =
    [
        "password=",
        "pwd=",
        "token=",
        "apikey=",
        "connection string",
        "data source=",
        "host="
    ];

    public async Task<OwnershipBackfillProductionPromotionValidationResult> ValidateAsync(
        OwnershipBackfillProductionPromotionOptions options,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<OwnershipBackfillProductionPromotionFinding>();
        var metrics = new Dictionary<string, string>(StringComparer.Ordinal);
        var inputInvalid = false;

        var stagingAcceptancePath = ValidateRequiredFilePath(options.StagingAcceptancePath, "--staging-acceptance", findings, ref inputInvalid);
        var productionDryRunPath = ValidateRequiredFilePath(options.ProductionDryRunSummaryPath, "--production-dry-run", findings, ref inputInvalid);
        var productionGatePath = ValidateRequiredFilePath(options.ProductionGateResultPath, "--production-gate-result", findings, ref inputInvalid);
        var productionPlanPath = ValidateRequiredFilePath(options.ProductionPlanPath, "--production-plan", findings, ref inputInvalid);
        var productionSignoffPath = ValidateRequiredFilePath(options.ProductionSignoffPath, "--production-signoff", findings, ref inputInvalid);
        var productionReadinessPath = ValidateRequiredFilePath(options.ProductionReadinessPath, "--production-readiness", findings, ref inputInvalid);
        var productionPreviousValuesPath = ValidateRequiredFilePath(options.ProductionPreviousValuesPath, "--production-previous-values", findings, ref inputInvalid);

        ValidateRequiredText(options.ProductionChangeRequestId, "--production-change-request-id is required.", "PROMOTION_CHANGE_REQUEST_REQUIRED", findings, ref inputInvalid);

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            Add(findings, "PROMOTION_OUTPUT_REQUIRED", "Blocking", "--output is required.", "Unknown");
            inputInvalid = true;
        }

        if (string.IsNullOrWhiteSpace(options.RulesetVersion))
        {
            Add(findings, "PROMOTION_RULESET_REQUIRED", "Blocking", "--ruleset-version is required.", "Unknown");
            inputInvalid = true;
        }

        if (options.MaxStagingAcceptanceAgeHours <= 0)
        {
            Add(findings, "PROMOTION_STAGING_TTL_INVALID", "Blocking", "--max-staging-acceptance-age-hours must be positive.", "Unknown");
            inputInvalid = true;
        }

        if (options.MaxProductionSignoffAgeHours <= 0)
        {
            Add(findings, "PROMOTION_SIGNOFF_TTL_INVALID", "Blocking", "--max-production-signoff-age-hours must be positive.", "Unknown");
            inputInvalid = true;
        }

        OwnershipBackfillStagingAcceptanceResult? stagingAcceptance = null;
        OwnershipBackfillDryRunSummary? productionDryRun = null;
        OwnershipBackfillGateResult? productionGate = null;
        OwnershipBackfillPlanResult? productionPlan = null;
        OwnershipBackfillPlanSignoffArtifact? productionSignoff = null;
        OwnershipBackfillApplyReadinessResult? productionReadiness = null;
        List<OwnershipBackfillPreviousValueSnapshot>? productionPreviousValues = null;

        JsonDocument? stagingAcceptanceDocument = null;
        JsonDocument? productionDryRunDocument = null;
        JsonDocument? productionGateDocument = null;
        JsonDocument? productionPlanDocument = null;
        JsonDocument? productionSignoffDocument = null;
        JsonDocument? productionReadinessDocument = null;
        JsonDocument? productionPreviousValuesDocument = null;

        if (!inputInvalid)
        {
            stagingAcceptance = await TryReadArtifactAsync<OwnershipBackfillStagingAcceptanceResult>(
                stagingAcceptancePath!,
                "staging-acceptance",
                findings,
                cancellationToken,
                document => stagingAcceptanceDocument = document);

            productionDryRun = await TryReadArtifactAsync<OwnershipBackfillDryRunSummary>(
                productionDryRunPath!,
                "production-dry-run",
                findings,
                cancellationToken,
                document => productionDryRunDocument = document);

            productionGate = await TryReadArtifactAsync<OwnershipBackfillGateResult>(
                productionGatePath!,
                "production-gate-result",
                findings,
                cancellationToken,
                document => productionGateDocument = document);

            productionPlan = await TryReadArtifactAsync<OwnershipBackfillPlanResult>(
                productionPlanPath!,
                "production-plan",
                findings,
                cancellationToken,
                document => productionPlanDocument = document);

            productionSignoff = await TryReadArtifactAsync<OwnershipBackfillPlanSignoffArtifact>(
                productionSignoffPath!,
                "production-signoff",
                findings,
                cancellationToken,
                document => productionSignoffDocument = document);

            productionReadiness = await TryReadArtifactAsync<OwnershipBackfillApplyReadinessResult>(
                productionReadinessPath!,
                "production-readiness",
                findings,
                cancellationToken,
                document => productionReadinessDocument = document);

            productionPreviousValues = await TryReadListArtifactAsync<OwnershipBackfillPreviousValueSnapshot>(
                productionPreviousValuesPath!,
                "production-previous-values",
                findings,
                cancellationToken,
                document => productionPreviousValuesDocument = document);

            if (stagingAcceptance is null ||
                productionDryRun is null ||
                productionGate is null ||
                productionPlan is null ||
                productionSignoff is null ||
                productionReadiness is null ||
                productionPreviousValues is null)
            {
                inputInvalid = true;
            }
        }

        try
        {
            if (!inputInvalid)
            {
                ValidateForbiddenFields(stagingAcceptanceDocument!, "staging-acceptance", findings);
                ValidateForbiddenFields(productionDryRunDocument!, "production-dry-run", findings);
                ValidateForbiddenFields(productionGateDocument!, "production-gate-result", findings);
                ValidateForbiddenFields(productionPlanDocument!, "production-plan", findings);
                ValidateForbiddenFields(productionSignoffDocument!, "production-signoff", findings);
                ValidateForbiddenFields(productionReadinessDocument!, "production-readiness", findings);
                ValidateForbiddenFields(productionPreviousValuesDocument!, "production-previous-values", findings);

                ValidateSecretLikeValues(stagingAcceptanceDocument!, "staging-acceptance", findings);
                ValidateSecretLikeValues(productionDryRunDocument!, "production-dry-run", findings);
                ValidateSecretLikeValues(productionGateDocument!, "production-gate-result", findings);
                ValidateSecretLikeValues(productionPlanDocument!, "production-plan", findings);
                ValidateSecretLikeValues(productionSignoffDocument!, "production-signoff", findings);
                ValidateSecretLikeValues(productionReadinessDocument!, "production-readiness", findings);
                ValidateSecretLikeValues(productionPreviousValuesDocument!, "production-previous-values", findings);

                ValidateStagingAcceptance(stagingAcceptance!, stagingAcceptanceDocument!, options, findings, metrics);
                ValidateProductionDryRun(productionDryRun!, findings, metrics);
                ValidateProductionGate(productionGate!, findings, metrics);
                ValidateProductionPlan(productionPlan!, findings, metrics);
                ValidateProductionSignoff(productionSignoff!, productionPlan!, options, findings, metrics);
                ValidateProductionReadiness(productionReadiness!, productionPlan!, findings, metrics);
                ValidatePreviousValuesCompleteness(productionPreviousValues!, productionPlan!, findings, metrics);
                ValidateCrossEnvironmentSeparation(stagingAcceptance!, productionReadiness!, productionSignoff!, options, findings, metrics);
                ValidateChangeRequestBinding(stagingAcceptance!, options.ProductionChangeRequestId!, findings, metrics);
                ValidateBackupRollbackReadiness(productionReadiness!, options, findings, metrics);
                ValidateNoPretendExecutionInPromotionInputs(productionDryRunDocument!, productionGateDocument!, productionPlanDocument!, productionSignoffDocument!, productionReadinessDocument!, findings);

                var hashCalculator = new OwnershipBackfillProductionPromotionHashCalculator();
                var promotionHash = hashCalculator.Compute(
                    stagingAcceptanceDocument!,
                    productionDryRunDocument!,
                    productionGateDocument!,
                    productionPlanDocument!,
                    productionSignoffDocument!,
                    productionReadinessDocument!,
                    productionPreviousValuesDocument!,
                    options.ProductionChangeRequestId!,
                    options.RulesetVersion);

                metrics["ProductionPromotionHash"] = promotionHash;
                metrics["StagingRunHash"] = stagingAcceptance!.StagingRunHash;
                metrics["ProductionApplyInputHash"] = productionReadiness!.ApplyInputHash;
                metrics["ProductionPlanHash"] = productionPlan!.PlanHash;
                metrics["ProductionChangeRequestId"] = options.ProductionChangeRequestId!;
                metrics["RulesetVersion"] = options.RulesetVersion;

                var hasBlocking = findings.Any(finding => IsBlockingSeverity(finding.Severity));
                var hasExpired = findings.Any(finding =>
                    finding.Code.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(finding.Category, "Expired", StringComparison.OrdinalIgnoreCase));

                var status = hasBlocking
                    ? (hasExpired ? "Expired" : "Rejected")
                    : "ReadyForProductionApproval";

                var decision = new OwnershipBackfillProductionPromotionDecision
                {
                    Ready = !hasBlocking,
                    DecisionId = BuildDecisionId(promotionHash),
                    DecisionStatus = status,
                    ProductionPromotionHash = promotionHash,
                    StagingRunHash = stagingAcceptance.StagingRunHash,
                    ProductionApplyInputHash = productionReadiness.ApplyInputHash,
                    ProductionPlanHash = productionPlan.PlanHash,
                    ProductionChangeRequestId = options.ProductionChangeRequestId!,
                    Findings = findings,
                    Metrics = metrics,
                    NonClaims =
                    [
                        .. OwnershipBackfillConstants.NonClaims,
                        "No production apply enabled claim.",
                        "No staging apply execution claim.",
                        "No production ownership backfill execution claim."
                    ]
                };

                return new OwnershipBackfillProductionPromotionValidationResult
                {
                    Decision = decision,
                    ExitCode = hasBlocking ? 2 : 0
                };
            }

            var invalidDecision = new OwnershipBackfillProductionPromotionDecision
            {
                Ready = false,
                DecisionId = BuildDecisionId("invalid"),
                DecisionStatus = "NotReady",
                ProductionPromotionHash = "unavailable",
                StagingRunHash = "unavailable",
                ProductionApplyInputHash = "unavailable",
                ProductionPlanHash = "unavailable",
                ProductionChangeRequestId = options.ProductionChangeRequestId ?? "unavailable",
                Findings = findings,
                Metrics = metrics,
                NonClaims =
                [
                    .. OwnershipBackfillConstants.NonClaims,
                    "No production apply enabled claim.",
                    "No staging apply execution claim.",
                    "No production ownership backfill execution claim."
                ]
            };

            return new OwnershipBackfillProductionPromotionValidationResult
            {
                Decision = invalidDecision,
                ExitCode = 1
            };
        }
        finally
        {
            stagingAcceptanceDocument?.Dispose();
            productionDryRunDocument?.Dispose();
            productionGateDocument?.Dispose();
            productionPlanDocument?.Dispose();
            productionSignoffDocument?.Dispose();
            productionReadinessDocument?.Dispose();
            productionPreviousValuesDocument?.Dispose();
        }
    }

    private static void ValidateStagingAcceptance(
        OwnershipBackfillStagingAcceptanceResult stagingAcceptance,
        JsonDocument stagingAcceptanceDocument,
        OwnershipBackfillProductionPromotionOptions options,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!stagingAcceptance.Accepted)
            Add(findings, "PROMOTION_STAGING_ACCEPTANCE_NOT_ACCEPTED", "Blocking", "Staging acceptance must be Accepted=true.", "MissingApproval");

        if (string.IsNullOrWhiteSpace(stagingAcceptance.StagingRunHash))
            Add(findings, "PROMOTION_STAGING_RUNHASH_MISSING", "Blocking", "StagingRunHash is required.", "EvidenceMismatch");

        if (string.IsNullOrWhiteSpace(stagingAcceptance.ApplyInputHash))
            Add(findings, "PROMOTION_STAGING_APPLY_INPUT_HASH_MISSING", "Blocking", "Staging ApplyInputHash is required.", "EvidenceMismatch");

        if (string.IsNullOrWhiteSpace(stagingAcceptance.StagingChangeId))
            Add(findings, "PROMOTION_STAGING_CHANGE_ID_MISSING", "Blocking", "Staging change id is required.", "EvidenceMismatch");

        if (string.IsNullOrWhiteSpace(stagingAcceptance.OperatorId))
            Add(findings, "PROMOTION_STAGING_OPERATOR_MISSING", "Blocking", "Staging operator id is required.", "EvidenceMismatch");

        if (stagingAcceptance.NonClaims.Count == 0)
            Add(findings, "PROMOTION_STAGING_NONCLAIMS_MISSING", "Blocking", "Staging acceptance non-claims are required.", "EvidenceMismatch");

        if (TryGetStringProperty(stagingAcceptanceDocument.RootElement, "SignedAtUtc", out var signedAtText) &&
            DateTimeOffset.TryParse(signedAtText, out var signedAtUtc))
        {
            var ageHours = Math.Max(0d, (DateTimeOffset.UtcNow - signedAtUtc).TotalHours);
            metrics["StagingAcceptanceAgeHours"] = ageHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

            if (ageHours > options.MaxStagingAcceptanceAgeHours)
            {
                Add(
                    findings,
                    "PROMOTION_STAGING_ACCEPTANCE_EXPIRED_BY_TTL",
                    "Blocking",
                    "Staging acceptance age exceeds configured TTL.",
                    "Expired",
                    expected: options.MaxStagingAcceptanceAgeHours.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    actual: ageHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        else
        {
            metrics["StagingAcceptanceAgeHours"] = "unverified";
            Add(findings, "PROMOTION_STAGING_TTL_UNVERIFIED", "Warning", "Staging acceptance timestamp is not available for TTL verification.", "EvidenceMismatch");
        }

        if (TryGetStringProperty(stagingAcceptanceDocument.RootElement, "ExpiresAtUtc", out var expiresText) &&
            DateTimeOffset.TryParse(expiresText, out var expiresUtc))
        {
            metrics["StagingAcceptanceExpiresAtUtc"] = expiresUtc.ToString("O");
            if (expiresUtc <= DateTimeOffset.UtcNow)
            {
                Add(findings, "PROMOTION_STAGING_ACCEPTANCE_EXPIRED", "Blocking", "Staging acceptance is expired.", "Expired");
            }
        }
    }

    private static void ValidateProductionDryRun(
        OwnershipBackfillDryRunSummary dryRunSummary,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!string.Equals(dryRunSummary.Mode, OwnershipBackfillRunMode.DryRun.ToString(), StringComparison.Ordinal))
        {
            Add(findings, "PROMOTION_PRODUCTION_DRYRUN_MODE_INVALID", "Blocking", "Production dry-run mode must be DryRun.", "EvidenceMismatch", expected: "DryRun", actual: dryRunSummary.Mode);
        }

        if (dryRunSummary.NonClaims.Count == 0)
            Add(findings, "PROMOTION_PRODUCTION_DRYRUN_NONCLAIMS_MISSING", "Blocking", "Production dry-run non-claims are required.", "EvidenceMismatch");

        if (dryRunSummary.TotalRecordsResolvable + dryRunSummary.TotalRecordsUnresolved > dryRunSummary.TotalRecordsScanned)
            Add(findings, "PROMOTION_PRODUCTION_DRYRUN_TOTALS_INCONSISTENT", "Blocking", "Production dry-run totals are inconsistent.", "EvidenceMismatch");

        metrics["ProductionDryRunRunId"] = dryRunSummary.RunId;
        metrics["ProductionDryRunTotalRecordsScanned"] = dryRunSummary.TotalRecordsScanned.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ProductionDryRunTotalRecordsUnresolved"] = dryRunSummary.TotalRecordsUnresolved.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ValidateProductionGate(
        OwnershipBackfillGateResult gateResult,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!gateResult.Passed)
            Add(findings, "PROMOTION_PRODUCTION_GATE_FAILED", "Blocking", "Production gate result must be Passed=true.", "EvidenceMismatch");

        if (gateResult.NonClaims.Count == 0)
            Add(findings, "PROMOTION_PRODUCTION_GATE_NONCLAIMS_MISSING", "Blocking", "Production gate non-claims are required.", "EvidenceMismatch");

        if (gateResult.Findings.Any(finding => IsBlockingSeverity(finding.Severity)))
            Add(findings, "PROMOTION_PRODUCTION_GATE_BLOCKING_FINDINGS", "Blocking", "Production gate contains blocking/error findings.", "EvidenceMismatch");

        metrics["ProductionGateRunId"] = gateResult.RunId;
        metrics["ProductionGatePassed"] = gateResult.Passed.ToString();
    }

    private static void ValidateProductionPlan(
        OwnershipBackfillPlanResult planResult,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!string.Equals(planResult.SummaryDraft.Mode, "PlanOnly", StringComparison.Ordinal))
            Add(findings, "PROMOTION_PRODUCTION_PLAN_MODE_INVALID", "Blocking", "Production plan mode must be PlanOnly.", "EvidenceMismatch", expected: "PlanOnly", actual: planResult.SummaryDraft.Mode);

        if (string.IsNullOrWhiteSpace(planResult.PlanHash))
            Add(findings, "PROMOTION_PRODUCTION_PLAN_HASH_MISSING", "Blocking", "Production plan hash is required.", "EvidenceMismatch");

        if (planResult.NonClaims.Count == 0 || planResult.SummaryDraft.NonClaims.Count == 0)
            Add(findings, "PROMOTION_PRODUCTION_PLAN_NONCLAIMS_MISSING", "Blocking", "Production plan non-claims are required.", "EvidenceMismatch");

        metrics["ProductionPlanId"] = planResult.PlanId;
        metrics["ProductionPlanHash"] = planResult.PlanHash;
        metrics["ProductionPlannedRecords"] = planResult.PlannedRecords.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ValidateProductionSignoff(
        OwnershipBackfillPlanSignoffArtifact signoff,
        OwnershipBackfillPlanResult plan,
        OwnershipBackfillProductionPromotionOptions options,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!string.Equals(signoff.PlanHash, plan.PlanHash, StringComparison.OrdinalIgnoreCase))
            Add(findings, "PROMOTION_PRODUCTION_SIGNOFF_PLANHASH_MISMATCH", "Blocking", "Production signoff PlanHash must match production plan PlanHash.", "HashMismatch", expected: plan.PlanHash, actual: signoff.PlanHash);

        if (!signoff.ConfirmationPhraseAccepted)
            Add(findings, "PROMOTION_PRODUCTION_SIGNOFF_CONFIRMATION_MISSING", "Blocking", "Production signoff confirmation is required.", "MissingApproval");

        if (signoff.NonClaims.Count == 0)
            Add(findings, "PROMOTION_PRODUCTION_SIGNOFF_NONCLAIMS_MISSING", "Blocking", "Production signoff non-claims are required.", "EvidenceMismatch");

        if (signoff.ExpiresAtUtc.HasValue && signoff.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            Add(findings, "PROMOTION_PRODUCTION_SIGNOFF_EXPIRED", "Blocking", "Production signoff is expired.", "Expired");

        var ageHours = Math.Max(0d, (DateTimeOffset.UtcNow - signoff.SignedAtUtc).TotalHours);
        metrics["ProductionSignoffAgeHours"] = ageHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        metrics["ProductionSignoffExpiresAtUtc"] = signoff.ExpiresAtUtc?.ToString("O") ?? "null";

        if (ageHours > options.MaxProductionSignoffAgeHours)
        {
            Add(
                findings,
                "PROMOTION_PRODUCTION_SIGNOFF_TTL_EXCEEDED",
                "Blocking",
                "Production signoff age exceeds configured TTL.",
                "Expired",
                expected: options.MaxProductionSignoffAgeHours.ToString(System.Globalization.CultureInfo.InvariantCulture),
                actual: ageHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static void ValidateProductionReadiness(
        OwnershipBackfillApplyReadinessResult readiness,
        OwnershipBackfillPlanResult plan,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!readiness.Passed)
            Add(findings, "PROMOTION_PRODUCTION_READINESS_FAILED", "Blocking", "Production readiness must be Passed=true.", "EvidenceMismatch");

        if (string.IsNullOrWhiteSpace(readiness.ApplyInputHash))
            Add(findings, "PROMOTION_PRODUCTION_APPLY_INPUT_HASH_MISSING", "Blocking", "Production readiness ApplyInputHash is required.", "EvidenceMismatch");

        if (!string.Equals(readiness.PlanHash, plan.PlanHash, StringComparison.OrdinalIgnoreCase))
            Add(findings, "PROMOTION_PRODUCTION_READINESS_PLANHASH_MISMATCH", "Blocking", "Production readiness PlanHash must match production plan PlanHash.", "HashMismatch", expected: plan.PlanHash, actual: readiness.PlanHash);

        if (readiness.NonClaims.Count == 0)
            Add(findings, "PROMOTION_PRODUCTION_READINESS_NONCLAIMS_MISSING", "Blocking", "Production readiness non-claims are required.", "EvidenceMismatch");

        if (readiness.Findings.Any(finding => IsBlockingSeverity(finding.Severity)))
            Add(findings, "PROMOTION_PRODUCTION_READINESS_BLOCKING_FINDINGS", "Blocking", "Production readiness contains blocking/error findings.", "EvidenceMismatch");

        metrics["ProductionReadinessId"] = readiness.ReadinessId;
        metrics["ProductionReadinessApplyInputHash"] = readiness.ApplyInputHash;
    }

    private static void ValidatePreviousValuesCompleteness(
        IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> previousValues,
        OwnershipBackfillPlanResult plan,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        var previousValueKeys = previousValues
            .Select(snapshot => $"{snapshot.RecordType}:{snapshot.RecordId}")
            .ToHashSet(StringComparer.Ordinal);

        var plannedKeys = plan.PlannedRecords
            .Select(record => $"{record.RecordType}:{record.RecordId}")
            .ToArray();

        var missingKeys = plannedKeys
            .Where(key => !previousValueKeys.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var completeness = plannedKeys.Length == 0
            ? 1d
            : (double)(plannedKeys.Length - missingKeys.Length) / plannedKeys.Length;

        metrics["ProductionPreviousValuesPlannedCount"] = plannedKeys.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ProductionPreviousValuesSnapshotCount"] = previousValues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ProductionPreviousValuesCompleteness"] = completeness.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        if (missingKeys.Length > 0)
        {
            Add(
                findings,
                "PROMOTION_PRODUCTION_PREVIOUS_VALUES_INCOMPLETE",
                "Blocking",
                "Production previous-values coverage is incomplete for planned records.",
                "RollbackEvidenceMissing",
                expected: "100%",
                actual: $"{(completeness * 100d):0.###}%");
        }
    }

    private static void ValidateCrossEnvironmentSeparation(
        OwnershipBackfillStagingAcceptanceResult stagingAcceptance,
        OwnershipBackfillApplyReadinessResult productionReadiness,
        OwnershipBackfillPlanSignoffArtifact productionSignoff,
        OwnershipBackfillProductionPromotionOptions options,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!options.RequireSeparateProductionEvidence)
            return;

        metrics["StagingApplyInputHash"] = stagingAcceptance.ApplyInputHash;
        metrics["ProductionApplyInputHash"] = productionReadiness.ApplyInputHash;

        if (string.Equals(stagingAcceptance.ApplyInputHash, productionReadiness.ApplyInputHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(
                findings,
                "PROMOTION_CROSS_ENV_APPLY_INPUT_HASH_REUSE",
                "Blocking",
                "Production ApplyInputHash must be separate from staging ApplyInputHash.",
                "HashMismatch",
                expected: "different hashes",
                actual: productionReadiness.ApplyInputHash);
        }

        if (string.Equals(stagingAcceptance.SignoffId, productionSignoff.SignoffId, StringComparison.OrdinalIgnoreCase))
        {
            Add(
                findings,
                "PROMOTION_CROSS_ENV_SIGNOFF_REUSE",
                "Blocking",
                "Production signoff must be separate from staging signoff.",
                "MissingApproval");
        }

        if (string.Equals(stagingAcceptance.ReadinessId, productionReadiness.ReadinessId, StringComparison.OrdinalIgnoreCase))
        {
            Add(
                findings,
                "PROMOTION_CROSS_ENV_READINESS_REUSE",
                "Blocking",
                "Production readiness must be separate from staging readiness.",
                "EvidenceMismatch");
        }
    }

    private static void ValidateChangeRequestBinding(
        OwnershipBackfillStagingAcceptanceResult stagingAcceptance,
        string productionChangeRequestId,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        metrics["ProductionChangeRequestId"] = productionChangeRequestId;

        if (string.Equals(stagingAcceptance.StagingChangeId, productionChangeRequestId, StringComparison.OrdinalIgnoreCase))
        {
            Add(
                findings,
                "PROMOTION_CHANGE_REQUEST_NOT_ENVIRONMENT_SPECIFIC",
                "Blocking",
                "Production change request id must be separate from staging change id.",
                "EvidenceMismatch",
                expected: "different change ids",
                actual: productionChangeRequestId);
        }
    }

    private static void ValidateBackupRollbackReadiness(
        OwnershipBackfillApplyReadinessResult readiness,
        OwnershipBackfillProductionPromotionOptions options,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        IDictionary<string, string> metrics)
    {
        var backupReference = FindMetricValue(readiness.Metrics, "backupreference");
        var rollbackReference = FindMetricValue(readiness.Metrics, "rollback");

        metrics["ProductionBackupReference"] = backupReference ?? "missing";
        metrics["ProductionRollbackReference"] = rollbackReference ?? "missing";

        if (options.RequireBackupReference && string.IsNullOrWhiteSpace(backupReference))
        {
            Add(
                findings,
                "PROMOTION_BACKUP_REFERENCE_MISSING",
                "Blocking",
                "Production backup reference is required but not present in structured readiness metrics.",
                "RollbackEvidenceMissing");
        }

        if (options.RequireRollbackReadiness && string.IsNullOrWhiteSpace(rollbackReference))
        {
            Add(
                findings,
                "PROMOTION_ROLLBACK_REFERENCE_MISSING",
                "Blocking",
                "Production rollback readiness reference is required but not present in structured readiness metrics.",
                "RollbackEvidenceMissing");
        }
    }

    private static void ValidateNoPretendExecutionInPromotionInputs(
        JsonDocument productionDryRunDocument,
        JsonDocument productionGateDocument,
        JsonDocument productionPlanDocument,
        JsonDocument productionSignoffDocument,
        JsonDocument productionReadinessDocument,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings)
    {
        var documents = new[]
        {
            ("production-dry-run", productionDryRunDocument),
            ("production-gate-result", productionGateDocument),
            ("production-plan", productionPlanDocument),
            ("production-signoff", productionSignoffDocument),
            ("production-readiness", productionReadinessDocument)
        };

        foreach (var (name, document) in documents)
        {
            if (TryGetBooleanProperty(document.RootElement, "applyExecuted", out var executed) && executed)
            {
                Add(findings, "PROMOTION_ARTIFACT_CLAIMS_EXECUTION", "Blocking", "Promotion inputs must not claim production apply execution.", "EvidenceMismatch", actual: name);
            }
        }
    }

    private static string? FindMetricValue(IReadOnlyDictionary<string, string> metrics, string fragment)
    {
        foreach (var item in metrics)
        {
            if (item.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Value))
                return item.Value;
        }

        return null;
    }

    private static string? ValidateRequiredFilePath(
        string? path,
        string optionName,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        ref bool inputInvalid)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Add(findings, "PROMOTION_REQUIRED_PATH_MISSING", "Blocking", $"{optionName} is required.", "Unknown");
            inputInvalid = true;
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            Add(findings, "PROMOTION_REQUIRED_PATH_NOT_FOUND", "Blocking", $"{optionName} file was not found.", "Unknown", expected: "Existing file", actual: fullPath);
            inputInvalid = true;
            return null;
        }

        return fullPath;
    }

    private static void ValidateRequiredText(
        string? value,
        string message,
        string code,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        ref bool inputInvalid)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        Add(findings, code, "Blocking", message, "Unknown");
        inputInvalid = true;
    }

    private static async Task<T?> TryReadArtifactAsync<T>(
        string path,
        string artifact,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        CancellationToken cancellationToken,
        Action<JsonDocument> setDocument)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var document = JsonDocument.Parse(content);
            setDocument(document);
            var parsed = JsonSerializer.Deserialize<T>(content, JsonOptions);
            if (parsed is null)
            {
                Add(findings, "PROMOTION_ARTIFACT_PARSE_FAILED", "Blocking", $"Unable to parse {artifact} artifact.", "EvidenceMismatch");
                return default;
            }

            return parsed;
        }
        catch (JsonException)
        {
            Add(findings, "PROMOTION_ARTIFACT_JSON_INVALID", "Blocking", $"{artifact} artifact JSON is invalid.", "EvidenceMismatch");
            return default;
        }
        catch (IOException)
        {
            Add(findings, "PROMOTION_ARTIFACT_READ_FAILED", "Blocking", $"{artifact} artifact could not be read.", "EvidenceMismatch");
            return default;
        }
    }

    private static async Task<List<T>?> TryReadListArtifactAsync<T>(
        string path,
        string artifact,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        CancellationToken cancellationToken,
        Action<JsonDocument> setDocument)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var document = JsonDocument.Parse(content);
            setDocument(document);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                Add(findings, "PROMOTION_ARTIFACT_SHAPE_INVALID", "Blocking", $"{artifact} artifact must be a JSON array.", "EvidenceMismatch");
                return null;
            }

            var parsed = JsonSerializer.Deserialize<List<T>>(content, JsonOptions);
            return parsed ?? [];
        }
        catch (JsonException)
        {
            Add(findings, "PROMOTION_ARTIFACT_JSON_INVALID", "Blocking", $"{artifact} artifact JSON is invalid.", "EvidenceMismatch");
            return null;
        }
        catch (IOException)
        {
            Add(findings, "PROMOTION_ARTIFACT_READ_FAILED", "Blocking", $"{artifact} artifact could not be read.", "EvidenceMismatch");
            return null;
        }
    }

    private static void ValidateForbiddenFields(
        JsonDocument document,
        string artifact,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings)
    {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPropertyNames(document.RootElement, properties);

        foreach (var propertyName in properties)
        {
            if (ForbiddenPropertyFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                Add(findings, "PROMOTION_FORBIDDEN_FIELD", "Blocking", "Artifact contains forbidden payload/secret-like field.", "SecretLeakage", actual: $"{artifact}:{propertyName}");
            }
        }
    }

    private static void ValidateSecretLikeValues(
        JsonDocument document,
        string artifact,
        ICollection<OwnershipBackfillProductionPromotionFinding> findings)
    {
        var values = new List<string>();
        CollectStringValues(document.RootElement, values);

        foreach (var value in values)
        {
            if (SecretLikeValueFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                Add(findings, "PROMOTION_SECRET_LIKE_VALUE", "Blocking", "Artifact contains a secret/connection-like string value.", "SecretLeakage", actual: artifact);
                return;
            }
        }
    }

    private static void CollectPropertyNames(JsonElement element, ISet<string> names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                names.Add(property.Name);
                CollectPropertyNames(property.Value, names);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectPropertyNames(item, names);
        }
    }

    private static void CollectStringValues(JsonElement element, ICollection<string> values)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            values.Add(element.GetString() ?? string.Empty);
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
                CollectStringValues(property.Value, values);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectStringValues(item, values);
        }
    }

    private static bool TryGetStringProperty(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind != JsonValueKind.String)
                return false;

            value = property.Value.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool TryGetBooleanProperty(JsonElement root, string propertyName, out bool value)
    {
        value = false;
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False)
                return false;

            value = property.Value.GetBoolean();
            return true;
        }

        return false;
    }

    private static bool IsBlockingSeverity(string severity)
    {
        return string.Equals(severity, "Blocking", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDecisionId(string token)
    {
        var now = DateTimeOffset.UtcNow;
        var safeToken = string.IsNullOrWhiteSpace(token) ? "promotion" : token[..Math.Min(token.Length, 12)];
        return $"{now:yyyyMMddHHmmss}-{safeToken}";
    }

    private static void Add(
        ICollection<OwnershipBackfillProductionPromotionFinding> findings,
        string code,
        string severity,
        string message,
        string? category = null,
        string? expected = null,
        string? actual = null)
    {
        findings.Add(new OwnershipBackfillProductionPromotionFinding
        {
            Code = code,
            Severity = severity,
            Message = message,
            Category = category,
            Expected = expected,
            Actual = actual
        });
    }
}
