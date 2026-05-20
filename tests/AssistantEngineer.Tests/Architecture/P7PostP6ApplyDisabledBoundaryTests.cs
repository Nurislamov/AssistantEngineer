using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7PostP6ApplyDisabledBoundaryTests
{
    [Fact]
    public async Task ApplyCommandStillReturnsNonZeroAndDisabledMessage()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        const string fakeConnection = "Data Source=fake.db;Password=DO-NOT-LEAK;";
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
            "--connection-string", fakeConnection
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.Equal(1, exitCode);
        var output = stdout + Environment.NewLine + stderr;
        Assert.Contains("disabled", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fakeConnection, output, StringComparison.Ordinal);
    }

    [Fact]
    public void CliApplyPathIsExplicitlyDisabledAndNotWiredToExecutors()
    {
        var cliSource = File.ReadAllText(CliPath);
        var constructorParameters = typeof(OwnershipBackfillCli).GetConstructors().Single().GetParameters();

        GovernanceSourceScanHelper.AssertCliApplyDisabled(
            cliSource,
            "Apply mode is designed but disabled in P6-04. No ownership metadata was written.");

        Assert.DoesNotContain(constructorParameters, parameter =>
            string.Equals(parameter.ParameterType.Name, "IOwnershipBackfillApplyExecutor", StringComparison.Ordinal));

        var applyMethodBody = GovernanceSourceScanHelper.ExtractMethodBody(
            cliSource,
            "private async Task<int> ExecuteApplyDisabledAsync");
        Assert.DoesNotContain("TestOnlyOwnershipBackfillApplyExecutor", applyMethodBody, StringComparison.Ordinal);
        Assert.DoesNotContain("DisabledStagingOwnershipBackfillApplyExecutor", applyMethodBody, StringComparison.Ordinal);
    }

    [Fact]
    public void OwnershipBackfillToolSourceContainsNoWritePatterns()
    {
        GovernanceSourceScanHelper.AssertNoWritePatterns(ToolDirectoryPath);
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

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");
}
