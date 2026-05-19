using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillEvidenceGateEvaluator : IOwnershipBackfillEvidenceGateEvaluator
{
    private static readonly IReadOnlyList<string> RequiredRecordTypes =
    [
        "Project",
        "Building",
        "WorkflowState",
        "Scenario",
        "Job",
        "JobEvent",
        "ScenarioHistory"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> RecordTypeAliases =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Scenario"] = ["Scenario", "ScenarioRecord"],
            ["Job"] = ["Job", "JobRecord"],
            ["JobEvent"] = ["JobEvent", "JobEventRecord"],
            ["ScenarioHistory"] = ["ScenarioHistory", "ScenarioHistoryRecord"],
            ["Project"] = ["Project"],
            ["Building"] = ["Building"],
            ["WorkflowState"] = ["WorkflowState"]
        };

    public OwnershipBackfillGateResult Evaluate(
        OwnershipBackfillEvidenceBundle evidence,
        OwnershipBackfillGateOptions options)
    {
        var findings = new List<OwnershipBackfillGateFinding>();
        var metrics = new Dictionary<string, string>(StringComparer.Ordinal);
        var thresholds = BuildThresholdMap(options);

        var summary = evidence.Summary;
        if (!string.Equals(summary.Mode, OwnershipBackfillRunMode.DryRun.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            AddFinding(
                findings,
                code: "EVIDENCE_MODE_INVALID",
                severity: options.FailOnSchemaMismatch ? "Blocking" : "Warning",
                message: "Summary mode must be DryRun.",
                metric: "Mode",
                expected: "DryRun",
                actual: summary.Mode);
        }

        if (summary.TotalRecordsScanned < 0)
        {
            AddFinding(
                findings,
                "SUMMARY_NEGATIVE_TOTAL",
                options.FailOnSchemaMismatch ? "Blocking" : "Warning",
                "TotalRecordsScanned must be non-negative.",
                metric: "TotalRecordsScanned",
                expected: ">= 0",
                actual: summary.TotalRecordsScanned.ToString());
        }

        var countedTotal = summary.TotalRecordsResolvable + summary.TotalRecordsUnresolved;
        if (countedTotal > summary.TotalRecordsScanned)
        {
            AddFinding(
                findings,
                "SUMMARY_TOTAL_MISMATCH",
                options.FailOnSchemaMismatch ? "Blocking" : "Warning",
                "TotalRecordsResolvable + TotalRecordsUnresolved must not exceed TotalRecordsScanned.",
                metric: "SummaryTotals",
                expected: $"<= {summary.TotalRecordsScanned}",
                actual: countedTotal.ToString());
        }

        var metricsByCanonicalRecordType = ResolveMetrics(summary.RecordTypeMetrics, findings, options.FailOnMissingRecordTypeMetrics);
        var unresolvedRate = summary.TotalRecordsScanned == 0
            ? 0d
            : (double)summary.TotalRecordsUnresolved / summary.TotalRecordsScanned;

        metrics["TotalRecordsScanned"] = summary.TotalRecordsScanned.ToString();
        metrics["TotalRecordsResolvable"] = summary.TotalRecordsResolvable.ToString();
        metrics["TotalRecordsUnresolved"] = summary.TotalRecordsUnresolved.ToString();
        metrics["TotalUnresolvedRate"] = unresolvedRate.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

        if (unresolvedRate > options.MaxTotalUnresolvedRate)
        {
            AddFinding(
                findings,
                "TOTAL_UNRESOLVED_RATE_EXCEEDED",
                "Blocking",
                "Total unresolved rate exceeds configured threshold.",
                metric: "TotalUnresolvedRate",
                expected: $"<= {options.MaxTotalUnresolvedRate:0.######}",
                actual: unresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
        }

        ValidateRecordRate("Project", options.MaxProjectUnresolvedRate);
        ValidateRecordRate("Scenario", options.MaxScenarioUnresolvedRate);
        ValidateRecordRate("Job", options.MaxJobUnresolvedRate);

        var ambiguousFromMetrics = metricsByCanonicalRecordType.Values.Sum(metric => metric.AmbiguousRecords);
        metrics["AmbiguousRecords"] = ambiguousFromMetrics.ToString();
        if (ambiguousFromMetrics > options.MaxAmbiguousRecords)
        {
            AddFinding(
                findings,
                "AMBIGUOUS_COUNT_EXCEEDED",
                options.FailOnAmbiguousRecords ? "Blocking" : "Warning",
                "Ambiguous records exceed configured limit.",
                metric: "AmbiguousRecords",
                expected: $"<= {options.MaxAmbiguousRecords}",
                actual: ambiguousFromMetrics.ToString());
        }

        var unresolvedByReason = summary.UnresolvedByReason ?? new Dictionary<string, int>(StringComparer.Ordinal);
        var containsAmbiguousReason = unresolvedByReason.Keys.Any(key =>
            key.Contains("AmbiguousOwnership", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase));

        if (containsAmbiguousReason && options.FailOnAmbiguousRecords)
        {
            AddFinding(
                findings,
                "AMBIGUOUS_REASON_PRESENT",
                "Blocking",
                "Unresolved reasons include ambiguous ownership while FailOnAmbiguousRecords is enabled.",
                metric: "UnresolvedByReason",
                expected: "No ambiguous reason entries",
                actual: string.Join(", ", unresolvedByReason.Keys.Where(key => key.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase))));
        }

        ValidateNonClaims(summary, findings, options.FailOnSchemaMismatch);
        ValidateUnresolvedRecordFieldNames(evidence.UnresolvedRecordPropertyNames, findings, options.FailOnSchemaMismatch);

        var blockingFindings = findings.Count(finding =>
            string.Equals(finding.Severity, "Error", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(finding.Severity, "Blocking", StringComparison.OrdinalIgnoreCase));

        var passed = blockingFindings == 0;
        var resultSummary = passed
            ? "Ownership backfill evidence gate passed."
            : $"Ownership backfill evidence gate failed with {blockingFindings} blocking findings.";

        return new OwnershipBackfillGateResult
        {
            Passed = passed,
            Findings = findings,
            Metrics = metrics,
            Summary = resultSummary,
            RunId = summary.RunId,
            Thresholds = thresholds,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        void ValidateRecordRate(string recordType, double threshold)
        {
            if (!metricsByCanonicalRecordType.TryGetValue(recordType, out var metric))
                return;

            var unresolvedRateByRecordType = metric.TotalRecords == 0
                ? 0d
                : (double)metric.UnresolvedRecords / metric.TotalRecords;

            metrics[$"{recordType}.TotalRecords"] = metric.TotalRecords.ToString();
            metrics[$"{recordType}.UnresolvedRecords"] = metric.UnresolvedRecords.ToString();
            metrics[$"{recordType}.UnresolvedRate"] = unresolvedRateByRecordType.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

            if (unresolvedRateByRecordType > threshold)
            {
                AddFinding(
                    findings,
                    $"{recordType.ToUpperInvariant()}_UNRESOLVED_RATE_EXCEEDED",
                    "Blocking",
                    $"{recordType} unresolved rate exceeds configured threshold.",
                    recordType: recordType,
                    metric: $"{recordType}.UnresolvedRate",
                    expected: $"<= {threshold:0.######}",
                    actual: unresolvedRateByRecordType.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    private static Dictionary<string, OwnershipBackfillRecordTypeMetrics> ResolveMetrics(
        IReadOnlyList<OwnershipBackfillRecordTypeMetrics> sourceMetrics,
        ICollection<OwnershipBackfillGateFinding> findings,
        bool failOnMissing)
    {
        var result = new Dictionary<string, OwnershipBackfillRecordTypeMetrics>(StringComparer.Ordinal);

        foreach (var requiredType in RequiredRecordTypes)
        {
            var aliases = RecordTypeAliases[requiredType];
            var resolved = sourceMetrics.FirstOrDefault(metric => aliases.Contains(metric.RecordType, StringComparer.Ordinal));

            if (resolved is null)
            {
                AddFinding(
                    findings,
                    "REQUIRED_RECORD_TYPE_METRIC_MISSING",
                    failOnMissing ? "Blocking" : "Warning",
                    "Required record-type metric is missing.",
                    recordType: requiredType,
                    metric: "RecordTypeMetrics",
                    expected: requiredType,
                    actual: "Missing");

                continue;
            }

            result[requiredType] = resolved;
        }

        return result;
    }

    private static void ValidateNonClaims(
        OwnershipBackfillDryRunSummary summary,
        ICollection<OwnershipBackfillGateFinding> findings,
        bool failOnSchemaMismatch)
    {
        if (summary.NonClaims is null || summary.NonClaims.Count == 0)
        {
            AddFinding(
                findings,
                "NON_CLAIMS_MISSING",
                failOnSchemaMismatch ? "Blocking" : "Warning",
                "Dry-run summary must include non-claims.",
                metric: "NonClaims",
                expected: "Non-empty list",
                actual: "Missing");

            return;
        }

        foreach (var required in OwnershipBackfillConstants.NonClaims)
        {
            if (summary.NonClaims.Any(item => string.Equals(item, required, StringComparison.OrdinalIgnoreCase)))
                continue;

            AddFinding(
                findings,
                "NON_CLAIMS_INCOMPLETE",
                failOnSchemaMismatch ? "Blocking" : "Warning",
                "Dry-run summary non-claims list is missing a required statement.",
                metric: "NonClaims",
                expected: required,
                actual: "Missing");
        }
    }

    private static void ValidateUnresolvedRecordFieldNames(
        IReadOnlySet<string> propertyNames,
        ICollection<OwnershipBackfillGateFinding> findings,
        bool failOnSchemaMismatch)
    {
        if (propertyNames.Count == 0)
            return;

        var forbiddenFields = propertyNames
            .Where(name =>
                name.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("apiKey", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("requestJson", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("responseJson", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (forbiddenFields.Length == 0)
            return;

        AddFinding(
            findings,
            "UNRESOLVED_RECORD_FORBIDDEN_FIELDS",
            failOnSchemaMismatch ? "Blocking" : "Warning",
            "Unresolved record evidence contains forbidden payload-like fields.",
            metric: "UnresolvedRecordFields",
            expected: "No payload/token/secret fields",
            actual: string.Join(", ", forbiddenFields));
    }

    private static Dictionary<string, string> BuildThresholdMap(OwnershipBackfillGateOptions options)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MaxTotalUnresolvedRate"] = options.MaxTotalUnresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture),
            ["MaxProjectUnresolvedRate"] = options.MaxProjectUnresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture),
            ["MaxScenarioUnresolvedRate"] = options.MaxScenarioUnresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture),
            ["MaxJobUnresolvedRate"] = options.MaxJobUnresolvedRate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture),
            ["MaxAmbiguousRecords"] = options.MaxAmbiguousRecords.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["FailOnMissingRecordTypeMetrics"] = options.FailOnMissingRecordTypeMetrics.ToString(),
            ["FailOnAmbiguousRecords"] = options.FailOnAmbiguousRecords.ToString(),
            ["FailOnSchemaMismatch"] = options.FailOnSchemaMismatch.ToString()
        };
    }

    private static void AddFinding(
        ICollection<OwnershipBackfillGateFinding> findings,
        string code,
        string severity,
        string message,
        string? recordType = null,
        string? metric = null,
        string? expected = null,
        string? actual = null)
    {
        findings.Add(new OwnershipBackfillGateFinding
        {
            Code = code,
            Severity = severity,
            Message = message,
            RecordType = recordType,
            Metric = metric,
            Expected = expected,
            Actual = actual
        });
    }
}
