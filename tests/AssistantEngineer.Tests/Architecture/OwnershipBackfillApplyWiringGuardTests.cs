namespace AssistantEngineer.Tests.Architecture;

public sealed class OwnershipBackfillApplyWiringGuardTests
{
    [Fact]
    public void CliApplyCommandRemainsMappedToDisabledFlow()
    {
        var cli = File.ReadAllText(CliPath);

        Assert.Contains("OwnershipBackfillCommandType.Apply => await ExecuteApplyDisabledAsync", cli, StringComparison.Ordinal);
        Assert.Contains("Apply mode is designed but disabled in P6-04. No ownership metadata was written.", cli, StringComparison.Ordinal);
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
        var applyMethod = ExtractMethodBody(cli, "ExecuteApplyDisabledAsync");

        Assert.DoesNotContain("DisabledStagingOwnershipBackfillApplyExecutor", applyMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteAsync(", applyMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolSourceContainsNoWriteWiringPatterns()
    {
        var sourceFiles = Directory.GetFiles(ToolDirectoryPath, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sourceFiles);

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("SaveChanges(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChangesAsync(", content, StringComparison.Ordinal);
            Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT INTO", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HasQueryFilter", content, StringComparison.Ordinal);
        }
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

    private static string ExtractMethodBody(string fileContent, string methodName)
    {
        var marker = $"private async Task<int> {methodName}";
        var startIndex = fileContent.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Method marker not found: {methodName}");

        var bodyStart = fileContent.IndexOf('{', startIndex);
        Assert.True(bodyStart >= 0, $"Method body start not found: {methodName}");

        var depth = 0;
        for (var i = bodyStart; i < fileContent.Length; i++)
        {
            if (fileContent[i] == '{')
                depth++;
            else if (fileContent[i] == '}')
                depth--;

            if (depth == 0)
                return fileContent.Substring(bodyStart, i - bodyStart + 1);
        }

        throw new InvalidOperationException($"Could not parse method body for: {methodName}");
    }

    private static string ToolDirectoryPath =>
        Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    private static string CliPath =>
        Path.Combine(ToolDirectoryPath, "Cli", "OwnershipBackfillCli.cs");
}
