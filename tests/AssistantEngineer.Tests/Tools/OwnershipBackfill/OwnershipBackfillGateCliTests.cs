using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillGateCliTests
{
    [Fact]
    public async Task ValidateEvidence_OnPassingEvidence_ExitsZeroAndWritesGateOutputs()
    {
        var root = CreateTempDirectory();
        var dryRunOutput = Path.Combine(root, "dry-run");
        var gateOutput = Path.Combine(root, "gate");

        try
        {
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var dryRunExit = await cli.ExecuteAsync(["dry-run", "--output", dryRunOutput], stdout, stderr, CancellationToken.None);
            Assert.Equal(0, dryRunExit);

            stdout.GetStringBuilder().Clear();
            stderr.GetStringBuilder().Clear();

            var gateExit = await cli.ExecuteAsync(
                ["validate-evidence", "--input", dryRunOutput, "--output", gateOutput],
                stdout,
                stderr,
                CancellationToken.None);

            Assert.Equal(0, gateExit);
            Assert.NotEmpty(Directory.GetFiles(gateOutput, "ownership-backfill-evidence-gate-result-*.json"));
            Assert.NotEmpty(Directory.GetFiles(gateOutput, "ownership-backfill-evidence-gate-result-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateEvidence_OnFailingEvidence_ExitsTwo()
    {
        var root = CreateTempDirectory();
        var gateOutput = Path.Combine(root, "gate");

        try
        {
            await CreateFailingEvidenceAsync(root);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
                ["validate-evidence", "--input", root, "--output", gateOutput],
                stdout,
                stderr,
                CancellationToken.None);

            Assert.Equal(2, exitCode);
            Assert.Contains("failed", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateEvidence_MissingInput_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["validate-evidence", "--output", "x"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("--input", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateEvidence_DoesNotEchoConnectionStringSecret()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        const string fakeSecret = "Host=127.0.0.1;Password=TOP-SECRET-GATE;";

        var exitCode = await cli.ExecuteAsync(
            ["validate-evidence", "--input", "missing", "--output", "x", "--summary", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var all = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_IsStillRejected()
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

    private static async Task CreateFailingEvidenceAsync(string directory)
    {
        var runId = "20260518030303-test-run";
        Directory.CreateDirectory(directory);

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "DryRun",
            TotalRecordsScanned = 10,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 10,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["ProjectOrganizationMissing"] = 10
            },
            RecordTypeMetrics =
            [
                CreateMetric("Project", 10, 0, 10, 0),
                CreateMetric("Building", 0, 0, 0, 0),
                CreateMetric("WorkflowState", 0, 0, 0, 0),
                CreateMetric("Scenario", 0, 0, 0, 0),
                CreateMetric("Job", 0, 0, 0, 0),
                CreateMetric("JobEvent", 0, 0, 0, 0),
                CreateMetric("ScenarioHistory", 0, 0, 0, 0)
            ],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        var summaryPath = Path.Combine(directory, $"ownership-backfill-dry-run-summary-{runId}.json");
        var unresolvedPath = Path.Combine(directory, $"ownership-backfill-unresolved-records-{runId}.json");
        var previousValuesPath = Path.Combine(directory, $"ownership-backfill-previous-values-{runId}.json");

        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(unresolvedPath, "[]");
        await File.WriteAllTextAsync(previousValuesPath, "[]");
    }

    private static OwnershipBackfillRecordTypeMetrics CreateMetric(string recordType, int total, int resolvable, int unresolved, int ambiguous)
    {
        return new OwnershipBackfillRecordTypeMetrics
        {
            RecordType = recordType,
            TotalRecords = total,
            ResolvableRecords = resolvable,
            UnresolvedRecords = unresolved,
            AmbiguousRecords = ambiguous,
            ResolvableRate = total == 0 ? 0d : (double)resolvable / total,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-gate-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}

