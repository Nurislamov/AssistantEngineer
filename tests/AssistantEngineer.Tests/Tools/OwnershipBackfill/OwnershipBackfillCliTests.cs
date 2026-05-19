using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliTests
{
    [Fact]
    public async Task DryRun_WithOutput_ExitsZeroAndCreatesEvidenceFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "ae-backfill-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var outputDirectory = Path.Combine(root, "dry-run-output");
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
                ["dry-run", "--output", outputDirectory],
                stdout,
                stderr,
                CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(outputDirectory));
            Assert.NotEmpty(Directory.GetFiles(outputDirectory, "ownership-backfill-dry-run-summary-*.json"));
            Assert.NotEmpty(Directory.GetFiles(outputDirectory, "ownership-backfill-dry-run-summary-*.md"));
            Assert.NotEmpty(Directory.GetFiles(outputDirectory, "ownership-backfill-unresolved-records-*.json"));
            Assert.NotEmpty(Directory.GetFiles(outputDirectory, "ownership-backfill-previous-values-*.json"));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyCommand_ExitsNonZero()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply", "--output", "tmp"], stdout, stderr, CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Help_ExitsZero()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["--help"], stdout, stderr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionString_IsNotEchoedToOutput()
    {
        const string fakeSecret = "Password=TOP-SECRET-ABC";

        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["dry-run", "--output", "x", "--connection-string", fakeSecret, "--unknown-option"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.NotEqual(0, exitCode);
        var merged = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, merged, StringComparison.Ordinal);
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
}


