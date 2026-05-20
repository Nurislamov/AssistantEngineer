using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliExitCodeConsistencyTests
{
    [Fact]
    public async Task Help_ReturnsSuccessExitCode()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["--help"], stdout, stderr, CancellationToken.None);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task InvalidInput_ReturnsInvalidInputExitCode()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["validate-evidence", "--input", "x"], stdout, stderr, CancellationToken.None);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ValidationRejected_ReturnsValidationFailedExitCode()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-cli-exit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateOutput = Path.Combine(root, "gate");

            var cli = OwnershipBackfillCliTestFactory.Create();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            await WriteFailingEvidenceAsync(evidenceDirectory);

            var gateExit = await cli.ExecuteAsync(
            [
                "validate-evidence",
                "--input", evidenceDirectory,
                "--output", gateOutput
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(2, gateExit);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task AcceptedValidationCommand_ReturnsSuccessExitCode()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-cli-exit-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var evidenceDirectory = Path.Combine(root, "evidence");
            var gateOutput = Path.Combine(root, "gate");

            var cli = OwnershipBackfillCliTestFactory.Create();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var dryRunExit = await cli.ExecuteAsync(["dry-run", "--output", evidenceDirectory], stdout, stderr, CancellationToken.None);
            Assert.Equal(0, dryRunExit);

            var gateExit = await cli.ExecuteAsync(
                ["validate-evidence", "--input", evidenceDirectory, "--output", gateOutput],
                stdout,
                stderr,
                CancellationToken.None);

            Assert.Equal(0, gateExit);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyDisabled_ReturnsInvalidInputExitCode()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply", "--output", "tmp"], stdout, stderr, CancellationToken.None);
        Assert.Equal(1, exitCode);
    }

    private static async Task WriteFailingEvidenceAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        const string runId = "20260520000101-cli-exit";

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "DryRun",
            TotalRecordsScanned = 10,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 10,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal) { ["ProjectOrganizationMissing"] = 10 },
            RecordTypeMetrics = OwnershipBackfillConstants.KnownRecordTypes
                .Select(x => new OwnershipBackfillRecordTypeMetrics
                {
                    RecordType = x,
                    TotalRecords = x == "Project" ? 10 : 0,
                    ResolvableRecords = 0,
                    UnresolvedRecords = x == "Project" ? 10 : 0,
                    AmbiguousRecords = 0,
                    ResolvableRate = 0d,
                    UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
                }).ToArray(),
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        await File.WriteAllTextAsync(
            Path.Combine(directory, $"ownership-backfill-dry-run-summary-{runId}.json"),
            JsonSerializer.Serialize(summary));
        await File.WriteAllTextAsync(
            Path.Combine(directory, $"ownership-backfill-unresolved-records-{runId}.json"),
            "[]");
        await File.WriteAllTextAsync(
            Path.Combine(directory, $"ownership-backfill-previous-values-{runId}.json"),
            "[]");
    }
}
