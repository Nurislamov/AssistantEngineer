using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class OwnershipBackfillApplyWiringGuardTests
{
    [Fact]
    public void CliApplyCommandRemainsMappedToDisabledFlow()
    {
        var cli = File.ReadAllText(CliPath);

        GovernanceSourceScanHelper.AssertCliApplyDisabled(
            cli,
            "Apply mode is designed but disabled in P6-04. No ownership metadata was written.");
    }

    [Fact]
    public void CliDoesNotDependOnProductionApplyExecutorContracts()
    {
        var cli = File.ReadAllText(CliPath);

        Assert.DoesNotContain("IOwnershipBackfillApplyExecutor", cli, StringComparison.Ordinal);
        Assert.DoesNotContain("TestOnlyOwnershipBackfillApplyExecutor", cli, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyDisabledPathDoesNotInvokeStagingExecutor()
    {
        var cli = File.ReadAllText(CliPath);
        var applyMethod = GovernanceSourceScanHelper.ExtractMethodBody(
            cli,
            "private async Task<int> ExecuteApplyDisabledAsync");

        Assert.DoesNotContain("DisabledStagingOwnershipBackfillApplyExecutor", applyMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteAsync(", applyMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolSourceContainsNoWriteWiringPatterns()
    {
        GovernanceSourceScanHelper.AssertNoWritePatterns(
            ToolDirectoryPath,
            additionalForbiddenPatterns: ["HasQueryFilter"]);
    }

    [Fact]
    public void TestOnlyExecutorIsNotWiredToCliApply()
    {
        var toolSourceFiles = Directory.GetFiles(ToolDirectoryPath, "*.cs", SearchOption.AllDirectories);
        var references = toolSourceFiles
            .Where(path => !path.EndsWith("TestOnlyOwnershipBackfillApplyExecutor.cs", StringComparison.Ordinal))
            .Select(path => (Path: path, Content: File.ReadAllText(path)))
            .Where(pair => pair.Content.Contains("TestOnlyOwnershipBackfillApplyExecutor", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(references);
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");
}
