using AssistantEngineer.Tools.OwnershipBackfill.Cli;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCommandLineParserSimplificationTests
{
    [Fact]
    public void Parser_UsesDescriptorCatalogDispatch()
    {
        var parserSource = File.ReadAllText(ParserPath);

        Assert.Contains("OwnershipBackfillCommandDescriptorCatalog.TryGet", parserSource, StringComparison.Ordinal);
        Assert.Contains("CommandParsers.TryGetValue", parserSource, StringComparison.Ordinal);
        Assert.DoesNotContain("if (string.Equals(command, \"dry-run\"", parserSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_SupportsAllCatalogCommands()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        foreach (var descriptor in OwnershipBackfillCommandDescriptorCatalog.All)
        {
            var result = parser.Parse([descriptor.Name, "--help"]);
            Assert.True(result.IsSuccess, descriptor.Name);
            Assert.True(result.ShowHelp, descriptor.Name);
            Assert.Equal(descriptor.CommandType, result.CommandType);
        }
    }

    [Fact]
    public void Parser_DelegatesValueReadingToArgumentReader()
    {
        var parserSource = File.ReadAllText(ParserPath);
        Assert.Contains("OwnershipBackfillArgumentReader.TryReadValue", parserSource, StringComparison.Ordinal);
    }

    private static string ParserPath =>
        Path.Combine(
            GetRepoRoot(),
            "tools",
            "AssistantEngineer.Tools.OwnershipBackfill",
            "Cli",
            "OwnershipBackfillCommandLineParser.cs");

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "docs")) &&
                Directory.Exists(Path.Combine(current.FullName, "tools")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
