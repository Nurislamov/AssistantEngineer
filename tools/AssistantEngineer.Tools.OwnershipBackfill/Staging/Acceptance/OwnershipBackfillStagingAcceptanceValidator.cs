using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingAcceptanceValidator
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

    public async Task<OwnershipBackfillStagingAcceptanceValidationResult> ValidateAsync(
        OwnershipBackfillStagingAcceptanceOptions options,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<OwnershipBackfillStagingAcceptanceFinding>();
        var metrics = new Dictionary<string, string>(StringComparer.Ordinal);
        var inputInvalid = false;

        var applyResultPath = ValidateRequiredFilePath(options.ApplyResultPath, "--apply-result", findings, ref inputInvalid);
        var postApplyDryRunPath = ValidateRequiredFilePath(options.PostApplyDryRunSummaryPath, "--post-apply-dry-run", findings, ref inputInvalid);
        var postApplyGatePath = ValidateRequiredFilePath(options.PostApplyGateResultPath, "--post-apply-gate-result", findings, ref inputInvalid);

        ValidateRequiredText(options.TenantIsolationMatrixResultReference, "--tenant-isolation-result is required.", "STAGING_ACCEPTANCE_TENANT_ISOLATION_REFERENCE_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.RegressionTestResultReference, "--regression-result is required.", "STAGING_ACCEPTANCE_REGRESSION_REFERENCE_REQUIRED", findings, ref inputInvalid);

        if (options.RequireRollbackEvidence)
        {
            ValidateRequiredText(options.RollbackEvidenceReference, "--rollback-evidence is required.", "STAGING_ACCEPTANCE_ROLLBACK_REFERENCE_REQUIRED", findings, ref inputInvalid);
        }

        ValidateRequiredText(options.ApplyInputHash, "--apply-input-hash is required.", "STAGING_ACCEPTANCE_APPLY_INPUT_HASH_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.PlanHash, "--plan-hash is required.", "STAGING_ACCEPTANCE_PLAN_HASH_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.SignoffId, "--signoff-id is required.", "STAGING_ACCEPTANCE_SIGNOFF_ID_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.ReadinessId, "--readiness-id is required.", "STAGING_ACCEPTANCE_READINESS_ID_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.StagingPreflightReference, "--staging-preflight is required.", "STAGING_ACCEPTANCE_PREFLIGHT_REFERENCE_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.OperatorId, "--operator is required.", "STAGING_ACCEPTANCE_OPERATOR_REQUIRED", findings, ref inputInvalid);
        ValidateRequiredText(options.StagingChangeId, "--staging-change-id is required.", "STAGING_ACCEPTANCE_CHANGE_ID_REQUIRED", findings, ref inputInvalid);

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            Add(findings, "STAGING_ACCEPTANCE_OUTPUT_REQUIRED", "Blocking", "--output is required.", "Unknown", expected: "Non-empty output directory");
            inputInvalid = true;
        }

        if (options.MaxPostApplyUnresolvedRate < 0d || options.MaxPostApplyUnresolvedRate > 1d)
        {
            Add(findings, "STAGING_ACCEPTANCE_UNRESOLVED_THRESHOLD_INVALID", "Blocking", "--max-post-apply-unresolved-rate must be between 0 and 1.", "Unknown");
            inputInvalid = true;
        }

        if (string.IsNullOrWhiteSpace(options.RulesetVersion))
        {
            Add(findings, "STAGING_ACCEPTANCE_RULESET_REQUIRED", "Blocking", "--ruleset-version is required.", "Unknown");
            inputInvalid = true;
        }

        OwnershipBackfillApplyExecutionResult? applyResult = null;
        OwnershipBackfillDryRunSummary? postApplyDryRunSummary = null;
        OwnershipBackfillGateResult? postApplyGateResult = null;

        JsonDocument? applyResultDocument = null;
        JsonDocument? postApplyDryRunDocument = null;
        JsonDocument? postApplyGateDocument = null;

        if (!inputInvalid)
        {
            applyResult = await TryReadArtifactAsync<OwnershipBackfillApplyExecutionResult>(
                applyResultPath!,
                "apply-result",
                findings,
                cancellationToken,
                document => applyResultDocument = document);

            postApplyDryRunSummary = await TryReadArtifactAsync<OwnershipBackfillDryRunSummary>(
                postApplyDryRunPath!,
                "post-apply-dry-run",
                findings,
                cancellationToken,
                document => postApplyDryRunDocument = document);

            postApplyGateResult = await TryReadArtifactAsync<OwnershipBackfillGateResult>(
                postApplyGatePath!,
                "post-apply-gate-result",
                findings,
                cancellationToken,
                document => postApplyGateDocument = document);

            if (applyResult is null || postApplyDryRunSummary is null || postApplyGateResult is null)
                inputInvalid = true;
        }

        string stagingRunHash;

        try
        {
            if (!inputInvalid)
            {
                ValidateForbiddenFields(applyResultDocument!, "apply-result", findings);
                ValidateForbiddenFields(postApplyDryRunDocument!, "post-apply-dry-run", findings);
                ValidateForbiddenFields(postApplyGateDocument!, "post-apply-gate-result", findings);

                ValidateApplyResult(applyResult!, options, findings, metrics);
                ValidatePostApplyDryRun(postApplyDryRunSummary!, options, findings, metrics);
                ValidatePostApplyGate(postApplyGateResult!, findings, metrics);
                ValidateHashReferences(
                    applyResultDocument!.RootElement,
                    options.ApplyInputHash!,
                    options.PlanHash!,
                    findings);

                ValidateStructuredReference(
                    options.TenantIsolationMatrixResultReference!,
                    "tenant-isolation-result",
                    options.RequireTenantIsolationPass,
                    findings,
                    metrics);

                ValidateStructuredReference(
                    options.RegressionTestResultReference!,
                    "regression-result",
                    options.RequireRegressionPass,
                    findings,
                    metrics);

                if (options.RequireRollbackEvidence)
                {
                    ValidateStructuredReference(
                        options.RollbackEvidenceReference!,
                        "rollback-evidence",
                        requirePassed: false,
                        findings,
                        metrics);
                }

                var calculator = new OwnershipBackfillStagingRunHashCalculator();
                stagingRunHash = calculator.Compute(
                    options.ApplyInputHash!,
                    options.PlanHash!,
                    options.SignoffId!,
                    options.ReadinessId!,
                    options.StagingPreflightReference!,
                    applyResultDocument!,
                    postApplyDryRunDocument!,
                    postApplyGateDocument!,
                    options.RollbackEvidenceReference ?? string.Empty,
                    options.TenantIsolationMatrixResultReference!,
                    options.RegressionTestResultReference!,
                    options.RulesetVersion);

                metrics["StagingRunHash"] = stagingRunHash;
                metrics["ApplyInputHash"] = options.ApplyInputHash!;
                metrics["PlanHash"] = options.PlanHash!;
                metrics["SignoffId"] = options.SignoffId!;
                metrics["ReadinessId"] = options.ReadinessId!;
                metrics["OperatorId"] = options.OperatorId!;
                metrics["StagingChangeId"] = options.StagingChangeId!;
                metrics["RulesetVersion"] = options.RulesetVersion;

                var accepted = !findings.Any(finding => IsBlockingSeverity(finding.Severity));
                var result = new OwnershipBackfillStagingAcceptanceResult
                {
                    Accepted = accepted,
                    AcceptanceId = BuildAcceptanceId(stagingRunHash),
                    StagingRunHash = stagingRunHash,
                    ApplyInputHash = options.ApplyInputHash!,
                    PlanHash = options.PlanHash!,
                    SignoffId = options.SignoffId!,
                    ReadinessId = options.ReadinessId!,
                    OperatorId = options.OperatorId!,
                    StagingChangeId = options.StagingChangeId!,
                    Findings = findings,
                    Metrics = metrics,
                    NonClaims =
                    [
                        .. OwnershipBackfillConstants.NonClaims,
                        "No staging apply execution claim.",
                        "No production apply enabled claim.",
                        "No ownership backfill execution claim."
                    ]
                };

                return new OwnershipBackfillStagingAcceptanceValidationResult
                {
                    Result = result,
                    ExitCode = accepted ? 0 : 2
                };
            }

            stagingRunHash = "unavailable";
            var invalidResult = new OwnershipBackfillStagingAcceptanceResult
            {
                Accepted = false,
                AcceptanceId = BuildAcceptanceId(stagingRunHash),
                StagingRunHash = stagingRunHash,
                ApplyInputHash = options.ApplyInputHash ?? "unavailable",
                PlanHash = options.PlanHash ?? "unavailable",
                SignoffId = options.SignoffId ?? "unavailable",
                ReadinessId = options.ReadinessId ?? "unavailable",
                OperatorId = options.OperatorId ?? "unavailable",
                StagingChangeId = options.StagingChangeId ?? "unavailable",
                Findings = findings,
                Metrics = metrics,
                NonClaims =
                [
                    .. OwnershipBackfillConstants.NonClaims,
                    "No staging apply execution claim.",
                    "No production apply enabled claim.",
                    "No ownership backfill execution claim."
                ]
            };

            return new OwnershipBackfillStagingAcceptanceValidationResult
            {
                Result = invalidResult,
                ExitCode = 1
            };
        }
        finally
        {
            applyResultDocument?.Dispose();
            postApplyDryRunDocument?.Dispose();
            postApplyGateDocument?.Dispose();
        }
    }

    private static string? ValidateRequiredFilePath(
        string? path,
        string optionName,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        ref bool inputInvalid)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Add(findings, "STAGING_ACCEPTANCE_REQUIRED_PATH_MISSING", "Blocking", $"{optionName} is required.", "Unknown");
            inputInvalid = true;
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            Add(findings, "STAGING_ACCEPTANCE_REQUIRED_PATH_NOT_FOUND", "Blocking", $"{optionName} file was not found.", "Unknown", expected: "Existing file", actual: fullPath);
            inputInvalid = true;
            return null;
        }

        return fullPath;
    }

    private static void ValidateRequiredText(
        string? value,
        string message,
        string code,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
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
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
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
                Add(findings, "STAGING_ACCEPTANCE_PARSE_FAILED", "Blocking", $"Unable to parse {artifact} artifact.", "EvidenceMismatch");
                return default;
            }

            return parsed;
        }
        catch (JsonException)
        {
            Add(findings, "STAGING_ACCEPTANCE_JSON_INVALID", "Blocking", $"{artifact} artifact JSON is invalid.", "EvidenceMismatch");
            return default;
        }
        catch (IOException)
        {
            Add(findings, "STAGING_ACCEPTANCE_READ_FAILED", "Blocking", $"{artifact} artifact could not be read.", "EvidenceMismatch");
            return default;
        }
    }

    private static void ValidateApplyResult(
        OwnershipBackfillApplyExecutionResult applyResult,
        OwnershipBackfillStagingAcceptanceOptions options,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        IDictionary<string, string> metrics)
    {
        metrics["ApplyResultSucceeded"] = applyResult.Succeeded.ToString();
        metrics["ApplyTotalRecordsPlanned"] = applyResult.TotalRecordsPlanned.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ApplyTotalRecordsUpdated"] = applyResult.TotalRecordsUpdated.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ApplyTotalRecordsSkipped"] = applyResult.TotalRecordsSkipped.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["ApplyTotalRecordsFailed"] = applyResult.TotalRecordsFailed.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (!applyResult.Succeeded)
            Add(findings, "STAGING_ACCEPTANCE_APPLY_NOT_SUCCEEDED", "Blocking", "Staging apply result status must be Succeeded.", "ApplyFailure", expected: "Succeeded=true", actual: "Succeeded=false");

        if (applyResult.NonClaims.Count == 0)
            Add(findings, "STAGING_ACCEPTANCE_APPLY_NONCLAIMS_MISSING", "Blocking", "Staging apply result non-claims are required.", "EvidenceMismatch");

        if (options.RequireZeroFailedRecords && applyResult.TotalRecordsFailed > 0)
        {
            Add(
                findings,
                "STAGING_ACCEPTANCE_FAILED_RECORDS_PRESENT",
                "Blocking",
                "Staging apply result contains failed records.",
                "ApplyFailure",
                expected: "0",
                actual: applyResult.TotalRecordsFailed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static void ValidatePostApplyDryRun(
        OwnershipBackfillDryRunSummary summary,
        OwnershipBackfillStagingAcceptanceOptions options,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!string.Equals(summary.Mode, OwnershipBackfillRunMode.DryRun.ToString(), StringComparison.Ordinal))
        {
            Add(findings, "STAGING_ACCEPTANCE_POST_DRYRUN_MODE_INVALID", "Blocking", "Post-apply dry-run summary mode must be DryRun.", "EvidenceMismatch", expected: "DryRun", actual: summary.Mode);
        }

        if (summary.NonClaims.Count == 0)
            Add(findings, "STAGING_ACCEPTANCE_POST_DRYRUN_NONCLAIMS_MISSING", "Blocking", "Post-apply dry-run summary non-claims are required.", "EvidenceMismatch");

        if (summary.TotalRecordsScanned < 0 || summary.TotalRecordsResolvable < 0 || summary.TotalRecordsUnresolved < 0)
            Add(findings, "STAGING_ACCEPTANCE_POST_DRYRUN_TOTALS_INVALID", "Blocking", "Post-apply dry-run totals must be non-negative.", "EvidenceMismatch");

        if (summary.TotalRecordsResolvable + summary.TotalRecordsUnresolved > summary.TotalRecordsScanned)
            Add(findings, "STAGING_ACCEPTANCE_POST_DRYRUN_TOTALS_INCONSISTENT", "Blocking", "Post-apply dry-run totals are inconsistent.", "EvidenceMismatch");

        var unresolvedRate = summary.TotalRecordsScanned == 0
            ? 0d
            : (double)summary.TotalRecordsUnresolved / summary.TotalRecordsScanned;
        metrics["PostApplyUnresolvedRate"] = unresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        metrics["PostApplyTotalRecordsScanned"] = summary.TotalRecordsScanned.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["PostApplyTotalRecordsUnresolved"] = summary.TotalRecordsUnresolved.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (unresolvedRate > options.MaxPostApplyUnresolvedRate)
        {
            Add(
                findings,
                "STAGING_ACCEPTANCE_POST_UNRESOLVED_RATE_EXCEEDED",
                "Blocking",
                "Post-apply unresolved rate exceeds configured threshold.",
                "UnresolvedDrift",
                expected: options.MaxPostApplyUnresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture),
                actual: unresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
        }

        var ambiguousCount = CountAmbiguous(summary);
        metrics["PostApplyAmbiguousCount"] = ambiguousCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (ambiguousCount > 0)
        {
            Add(
                findings,
                "STAGING_ACCEPTANCE_POST_AMBIGUOUS_PRESENT",
                "Blocking",
                "Post-apply dry-run contains ambiguous ownership records.",
                "AmbiguousOwnership",
                expected: "0",
                actual: ambiguousCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static void ValidatePostApplyGate(
        OwnershipBackfillGateResult gateResult,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        IDictionary<string, string> metrics)
    {
        metrics["PostApplyGatePassed"] = gateResult.Passed.ToString();
        metrics["PostApplyGateRunId"] = gateResult.RunId;

        if (!gateResult.Passed)
            Add(findings, "STAGING_ACCEPTANCE_POST_GATE_FAILED", "Blocking", "Post-apply evidence gate must be Passed=true.", "EvidenceMismatch");

        if (gateResult.NonClaims.Count == 0)
            Add(findings, "STAGING_ACCEPTANCE_POST_GATE_NONCLAIMS_MISSING", "Blocking", "Post-apply gate non-claims are required.", "EvidenceMismatch");

        if (gateResult.Findings.Any(finding => IsBlockingSeverity(finding.Severity)))
            Add(findings, "STAGING_ACCEPTANCE_POST_GATE_BLOCKING_FINDINGS", "Blocking", "Post-apply gate contains blocking/error findings.", "EvidenceMismatch");
    }

    private static void ValidateHashReferences(
        JsonElement applyResultRoot,
        string applyInputHash,
        string planHash,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings)
    {
        if (TryGetStringProperty(applyResultRoot, "applyInputHash", out var applyInputHashInArtifact) &&
            !string.Equals(applyInputHashInArtifact, applyInputHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(findings, "STAGING_ACCEPTANCE_APPLY_INPUT_HASH_MISMATCH", "Blocking", "ApplyInputHash does not match apply result artifact.", "HashMismatch", expected: applyInputHash, actual: applyInputHashInArtifact);
        }

        if (TryGetStringProperty(applyResultRoot, "planHash", out var planHashInArtifact) &&
            !string.Equals(planHashInArtifact, planHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(findings, "STAGING_ACCEPTANCE_PLAN_HASH_MISMATCH", "Blocking", "PlanHash does not match apply result artifact.", "HashMismatch", expected: planHash, actual: planHashInArtifact);
        }
    }

    private static void ValidateStructuredReference(
        string reference,
        string label,
        bool requirePassed,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        IDictionary<string, string> metrics)
    {
        metrics[$"{label}:reference"] = reference;
        var fullPath = Path.GetFullPath(reference);

        if (!File.Exists(fullPath))
        {
            metrics[$"{label}:status"] = "UnverifiedReference";
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
            ValidateForbiddenFields(document, label, findings);

            if (!TryGetBooleanProperty(document.RootElement, "passed", out var passed))
            {
                metrics[$"{label}:status"] = "StructuredWithoutPassedFlag";
                Add(findings, "STAGING_ACCEPTANCE_REFERENCE_STATUS_UNAVAILABLE", "Warning", $"{label} reference is structured but does not expose a passed flag.", "Unknown", actual: fullPath);
                return;
            }

            metrics[$"{label}:status"] = passed ? "Passed" : "Failed";
            if (requirePassed && !passed)
            {
                Add(findings, "STAGING_ACCEPTANCE_REFERENCE_FAILED", "Blocking", $"{label} reference indicates failed status.", label.Contains("tenant", StringComparison.OrdinalIgnoreCase) ? "TenantIsolationFailure" : "RegressionFailure", expected: "passed=true", actual: "passed=false");
            }
        }
        catch (IOException)
        {
            Add(findings, "STAGING_ACCEPTANCE_REFERENCE_READ_FAILED", "Warning", $"{label} reference could not be read as a structured result.", "Unknown", actual: fullPath);
        }
        catch (JsonException)
        {
            metrics[$"{label}:status"] = "UnstructuredReference";
        }
    }

    private static int CountAmbiguous(OwnershipBackfillDryRunSummary summary)
    {
        var total = 0;

        foreach (var pair in summary.UnresolvedByReason)
        {
            if (pair.Key.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase))
                total += pair.Value;
        }

        foreach (var metric in summary.RecordTypeMetrics)
        {
            foreach (var pair in metric.UnresolvedByReason)
            {
                if (pair.Key.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase))
                    total += pair.Value;
            }
        }

        return total;
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

    private static void ValidateForbiddenFields(
        JsonDocument document,
        string artifact,
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings)
    {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPropertyNames(document.RootElement, properties);

        foreach (var propertyName in properties)
        {
            if (ForbiddenPropertyFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                Add(findings, "STAGING_ACCEPTANCE_FORBIDDEN_FIELD", "Blocking", "Artifact contains forbidden payload/secret-like field.", "SecretLeakage", actual: $"{artifact}:{propertyName}");
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

    private static bool IsBlockingSeverity(string severity)
    {
        return string.Equals(severity, "Blocking", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAcceptanceId(string token)
    {
        var now = DateTimeOffset.UtcNow;
        var safeToken = string.IsNullOrWhiteSpace(token) ? "acceptance" : token[..Math.Min(token.Length, 12)];
        return $"{now:yyyyMMddHHmmss}-{safeToken}";
    }

    private static void Add(
        ICollection<OwnershipBackfillStagingAcceptanceFinding> findings,
        string code,
        string severity,
        string message,
        string category,
        string? expected = null,
        string? actual = null)
    {
        findings.Add(new OwnershipBackfillStagingAcceptanceFinding
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
