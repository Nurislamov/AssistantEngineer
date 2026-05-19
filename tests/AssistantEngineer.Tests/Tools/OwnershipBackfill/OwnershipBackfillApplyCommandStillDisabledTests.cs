using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyCommandStillDisabledTests
{
    [Fact]
    public async Task ApplyCommandStillReturnsDisabledNonZero()
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
            "--gate-result", "missing.json",
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
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCommandWithPlanAndSignoffStillDisabled()
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
            "--gate-result", "missing.json",
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
        var allText = stdout.ToString() + Environment.NewLine + stderr;
        Assert.Contains("Apply mode is designed but disabled", allText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyCommandDoesNotEchoConnectionString()
    {
        const string fakeSecret = "Data Source=fake.db;Password=TOP-SECRET;";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
        [
            "apply",
            "--enable-apply",
            "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
            "--evidence", ".",
            "--gate-result", "missing.json",
            "--plan", "missing-plan.json",
            "--plan-signoff", "missing-signoff.json",
            "--output", ".",
            "--database-provider", "SQLite",
            "--connection-string", fakeSecret
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.Equal(1, exitCode);
        var allText = stdout.ToString() + Environment.NewLine + stderr;
        Assert.DoesNotContain(fakeSecret, allText, StringComparison.Ordinal);
    }

    [Fact]
    public void CliConstructorDoesNotDependOnApplyExecutor()
    {
        var constructorParameters = typeof(OwnershipBackfillCli).GetConstructors().Single().GetParameters();
        Assert.DoesNotContain(constructorParameters, parameter =>
            string.Equals(parameter.ParameterType.Name, "IOwnershipBackfillApplyExecutor", StringComparison.Ordinal));
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
