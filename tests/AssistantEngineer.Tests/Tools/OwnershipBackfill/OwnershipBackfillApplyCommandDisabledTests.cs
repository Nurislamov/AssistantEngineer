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

public sealed class OwnershipBackfillApplyCommandDisabledTests
{
    [Fact]
    public async Task ApplyCommand_ReturnsNonZero_WhenCalled()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply"], stdout, stderr, CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCommand_WithEnableApply_StillReturnsNonZero()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDir = Path.Combine(root, "dry-run");
            await CreatePassingEvidenceBundleAsync(evidenceDir);
            var gatePath = await CreateGateResultAsync(root, passed: true);
            var dbPath = Path.Combine(root, "should-not-be-created.db");

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "apply",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
                "--evidence", evidenceDir,
                "--gate-result", gatePath,
                "--output", Path.Combine(root, "apply-out"),
                "--database-provider", "SQLite",
                "--connection-string", $"Data Source={dbPath}"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.Contains("disabled", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(dbPath));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ApplyCommand_WithCorrectConfirmation_StillReturnsNonZero()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
        [
            "apply",
            "--enable-apply",
            "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCommand_DoesNotEchoConnectionString()
    {
        const string fakeSecret = "Data Source=fake.db;Password=TOP-SECRET-APPLY;";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
        [
            "apply",
            "--connection-string", fakeSecret,
            "--enable-apply"
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        var text = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_DoesNotCreateExecutedApplySummaryArtifacts()
    {
        var root = CreateTempDirectory();

        try
        {
            var evidenceDir = Path.Combine(root, "dry-run");
            await CreatePassingEvidenceBundleAsync(evidenceDir);
            var gatePath = await CreateGateResultAsync(root, passed: true);
            var applyOutput = Path.Combine(root, "apply-out");

            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "apply",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
                "--evidence", evidenceDir,
                "--gate-result", gatePath,
                "--output", applyOutput,
                "--database-provider", "SQLite",
                "--connection-string", "Data Source=fake.db"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.False(Directory.Exists(applyOutput) && Directory.GetFiles(applyOutput, "ownership-backfill-apply-summary-*.json").Length > 0);
        }
        finally
        {
            DeleteDirectory(root);
        }
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

    private static async Task CreatePassingEvidenceBundleAsync(string evidenceDirectory)
    {
        Directory.CreateDirectory(evidenceDirectory);
        var runId = "20260518050505-test-run";

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
            RecordTypeMetrics =
            [
                CreateMetric("Project"),
                CreateMetric("Building"),
                CreateMetric("WorkflowState"),
                CreateMetric("Scenario"),
                CreateMetric("Job"),
                CreateMetric("JobEvent"),
                CreateMetric("ScenarioHistory")
            ],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, $"ownership-backfill-dry-run-summary-{runId}.json"), JsonSerializer.Serialize(summary));
    }

    private static async Task<string> CreateGateResultAsync(string root, bool passed)
    {
        var gate = new
        {
            Passed = passed,
            Findings = Array.Empty<object>(),
            Metrics = new Dictionary<string, string>(),
            Summary = "summary"
        };

        var gatePath = Path.Combine(root, "gate-result.json");
        await File.WriteAllTextAsync(gatePath, JsonSerializer.Serialize(gate));
        return gatePath;
    }

    private static OwnershipBackfillRecordTypeMetrics CreateMetric(string recordType)
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
        var path = Path.Combine(Path.GetTempPath(), $"ae-apply-disabled-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
