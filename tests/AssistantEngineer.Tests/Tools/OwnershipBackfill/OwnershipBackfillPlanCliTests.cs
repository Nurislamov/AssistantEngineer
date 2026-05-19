using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillPlanCliTests
{
    [Fact]
    public async Task PlanApply_WithPassingEvidence_ExitsZeroAndWritesPlanFiles()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateOutput = Path.Combine(root, "gate");
            var planOutput = Path.Combine(root, "plan");

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var dryRunExit = await cli.ExecuteAsync(["dry-run", "--output", evidenceDirectory], stdout, stderr, CancellationToken.None);
            Assert.Equal(0, dryRunExit);

            stdout.GetStringBuilder().Clear();
            stderr.GetStringBuilder().Clear();

            var gateExit = await cli.ExecuteAsync(["validate-evidence", "--input", evidenceDirectory, "--output", gateOutput], stdout, stderr, CancellationToken.None);
            Assert.Equal(0, gateExit);

            var gateJson = Directory.GetFiles(gateOutput, "ownership-backfill-evidence-gate-result-*.json").Single();
            stdout.GetStringBuilder().Clear();
            stderr.GetStringBuilder().Clear();

            var planExit = await cli.ExecuteAsync(
            [
                "plan-apply",
                "--evidence", evidenceDirectory,
                "--gate-result", gateJson,
                "--output", planOutput
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, planExit);
            Assert.NotEmpty(Directory.GetFiles(planOutput, "ownership-backfill-apply-plan-*.json"));
            Assert.NotEmpty(Directory.GetFiles(planOutput, "ownership-backfill-apply-summary-draft-*.json"));
            Assert.NotEmpty(Directory.GetFiles(planOutput, "ownership-backfill-apply-summary-draft-*.md"));
            Assert.NotEmpty(Directory.GetFiles(planOutput, "ownership-backfill-planned-records-*.json"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task PlanApply_WithFailedGate_ExitsTwo()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateResultPath = Path.Combine(root, "gate-result.json");

            await WriteSimpleEvidenceAsync(evidenceDirectory);
            await WriteGateResultAsync(gateResultPath, passed: false);

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "plan-apply",
                "--evidence", evidenceDirectory,
                "--gate-result", gateResultPath,
                "--output", Path.Combine(root, "plan")
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
    public async Task PlanApply_MissingEvidence_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["plan-apply", "--gate-result", "x", "--output", "y"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--evidence", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlanApply_MissingGateResult_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["plan-apply", "--evidence", "x", "--output", "y"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--gate-result", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlanApply_DoesNotEchoFakeSecret()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        const string fakeSecret = "Server=x;Password=TOP-SECRET-PLAN;";

        var exitCode = await cli.ExecuteAsync(
            ["plan-apply", "--evidence", "x", "--gate-result", "y", "--output", "z", "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var all = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_RemainsDisabled()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply", "--output", "tmp"], stdout, stderr, CancellationToken.None);

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

    private static async Task WriteSimpleEvidenceAsync(string evidenceDirectory)
    {
        Directory.CreateDirectory(evidenceDirectory);
        var runId = "20260518100101-test-run";

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "DryRun",
            TotalRecordsScanned = 0,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 0,
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

        var summaryPath = Path.Combine(evidenceDirectory, $"ownership-backfill-dry-run-summary-{runId}.json");
        var unresolvedPath = Path.Combine(evidenceDirectory, $"ownership-backfill-unresolved-records-{runId}.json");
        var previousPath = Path.Combine(evidenceDirectory, $"ownership-backfill-previous-values-{runId}.json");

        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary));
        await File.WriteAllTextAsync(unresolvedPath, "[]");
        await File.WriteAllTextAsync(previousPath, "[]");
    }

    private static async Task WriteGateResultAsync(string gateResultPath, bool passed)
    {
        var gate = new OwnershipBackfillGateResult
        {
            Passed = passed,
            Findings = [],
            Metrics = new Dictionary<string, string>(StringComparer.Ordinal),
            Summary = passed ? "Gate passed." : "Gate failed.",
            RunId = "gate-001",
            Thresholds = new Dictionary<string, string>(StringComparer.Ordinal),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(gateResultPath, JsonSerializer.Serialize(gate));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-plan-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
