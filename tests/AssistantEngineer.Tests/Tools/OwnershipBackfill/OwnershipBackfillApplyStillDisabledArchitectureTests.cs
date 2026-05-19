using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyStillDisabledArchitectureTests
{
    [Fact]
    public async Task ApplyCommandWithEnableFlagsStillReturnsNonZeroAndDisabledMessage()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        const string fakeConnection = "Data Source=fake.db;Password=SHOULD-NOT-LEAK;";
        var outputPath = CreateTempDirectory();

        try
        {
            var exitCode = await cli.ExecuteAsync(
            [
                "apply",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
                "--evidence", ".",
                "--gate-result", "missing-gate.json",
                "--plan", "missing-plan.json",
                "--plan-signoff", "missing-signoff.json",
                "--output", outputPath,
                "--database-provider", "SQLite",
                "--connection-string", fakeConnection,
                "--batch-size", "500"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            var combined = stdout + Environment.NewLine + stderr;
            Assert.Contains("Apply mode is designed but disabled", combined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(fakeConnection, combined, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyCommandDoesNotProduceApplyArtifacts()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var outputPath = CreateTempDirectory();
        try
        {
            var exitCode = await cli.ExecuteAsync(
            [
                "apply",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
                "--evidence", ".",
                "--gate-result", "missing-gate.json",
                "--plan", "missing-plan.json",
                "--plan-signoff", "missing-signoff.json",
                "--output", outputPath,
                "--database-provider", "SQLite",
                "--connection-string", "Data Source=fake.db"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(1, exitCode);
            var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
            Assert.Empty(files);
        }
        finally
        {
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyCommandOutputDoesNotIndicateStagingOrProductionExecutorInvocation()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
        [
            "apply",
            "--enable-apply",
            "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
            "--evidence", ".",
            "--gate-result", "missing-gate.json",
            "--plan", "missing-plan.json",
            "--plan-signoff", "missing-signoff.json",
            "--output", ".",
            "--database-provider", "SQLite",
            "--connection-string", "Data Source=fake.db"
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.Equal(1, exitCode);
        var combined = stdout + Environment.NewLine + stderr;
        Assert.DoesNotContain("Staging apply executor is designed but disabled", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Production apply remains disabled in this stage", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-apply-arch-disabled-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
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
