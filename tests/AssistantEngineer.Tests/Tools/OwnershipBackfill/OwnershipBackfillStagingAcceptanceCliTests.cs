using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingAcceptanceCliTests
{
    [Fact]
    public async Task ValidateStagingAcceptance_ValidEvidence_ExitsZero()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteArtifactsAsync(root, applySucceeded: true, failedRecords: 0, unresolvedCount: 0, gatePassed: true);
            var output = Path.Combine(root, "acceptance-out");
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-staging-acceptance",
                "--apply-result", artifacts.ApplyResultPath,
                "--post-apply-dry-run", artifacts.PostApplyDryRunPath,
                "--post-apply-gate-result", artifacts.PostApplyGatePath,
                "--tenant-isolation-result", "tenant-ref",
                "--regression-result", "regression-ref",
                "--rollback-evidence", "rollback-ref",
                "--apply-input-hash", "apply-hash-001",
                "--plan-hash", "plan-hash-001",
                "--signoff-id", "signoff-001",
                "--readiness-id", "readiness-001",
                "--staging-preflight", "preflight-001",
                "--operator", "operator-001",
                "--staging-change-id", "change-001",
                "--output", output
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-staging-acceptance-result-*.json"));
            Assert.NotEmpty(Directory.GetFiles(output, "ownership-backfill-staging-acceptance-result-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateStagingAcceptance_RejectedEvidence_ExitsTwo()
    {
        var root = CreateTempDirectory();
        try
        {
            var artifacts = await WriteArtifactsAsync(root, applySucceeded: false, failedRecords: 1, unresolvedCount: 2, gatePassed: false);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-staging-acceptance",
                "--apply-result", artifacts.ApplyResultPath,
                "--post-apply-dry-run", artifacts.PostApplyDryRunPath,
                "--post-apply-gate-result", artifacts.PostApplyGatePath,
                "--tenant-isolation-result", "tenant-ref",
                "--regression-result", "regression-ref",
                "--rollback-evidence", "rollback-ref",
                "--apply-input-hash", "apply-hash-001",
                "--plan-hash", "plan-hash-001",
                "--signoff-id", "signoff-001",
                "--readiness-id", "readiness-001",
                "--staging-preflight", "preflight-001",
                "--operator", "operator-001",
                "--staging-change-id", "change-001",
                "--output", Path.Combine(root, "acceptance-out")
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(2, exitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateStagingAcceptance_InvalidInput_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-staging-acceptance", "--apply-result", "x"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--post-apply-dry-run", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateStagingAcceptance_DoesNotEchoSecrets()
    {
        const string fakeSecret = "TOP-SECRET-CONNECTION";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-staging-acceptance", "--apply-result", "x", "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var combined = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_RemainsDisabled()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static OwnershipBackfillCli CreateCli()
    {
        return new OwnershipBackfillCli(
            new OwnershipBackfillCommandLineParser(),
            new OwnershipBackfillEvidenceWriter(),
            new NoDataOwnershipBackfillDryRunScanner(),
            new DatabaseOwnershipBackfillDryRunScanner(),
            new OwnershipBackfillEvidenceLoader(),
            new OwnershipBackfillEvidenceGateEvaluator(),
            new OwnershipBackfillGateResultWriter(),
            new OwnershipBackfillApplyPreconditionValidator(),
            new OwnershipBackfillApplyPlanGenerator(),
            new OwnershipBackfillApplyPlanWriter(),
            new OwnershipBackfillPlanSignoffValidator(),
            new OwnershipBackfillPlanSignoffWriter());
    }

    private static async Task<(string ApplyResultPath, string PostApplyDryRunPath, string PostApplyGatePath)> WriteArtifactsAsync(
        string root,
        bool applySucceeded,
        int failedRecords,
        int unresolvedCount,
        bool gatePassed)
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
            TotalRecordsScanned = 10,
            TotalRecordsResolvable = 10 - unresolvedCount,
            TotalRecordsUnresolved = unresolvedCount,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(recordType => new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = recordType,
                    TotalRecords = 0,
                    ResolvableRecords = 0,
                    UnresolvedRecords = 0,
                    AmbiguousRecords = 0,
                    ResolvableRate = 0d,
                    UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
                })
                .ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var gateResult = new OwnershipBackfillGateResult
        {
            Passed = gatePassed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = gatePassed ? "passed" : "failed",
            RunId = "gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(applyResultPath, JsonSerializer.Serialize(applyResult));
        await File.WriteAllTextAsync(postApplyDryRunPath, JsonSerializer.Serialize(dryRunSummary));
        await File.WriteAllTextAsync(postApplyGatePath, JsonSerializer.Serialize(gateResult));

        return (applyResultPath, postApplyDryRunPath, postApplyGatePath);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-staging-acceptance-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
