using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingAcceptanceValidatorTests
{
    [Fact]
    public async Task ValidCompleteEvidence_IsAccepted()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(0, validation.ExitCode);
            Assert.True(validation.Result.Accepted);
            Assert.False(string.IsNullOrWhiteSpace(validation.Result.StagingRunHash));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingApplyResult_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, applyResultPath: Path.Combine(root, "missing.json"));

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.False(validation.Result.Accepted);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailedApplyResult_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, applySucceeded: false);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.False(validation.Result.Accepted);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_APPLY_NOT_SUCCEEDED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task FailedRecordsAboveZero_IsRejectedWhenRequired()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, failedRecords: 2);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.False(validation.Result.Accepted);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_FAILED_RECORDS_PRESENT");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PostApplyUnresolvedRateAboveThreshold_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, dryRunUnresolved: 2, dryRunScanned: 10);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, maxPostApplyUnresolvedRate: 0.01d);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_POST_UNRESOLVED_RATE_EXCEEDED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PostApplyGateFailed_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root, gatePassed: false);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();

            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_POST_GATE_FAILED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingTenantIsolationReference_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, tenantIsolationReference: null);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_TENANT_ISOLATION_REFERENCE_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingRegressionReference_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, regressionReference: null);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_REGRESSION_REFERENCE_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingRollbackReference_IsRejectedWhenRequired()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, rollbackReference: null, requireRollbackEvidence: true);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_ROLLBACK_REFERENCE_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingOperator_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, operatorId: null);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_OPERATOR_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task MissingStagingChangeId_IsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var options = CreateOptions(root, artifacts, stagingChangeId: null);

            var validation = await validator.ValidateAsync(options);

            Assert.Equal(1, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_CHANGE_ID_REQUIRED");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task SecretOrPayloadFields_AreRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            await File.WriteAllTextAsync(
                artifacts.ApplyResultPath,
                """{"succeeded":true,"executionId":"execution-001","mode":"TestOnly","totalRecordsPlanned":1,"totalRecordsUpdated":1,"totalRecordsSkipped":0,"totalRecordsFailed":0,"findings":[],"previousValues":[],"nonClaims":["No ownership backfill execution claim."],"payloadData":{"x":1}}""");

            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.Equal(2, validation.ExitCode);
            Assert.Contains(validation.Result.Findings, finding => finding.Code == "STAGING_ACCEPTANCE_FORBIDDEN_FIELD");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StagingRunHash_IsGenerated()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteValidArtifactsAsync(root);
            var validator = new OwnershipBackfillStagingAcceptanceValidator();
            var validation = await validator.ValidateAsync(CreateOptions(root, artifacts));

            Assert.False(string.IsNullOrWhiteSpace(validation.Result.StagingRunHash));
            Assert.True(validation.Result.Metrics.ContainsKey("StagingRunHash"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillStagingAcceptanceOptions CreateOptions(
        string root,
        ArtifactPaths artifacts,
        string? applyResultPath = null,
        string? tenantIsolationReference = "tenant-matrix-pass-ref",
        string? regressionReference = "regression-pass-ref",
        string? rollbackReference = "rollback-ready-ref",
        string? operatorId = "local-operator",
        string? stagingChangeId = "staging-change-001",
        bool requireRollbackEvidence = true,
        double maxPostApplyUnresolvedRate = 0.01d)
    {
        return new OwnershipBackfillStagingAcceptanceOptions
        {
            ApplyResultPath = applyResultPath ?? artifacts.ApplyResultPath,
            PostApplyDryRunSummaryPath = artifacts.PostApplyDryRunPath,
            PostApplyGateResultPath = artifacts.PostApplyGatePath,
            TenantIsolationMatrixResultReference = tenantIsolationReference,
            RegressionTestResultReference = regressionReference,
            RollbackEvidenceReference = rollbackReference,
            ApplyInputHash = "apply-input-hash-001",
            PlanHash = "plan-hash-001",
            SignoffId = "signoff-001",
            ReadinessId = "readiness-001",
            StagingPreflightReference = "staging-preflight-001",
            OperatorId = operatorId,
            StagingChangeId = stagingChangeId,
            OutputDirectory = Path.Combine(root, "acceptance-out"),
            RulesetVersion = "P6-12",
            MaxPostApplyUnresolvedRate = maxPostApplyUnresolvedRate,
            RequireZeroFailedRecords = true,
            RequireRollbackEvidence = requireRollbackEvidence,
            RequireTenantIsolationPass = true,
            RequireRegressionPass = true
        };
    }

    private static async Task<ArtifactPaths> WriteValidArtifactsAsync(
        string root,
        bool applySucceeded = true,
        int failedRecords = 0,
        int dryRunUnresolved = 0,
        int dryRunScanned = 10,
        bool gatePassed = true)
    {
        var applyResultPath = Path.Combine(root, "apply-result.json");
        var postApplyDryRunPath = Path.Combine(root, "post-apply-dry-run.json");
        var postApplyGatePath = Path.Combine(root, "post-apply-gate.json");

        var applyResult = new OwnershipBackfillApplyExecutionResult
        {
            Succeeded = applySucceeded,
            ExecutionId = "execution-001",
            Mode = "TestOnly",
            TotalRecordsPlanned = 10,
            TotalRecordsUpdated = applySucceeded ? 10 - failedRecords : 0,
            TotalRecordsSkipped = 0,
            TotalRecordsFailed = failedRecords,
            Findings = [],
            PreviousValues = [],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var dryRunSummary = new OwnershipBackfillDryRunSummary
        {
            RunId = "post-run-001",
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            Mode = "DryRun",
            TotalRecordsScanned = dryRunScanned,
            TotalRecordsResolvable = Math.Max(0, dryRunScanned - dryRunUnresolved),
            TotalRecordsUnresolved = dryRunUnresolved,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(CreateZeroMetric)
                .ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var gateResult = new OwnershipBackfillGateResult
        {
            Passed = gatePassed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = gatePassed ? "post gate passed" : "post gate failed",
            RunId = "gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["MaxTotalUnresolvedRate"] = "0.01"
            },
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(applyResultPath, JsonSerializer.Serialize(applyResult));
        await File.WriteAllTextAsync(postApplyDryRunPath, JsonSerializer.Serialize(dryRunSummary));
        await File.WriteAllTextAsync(postApplyGatePath, JsonSerializer.Serialize(gateResult));

        return new ArtifactPaths(applyResultPath, postApplyDryRunPath, postApplyGatePath);
    }

    private static OwnershipBackfillRecordTypeMetrics CreateZeroMetric(string recordType)
    {
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-staging-acceptance-validator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed record ArtifactPaths(string ApplyResultPath, string PostApplyDryRunPath, string PostApplyGatePath);
}
