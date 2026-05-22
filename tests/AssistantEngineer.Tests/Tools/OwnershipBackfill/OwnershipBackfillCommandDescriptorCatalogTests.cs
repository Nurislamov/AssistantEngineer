using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCommandDescriptorCatalogTests
{
    [Fact]
    public void EveryCommandType_HasDescriptor()
    {
        foreach (var commandType in Enum.GetValues<OwnershipBackfillCommandType>())
        {
            var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(commandType);
            Assert.Equal(commandType, descriptor.CommandType);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Name));
        }
    }

    [Fact]
    public void InventoryCommands_ArePresentInDescriptorCatalog()
    {
        var descriptorNames = OwnershipBackfillCommandDescriptorCatalog.All
            .Select(descriptor => descriptor.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var command in LoadInventoryCommandNames())
            Assert.Contains(command, descriptorNames);
    }

    [Fact]
    public void Descriptors_DefineArgumentsAndUsageSummary()
    {
        foreach (var descriptor in OwnershipBackfillCommandDescriptorCatalog.All)
        {
            Assert.True(descriptor.SupportsHelp, descriptor.Name);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.UsageSummary));

            foreach (var argument in descriptor.RequiredArguments.Concat(descriptor.OptionalArguments).Concat(descriptor.FlagArguments))
                Assert.StartsWith("--", argument, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ApplyDescriptor_RemainsDisabled()
    {
        var apply = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.Apply);
        Assert.False(apply.ApplyEnabled);
    }

    private static IReadOnlyList<string> LoadInventoryCommandNames()
    {
        var path = Path.Combine(
            GetRepoRoot(),
            "docs",
            "security",
            "ownership-backfill-cli-command-inventory.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("commands")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

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
