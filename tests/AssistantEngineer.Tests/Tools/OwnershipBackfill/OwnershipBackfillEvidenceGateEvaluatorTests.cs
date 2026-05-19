using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillEvidenceGateEvaluatorTests
{
    [Fact]
    public void ValidZeroCountDryRun_Passes()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var evidence = CreateBundle(CreateSummary());

        var result = evaluator.Evaluate(evidence, CreateOptions());

        Assert.True(result.Passed);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void MissingRequiredRecordTypeMetrics_Fails()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(metrics: [CreateMetric("Project")]);

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions());

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "REQUIRED_RECORD_TYPE_METRIC_MISSING");
    }

    [Fact]
    public void TotalUnresolvedRateAboveThreshold_Fails()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(
            totalScanned: 100,
            totalResolvable: 80,
            totalUnresolved: 20,
            metrics: BuildMetrics(("Project", 10, 8, 2, 0), ("Building", 90, 72, 18, 0)));

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions(maxTotalUnresolvedRate: 0.05d));

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "TOTAL_UNRESOLVED_RATE_EXCEEDED");
    }

    [Fact]
    public void ProjectUnresolvedRateAboveZeroThreshold_Fails()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var metrics = BuildMetrics(("Project", 1, 0, 1, 0));
        var summary = CreateSummary(totalScanned: 1, totalResolvable: 0, totalUnresolved: 1, metrics: metrics);

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions(maxProjectUnresolvedRate: 0d));

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "PROJECT_UNRESOLVED_RATE_EXCEEDED");
    }

    [Fact]
    public void AmbiguousRecords_FailByDefault()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(
            totalScanned: 1,
            totalResolvable: 0,
            totalUnresolved: 1,
            unresolvedByReason: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["AmbiguousOwnership"] = 1
            },
            metrics: BuildMetrics(("Scenario", 1, 0, 1, 1)));

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions(maxAmbiguousRecords: 0));

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "AMBIGUOUS_COUNT_EXCEEDED");
        Assert.Contains(result.Findings, finding => finding.Code == "AMBIGUOUS_REASON_PRESENT");
    }

    [Fact]
    public void MissingNonClaims_Fails()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(nonClaims: []);

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions());

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "NON_CLAIMS_MISSING");
    }

    [Fact]
    public void PayloadLikeUnresolvedFields_Fail()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var bundle = CreateBundle(CreateSummary(), unresolvedPropertyNames: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "recordType",
            "recordId",
            "payloadJson"
        });

        var result = evaluator.Evaluate(bundle, CreateOptions());

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "UNRESOLVED_RECORD_FORBIDDEN_FIELDS");
    }

    [Fact]
    public void ThresholdOverride_AllowsHigherUnresolvedRate()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(
            totalScanned: 10,
            totalResolvable: 7,
            totalUnresolved: 3,
            metrics: BuildMetrics(("Project", 10, 7, 3, 0)));

        var result = evaluator.Evaluate(
            CreateBundle(summary),
            CreateOptions(maxTotalUnresolvedRate: 0.50d, maxProjectUnresolvedRate: 0.50d));

        Assert.True(result.Passed);
    }

    [Fact]
    public void ModeMustBeDryRun_FailsOnMismatch()
    {
        var evaluator = new OwnershipBackfillEvidenceGateEvaluator();
        var summary = CreateSummary(mode: "Apply");

        var result = evaluator.Evaluate(CreateBundle(summary), CreateOptions());

        Assert.False(result.Passed);
        Assert.Contains(result.Findings, finding => finding.Code == "EVIDENCE_MODE_INVALID");
    }

    private static OwnershipBackfillEvidenceBundle CreateBundle(
        OwnershipBackfillDryRunSummary summary,
        IReadOnlySet<string>? unresolvedPropertyNames = null)
    {
        return new OwnershipBackfillEvidenceBundle
        {
            Summary = summary,
            UnresolvedRecords = [],
            PreviousValues = [],
            UnresolvedRecordPropertyNames = unresolvedPropertyNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "recordType",
                "recordId",
                "reason",
                "candidateProjectId",
                "candidateBuildingId",
                "candidateOrganizationId",
                "notes"
            }
        };
    }

    private static OwnershipBackfillGateOptions CreateOptions(
        double maxTotalUnresolvedRate = 0.05d,
        double maxProjectUnresolvedRate = 0d,
        int maxAmbiguousRecords = 0)
    {
        return new OwnershipBackfillGateOptions
        {
            EvidenceDirectory = "input",
            OutputDirectory = "output",
            MaxTotalUnresolvedRate = maxTotalUnresolvedRate,
            MaxProjectUnresolvedRate = maxProjectUnresolvedRate,
            MaxScenarioUnresolvedRate = 0.05d,
            MaxJobUnresolvedRate = 0.10d,
            MaxAmbiguousRecords = maxAmbiguousRecords,
            FailOnMissingRecordTypeMetrics = true,
            FailOnAmbiguousRecords = true,
            FailOnSchemaMismatch = true
        };
    }

    private static OwnershipBackfillDryRunSummary CreateSummary(
        string mode = "DryRun",
        int totalScanned = 0,
        int totalResolvable = 0,
        int totalUnresolved = 0,
        IReadOnlyDictionary<string, int>? unresolvedByReason = null,
        IReadOnlyList<OwnershipBackfillRecordTypeMetrics>? metrics = null,
        IReadOnlyList<string>? nonClaims = null)
    {
        return new OwnershipBackfillDryRunSummary
        {
            RunId = "20260518010101-test-run",
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = mode,
            TotalRecordsScanned = totalScanned,
            TotalRecordsResolvable = totalResolvable,
            TotalRecordsUnresolved = totalUnresolved,
            UnresolvedByReason = unresolvedByReason ?? new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = metrics ?? BuildMetrics(),
            NonClaims = nonClaims ?? OwnershipBackfillConstants.NonClaims
        };
    }

    private static IReadOnlyList<OwnershipBackfillRecordTypeMetrics> BuildMetrics(params (string recordType, int total, int resolvable, int unresolved, int ambiguous)[] overrides)
    {
        var map = overrides.ToDictionary(item => item.recordType, item => item, StringComparer.Ordinal);
        return
        [
            CreateMetric("Project", map),
            CreateMetric("Building", map),
            CreateMetric("WorkflowState", map),
            CreateMetric("Scenario", map),
            CreateMetric("Job", map),
            CreateMetric("JobEvent", map),
            CreateMetric("ScenarioHistory", map)
        ];
    }

    private static OwnershipBackfillRecordTypeMetrics CreateMetric(
        string recordType,
        IReadOnlyDictionary<string, (string recordType, int total, int resolvable, int unresolved, int ambiguous)>? map = null)
    {
        if (map is not null && map.TryGetValue(recordType, out var value))
        {
            return new OwnershipBackfillRecordTypeMetrics
            {
                RecordType = value.recordType,
                TotalRecords = value.total,
                ResolvableRecords = value.resolvable,
                UnresolvedRecords = value.unresolved,
                AmbiguousRecords = value.ambiguous,
                ResolvableRate = value.total == 0 ? 0d : (double)value.resolvable / value.total,
                UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
            };
        }

        return new OwnershipBackfillRecordTypeMetrics
        {
            RecordType = recordType,
            TotalRecords = 0,
            ResolvableRecords = 0,
            UnresolvedRecords = 0,
            AmbiguousRecords = 0,
            ResolvableRate = 0d,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }
}
