using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tools.OwnershipBackfill.Readiness;

public sealed class OwnershipBackfillApplyReadinessValidator
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
        "apiKey"
    ];

    public async Task<OwnershipBackfillApplyReadinessValidationResult> ValidateAsync(
        OwnershipBackfillApplyReadinessOptions options,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<OwnershipBackfillApplyReadinessFinding>();
        var metrics = new Dictionary<string, string>(StringComparer.Ordinal);
        var inputInvalid = false;

        var dryRunSummaryPath = ValidatePath(options.DryRunSummaryPath, "--dry-run", findings, ref inputInvalid);
        var gateResultPath = ValidatePath(options.GateResultPath, "--gate-result", findings, ref inputInvalid);
        var planPath = ValidatePath(options.PlanPath, "--plan", findings, ref inputInvalid);
        var signoffPath = ValidatePath(options.SignoffPath, "--signoff", findings, ref inputInvalid);
        var previousValuesPath = ValidatePath(options.PreviousValuesPath, "--previous-values", findings, ref inputInvalid);

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            Add(findings, "READINESS_OUTPUT_REQUIRED", "Blocking", "--output is required.", "output");
            inputInvalid = true;
        }

        if (options.MaxSignoffAgeHours <= 0)
        {
            Add(findings, "READINESS_SIGNOFF_AGE_INVALID", "Blocking", "--max-signoff-age-hours must be positive.", "signoff");
            inputInvalid = true;
        }

        if (string.IsNullOrWhiteSpace(options.RulesetVersion))
        {
            Add(findings, "READINESS_RULESET_REQUIRED", "Blocking", "--ruleset-version is required.", "ruleset");
            inputInvalid = true;
        }

        OwnershipBackfillDryRunSummary? dryRunSummary = null;
        OwnershipBackfillGateResult? gateResult = null;
        OwnershipBackfillPlanResult? plan = null;
        OwnershipBackfillPlanSignoffArtifact? signoff = null;
        List<OwnershipBackfillPreviousValueSnapshot>? previousValues = null;

        JsonDocument? dryRunSummaryDocument = null;
        JsonDocument? gateResultDocument = null;
        JsonDocument? planDocument = null;
        JsonDocument? signoffDocument = null;
        JsonDocument? previousValuesDocument = null;

        if (!inputInvalid)
        {
            dryRunSummary = await TryReadArtifactAsync<OwnershipBackfillDryRunSummary>(dryRunSummaryPath!, "dry-run", findings, cancellationToken, document => dryRunSummaryDocument = document);
            gateResult = await TryReadArtifactAsync<OwnershipBackfillGateResult>(gateResultPath!, "gate-result", findings, cancellationToken, document => gateResultDocument = document);
            plan = await TryReadArtifactAsync<OwnershipBackfillPlanResult>(planPath!, "plan", findings, cancellationToken, document => planDocument = document);
            signoff = await TryReadArtifactAsync<OwnershipBackfillPlanSignoffArtifact>(signoffPath!, "signoff", findings, cancellationToken, document => signoffDocument = document);
            previousValues = await TryReadListArtifactAsync<OwnershipBackfillPreviousValueSnapshot>(previousValuesPath!, "previous-values", findings, cancellationToken, document => previousValuesDocument = document);

            if (dryRunSummary is null || gateResult is null || plan is null || signoff is null || previousValues is null)
                inputInvalid = true;
        }

        try
        {
            if (!inputInvalid)
            {
                ValidateForbiddenFields(dryRunSummaryDocument!, "dry-run", findings);
                ValidateForbiddenFields(gateResultDocument!, "gate-result", findings);
                ValidateForbiddenFields(planDocument!, "plan", findings);
                ValidateForbiddenFields(signoffDocument!, "signoff", findings);
                ValidateForbiddenFields(previousValuesDocument!, "previous-values", findings);

                ValidateDryRunSummary(dryRunSummary!, findings);
                ValidateGateResult(gateResult!, findings);
                ValidatePlan(plan!, findings);
                ValidateSignoff(signoff!, plan!, options, findings, metrics);
                ValidateHashChain(plan!, signoff!, options, findings);
                ValidatePreviousValues(previousValues!, plan!, options.RequireRollbackReadiness, findings, metrics);

                var applyInputHash = ComputeApplyInputHash(
                    dryRunSummary!,
                    gateResult!,
                    plan!,
                    signoff!,
                    previousValues!,
                    options.RulesetVersion);

                metrics["ApplyInputHash"] = applyInputHash;
                metrics["PlanHash"] = plan!.PlanHash;
                metrics["SignoffPlanHash"] = signoff!.PlanHash;
                metrics["RulesetVersion"] = options.RulesetVersion;
                metrics["SignoffReviewer"] = signoff.Reviewer;
                metrics["SignoffTicket"] = signoff.Ticket;

                var hasBlocking = findings.Any(finding => IsBlockingSeverity(finding.Severity));
                var result = new OwnershipBackfillApplyReadinessResult
                {
                    Passed = !hasBlocking,
                    ReadinessId = BuildReadinessId(applyInputHash),
                    ApplyInputHash = applyInputHash,
                    PlanHash = plan.PlanHash,
                    SignoffPlanHash = signoff.PlanHash,
                    RulesetVersion = options.RulesetVersion,
                    Findings = findings,
                    Metrics = metrics,
                    NonClaims = OwnershipBackfillConstants.NonClaims
                };

                return new OwnershipBackfillApplyReadinessValidationResult
                {
                    Result = result,
                    ExitCode = hasBlocking ? 2 : 0
                };
            }

            var invalidResult = new OwnershipBackfillApplyReadinessResult
            {
                Passed = false,
                ReadinessId = BuildReadinessId("invalid"),
                ApplyInputHash = "unavailable",
                PlanHash = "unavailable",
                SignoffPlanHash = "unavailable",
                RulesetVersion = options.RulesetVersion ?? "P6-08",
                Findings = findings,
                Metrics = metrics,
                NonClaims = OwnershipBackfillConstants.NonClaims
            };

            return new OwnershipBackfillApplyReadinessValidationResult
            {
                Result = invalidResult,
                ExitCode = 1
            };
        }
        finally
        {
            dryRunSummaryDocument?.Dispose();
            gateResultDocument?.Dispose();
            planDocument?.Dispose();
            signoffDocument?.Dispose();
            previousValuesDocument?.Dispose();
        }
    }

    private static string? ValidatePath(
        string? path,
        string optionName,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
        ref bool inputInvalid)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Add(findings, "READINESS_PATH_REQUIRED", "Blocking", $"{optionName} is required.", optionName);
            inputInvalid = true;
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            Add(findings, "READINESS_PATH_NOT_FOUND", "Blocking", $"{optionName} file was not found.", optionName, actual: fullPath);
            inputInvalid = true;
            return null;
        }

        return fullPath;
    }

    private static async Task<T?> TryReadArtifactAsync<T>(
        string path,
        string artifact,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
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
                Add(findings, "READINESS_ARTIFACT_PARSE_FAILED", "Blocking", $"Unable to parse {artifact} artifact.", artifact);
                return default;
            }

            return parsed;
        }
        catch (JsonException)
        {
            Add(findings, "READINESS_ARTIFACT_JSON_INVALID", "Blocking", $"{artifact} artifact JSON is invalid.", artifact);
            return default;
        }
        catch (IOException)
        {
            Add(findings, "READINESS_ARTIFACT_READ_FAILED", "Blocking", $"{artifact} artifact could not be read.", artifact);
            return default;
        }
    }

    private static async Task<List<T>?> TryReadListArtifactAsync<T>(
        string path,
        string artifact,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
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
                Add(findings, "READINESS_ARTIFACT_SHAPE_INVALID", "Blocking", $"{artifact} artifact must be a JSON array.", artifact);
                return null;
            }

            var parsed = JsonSerializer.Deserialize<List<T>>(content, JsonOptions);
            return parsed ?? [];
        }
        catch (JsonException)
        {
            Add(findings, "READINESS_ARTIFACT_JSON_INVALID", "Blocking", $"{artifact} artifact JSON is invalid.", artifact);
            return null;
        }
        catch (IOException)
        {
            Add(findings, "READINESS_ARTIFACT_READ_FAILED", "Blocking", $"{artifact} artifact could not be read.", artifact);
            return null;
        }
    }

    private static void ValidateForbiddenFields(
        JsonDocument document,
        string artifact,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings)
    {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPropertyNames(document.RootElement, properties);

        foreach (var propertyName in properties)
        {
            if (ForbiddenPropertyFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                Add(findings, "READINESS_FORBIDDEN_FIELD", "Blocking", "Artifact contains forbidden payload/secret-like field.", artifact, actual: propertyName);
            }
        }
    }

    private static void ValidateDryRunSummary(
        OwnershipBackfillDryRunSummary summary,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings)
    {
        if (!string.Equals(summary.Mode, OwnershipBackfillRunMode.DryRun.ToString(), StringComparison.Ordinal))
            Add(findings, "READINESS_DRYRUN_MODE_INVALID", "Blocking", "Dry-run summary mode must be DryRun.", "dry-run", expected: "DryRun", actual: summary.Mode);

        if (summary.NonClaims.Count == 0)
            Add(findings, "READINESS_DRYRUN_NONCLAIMS_MISSING", "Blocking", "Dry-run summary non-claims are required.", "dry-run");

        if (summary.TotalRecordsScanned < 0 ||
            summary.TotalRecordsResolvable < 0 ||
            summary.TotalRecordsUnresolved < 0)
        {
            Add(findings, "READINESS_DRYRUN_METRICS_INVALID", "Blocking", "Dry-run summary totals must be non-negative.", "dry-run");
        }

        if (summary.TotalRecordsResolvable + summary.TotalRecordsUnresolved > summary.TotalRecordsScanned)
        {
            Add(findings, "READINESS_DRYRUN_TOTALS_INCONSISTENT", "Blocking", "Dry-run totals are inconsistent.", "dry-run");
        }
    }

    private static void ValidateGateResult(
        OwnershipBackfillGateResult gateResult,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings)
    {
        if (!gateResult.Passed)
            Add(findings, "READINESS_GATE_FAILED", "Blocking", "Gate result must be Passed=true.", "gate-result");

        if (gateResult.NonClaims.Count == 0)
            Add(findings, "READINESS_GATE_NONCLAIMS_MISSING", "Blocking", "Gate result non-claims are required.", "gate-result");

        if (gateResult.Findings.Any(finding => IsBlockingSeverity(finding.Severity)))
        {
            Add(findings, "READINESS_GATE_BLOCKING_FINDINGS", "Blocking", "Gate result contains blocking/error findings.", "gate-result");
        }
    }

    private static void ValidatePlan(
        OwnershipBackfillPlanResult plan,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings)
    {
        if (!string.Equals(plan.SummaryDraft.Mode, "PlanOnly", StringComparison.Ordinal))
            Add(findings, "READINESS_PLAN_MODE_INVALID", "Blocking", "Plan summary mode must be PlanOnly.", "plan", expected: "PlanOnly", actual: plan.SummaryDraft.Mode);

        if (string.IsNullOrWhiteSpace(plan.PlanHash))
            Add(findings, "READINESS_PLAN_HASH_MISSING", "Blocking", "Plan hash is required.", "plan");

        if (plan.NonClaims.Count == 0 || plan.SummaryDraft.NonClaims.Count == 0)
            Add(findings, "READINESS_PLAN_NONCLAIMS_MISSING", "Blocking", "Plan non-claims are required.", "plan");

        if (plan.PlannedRecords.Any(record => record.Reason.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase)))
            Add(findings, "READINESS_PLAN_AMBIGUOUS_RECORDS", "Blocking", "Plan contains ambiguous records.", "plan");

        if (plan.PlannedRecords.Any(record => !record.ProposedOrganizationId.HasValue || !record.ProposedProjectId.HasValue))
            Add(findings, "READINESS_PLAN_UNRESOLVED_RECORDS", "Blocking", "Plan contains unresolved records with missing proposed ownership identifiers.", "plan");
    }

    private static void ValidateSignoff(
        OwnershipBackfillPlanSignoffArtifact signoff,
        OwnershipBackfillPlanResult plan,
        OwnershipBackfillApplyReadinessOptions options,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
        IDictionary<string, string> metrics)
    {
        if (!string.Equals(signoff.PlanHash, plan.PlanHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(findings, "READINESS_SIGNOFF_PLANHASH_MISMATCH", "Blocking", "Signoff PlanHash must match plan PlanHash.", "signoff", expected: plan.PlanHash, actual: signoff.PlanHash);
        }

        if (string.IsNullOrWhiteSpace(signoff.Reviewer))
            Add(findings, "READINESS_SIGNOFF_REVIEWER_MISSING", "Blocking", "Signoff reviewer is required.", "signoff");

        if (string.IsNullOrWhiteSpace(signoff.Ticket))
            Add(findings, "READINESS_SIGNOFF_TICKET_MISSING", "Blocking", "Signoff ticket is required.", "signoff");

        if (!signoff.ConfirmationPhraseAccepted)
            Add(findings, "READINESS_SIGNOFF_CONFIRMATION_MISSING", "Blocking", "Signoff confirmation acceptance is required.", "signoff");

        if (signoff.NonClaims.Count == 0)
            Add(findings, "READINESS_SIGNOFF_NONCLAIMS_MISSING", "Blocking", "Signoff non-claims are required.", "signoff");

        if (signoff.ExpiresAtUtc.HasValue && signoff.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            Add(findings, "READINESS_SIGNOFF_EXPIRED", "Blocking", "Signoff is expired.", "signoff");

        var signoffAgeHours = Math.Max(0d, (DateTimeOffset.UtcNow - signoff.SignedAtUtc).TotalHours);
        metrics["SignoffAgeHours"] = signoffAgeHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        metrics["SignoffExpiresAtUtc"] = signoff.ExpiresAtUtc?.ToString("O") ?? "null";

        if (signoffAgeHours > options.MaxSignoffAgeHours)
        {
            Add(
                findings,
                "READINESS_SIGNOFF_TTL_EXCEEDED",
                "Blocking",
                "Signoff age exceeds configured TTL.",
                "signoff",
                expected: options.MaxSignoffAgeHours.ToString(System.Globalization.CultureInfo.InvariantCulture),
                actual: signoffAgeHours.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static void ValidateHashChain(
        OwnershipBackfillPlanResult plan,
        OwnershipBackfillPlanSignoffArtifact signoff,
        OwnershipBackfillApplyReadinessOptions options,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings)
    {
        if (!string.IsNullOrWhiteSpace(options.ExpectedPlanHash) &&
            !string.Equals(options.ExpectedPlanHash, plan.PlanHash, StringComparison.OrdinalIgnoreCase))
        {
            Add(findings, "READINESS_EXPECTED_PLANHASH_MISMATCH", "Blocking", "--expected-plan-hash does not match plan hash.", "plan", expected: options.ExpectedPlanHash, actual: plan.PlanHash);
        }

        if (!string.Equals(signoff.PlanHash, plan.PlanHash, StringComparison.OrdinalIgnoreCase))
            Add(findings, "READINESS_HASH_CHAIN_BROKEN", "Blocking", "Plan hash chain is inconsistent.", "hash-chain");
    }

    private static void ValidatePreviousValues(
        IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> previousValues,
        OwnershipBackfillPlanResult plan,
        bool requireRollbackReadiness,
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
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

        metrics["PreviousValuesPlannedCount"] = plannedKeys.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["PreviousValuesSnapshotCount"] = previousValues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metrics["PreviousValuesCompleteness"] = completeness.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        if (missingKeys.Length > 0)
        {
            var severity = requireRollbackReadiness ? "Blocking" : "Warning";
            Add(
                findings,
                "READINESS_PREVIOUS_VALUES_INCOMPLETE",
                severity,
                "Previous-values snapshot is incomplete for planned records.",
                "previous-values",
                expected: "100%",
                actual: $"{(completeness * 100d):0.###}%");
        }

        var conflictCount = 0;
        if (plan.SummaryDraft.SkippedByReason.TryGetValue("CurrentValueConflict", out var conflicts))
            conflictCount = conflicts;

        metrics["PlanCurrentValueConflictCount"] = conflictCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (requireRollbackReadiness && conflictCount > 0)
        {
            Add(findings, "READINESS_ROLLBACK_CONFLICTS_PRESENT", "Blocking", "Rollback readiness requires zero current value conflicts.", "plan", expected: "0", actual: conflictCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static string ComputeApplyInputHash(
        OwnershipBackfillDryRunSummary summary,
        OwnershipBackfillGateResult gateResult,
        OwnershipBackfillPlanResult plan,
        OwnershipBackfillPlanSignoffArtifact signoff,
        IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> previousValues,
        string rulesetVersion)
    {
        var canonical = new StringBuilder();
        canonical.Append("stage=P6-08;");
        canonical.Append("ruleset=").Append(rulesetVersion).Append(';');

        canonical.Append("dry-run:");
        canonical.Append(summary.RunId).Append('|')
            .Append(summary.Mode).Append('|')
            .Append(summary.TotalRecordsScanned).Append('|')
            .Append(summary.TotalRecordsResolvable).Append('|')
            .Append(summary.TotalRecordsUnresolved).Append(';');

        foreach (var unresolved in summary.UnresolvedByReason.OrderBy(item => item.Key, StringComparer.Ordinal))
            canonical.Append(unresolved.Key).Append('=').Append(unresolved.Value).Append(';');

        foreach (var metric in summary.RecordTypeMetrics.OrderBy(item => item.RecordType, StringComparer.Ordinal))
        {
            canonical.Append(metric.RecordType).Append('|')
                .Append(metric.TotalRecords).Append('|')
                .Append(metric.ResolvableRecords).Append('|')
                .Append(metric.UnresolvedRecords).Append('|')
                .Append(metric.AmbiguousRecords).Append(';');
        }

        canonical.Append("gate:");
        canonical.Append(gateResult.Passed).Append('|')
            .Append(gateResult.RunId).Append('|')
            .Append(gateResult.Summary).Append(';');

        foreach (var threshold in gateResult.Thresholds.OrderBy(item => item.Key, StringComparer.Ordinal))
            canonical.Append(threshold.Key).Append('=').Append(threshold.Value).Append(';');

        foreach (var finding in gateResult.Findings.OrderBy(item => item.Code, StringComparer.Ordinal))
        {
            canonical.Append(finding.Code).Append('|')
                .Append(finding.Severity).Append('|')
                .Append(finding.RecordType).Append('|')
                .Append(finding.Metric).Append('|')
                .Append(finding.Expected).Append('|')
                .Append(finding.Actual).Append(';');
        }

        canonical.Append("plan:");
        canonical.Append(plan.PlanId).Append('|')
            .Append(plan.PlanHash).Append('|')
            .Append(plan.RulesetVersion).Append('|')
            .Append(plan.SummaryDraft.Mode).Append(';');

        foreach (var record in plan.PlannedRecords
                     .OrderBy(item => item.RecordType, StringComparer.Ordinal)
                     .ThenBy(item => item.RecordId, StringComparer.Ordinal)
                     .ThenBy(item => item.DeterministicRecordHash, StringComparer.Ordinal))
        {
            canonical.Append(record.RecordType).Append('|')
                .Append(record.RecordId).Append('|')
                .Append(ToToken(record.CurrentProjectId)).Append('|')
                .Append(ToToken(record.CurrentBuildingId)).Append('|')
                .Append(ToToken(record.CurrentOrganizationId)).Append('|')
                .Append(ToToken(record.CurrentOwnerUserId)).Append('|')
                .Append(ToToken(record.ProposedProjectId)).Append('|')
                .Append(ToToken(record.ProposedBuildingId)).Append('|')
                .Append(ToToken(record.ProposedOrganizationId)).Append('|')
                .Append(ToToken(record.ProposedOwnerUserId)).Append('|')
                .Append(record.Reason).Append('|')
                .Append(record.DeterministicRecordHash).Append(';');
        }

        canonical.Append("signoff:");
        canonical.Append(signoff.SignoffId).Append('|')
            .Append(signoff.PlanId).Append('|')
            .Append(signoff.PlanHash).Append('|')
            .Append(signoff.Reviewer).Append('|')
            .Append(signoff.Ticket).Append('|')
            .Append(signoff.ConfirmationPhraseAccepted).Append('|')
            .Append(signoff.SignedAtUtc.ToString("O")).Append('|')
            .Append(signoff.ExpiresAtUtc?.ToString("O") ?? "null").Append('|')
            .Append(signoff.ToolStage).Append(';');

        canonical.Append("previous-values:");
        foreach (var snapshot in previousValues
                     .OrderBy(item => item.RecordType, StringComparer.Ordinal)
                     .ThenBy(item => item.RecordId, StringComparer.Ordinal))
        {
            canonical.Append(snapshot.RecordType).Append('|')
                .Append(snapshot.RecordId).Append('|')
                .Append(ToToken(snapshot.PreviousProjectId)).Append('|')
                .Append(ToToken(snapshot.PreviousBuildingId)).Append('|')
                .Append(ToToken(snapshot.PreviousOrganizationId)).Append('|')
                .Append(ToToken(snapshot.PreviousOwnerUserId)).Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
        return string.Equals(severity, "Blocking", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildReadinessId(string token)
    {
        var now = DateTimeOffset.UtcNow;
        var safeToken = string.IsNullOrWhiteSpace(token) ? "readiness" : token[..Math.Min(token.Length, 12)];
        return $"{now:yyyyMMddHHmmss}-{safeToken}";
    }

    private static string ToToken(int? value) =>
        value.HasValue
            ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null";

    private static void Add(
        ICollection<OwnershipBackfillApplyReadinessFinding> findings,
        string code,
        string severity,
        string message,
        string? artifact = null,
        string? expected = null,
        string? actual = null)
    {
        findings.Add(new OwnershipBackfillApplyReadinessFinding
        {
            Code = code,
            Severity = severity,
            Message = message,
            Artifact = artifact,
            Expected = expected,
            Actual = actual
        });
    }
}

