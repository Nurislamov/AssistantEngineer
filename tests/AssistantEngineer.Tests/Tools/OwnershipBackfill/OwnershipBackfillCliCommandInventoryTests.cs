using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliCommandInventoryTests
{
    [Fact]
    public void CliCommandInventory_ExistsAndParses()
    {
        var path = Path.Combine(GetRepoRoot(), "docs", "security", "ownership-backfill-cli-command-inventory.json");
        Assert.True(File.Exists(path));

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(doc.RootElement.TryGetProperty("commands", out var commands));
        Assert.Equal(JsonValueKind.Array, commands.ValueKind);
    }

    [Fact]
    public void EveryEnumCommand_AppearsInInventory()
    {
        var commands = LoadCommandSetFromInventory();
        foreach (var commandType in Enum.GetValues<OwnershipBackfillCommandType>())
        {
            var commandName = ToCommandName(commandType);
            Assert.Contains(commandName, commands);
        }
    }

    [Fact]
    public void InventoryCommands_AreKnownToEnum()
    {
        var enumSet = Enum.GetValues<OwnershipBackfillCommandType>()
            .Select(ToCommandName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var command in LoadCommandSetFromInventory())
            Assert.Contains(command, enumSet);
    }

    [Fact]
    public void EveryInventoryCommand_IsRecognizedByParser()
    {
        var parser = new OwnershipBackfillCommandLineParser();
        foreach (var command in LoadCommandSetFromInventory())
        {
            var result = parser.Parse([command, "--help"]);
            Assert.True(result.IsSuccess, $"Command '{command}' should be recognized.");
            Assert.True(result.ShowHelp, $"Command '{command}' should support help routing.");
        }
    }

    [Fact]
    public void ApplyInventory_RemainsDisabledAndNoWrite()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(GetInventoryPath()));
        var apply = doc.RootElement.GetProperty("commands")
            .EnumerateArray()
            .Single(x => string.Equals(x.GetProperty("name").GetString(), "apply", StringComparison.OrdinalIgnoreCase));

        Assert.False(apply.GetProperty("applyEnabled").GetBoolean());
        Assert.False(apply.GetProperty("writesToDb").GetBoolean());
        Assert.Contains(OwnershipBackfillExitCodes.InvalidInput, apply.GetProperty("exitCodes").EnumerateArray().Select(x => x.GetInt32()));
    }

    [Fact]
    public void AllCommands_AreNoWriteAndUseKnownExitCodes()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(GetInventoryPath()));
        var knownExitCodes = doc.RootElement
            .GetProperty("exitCodes")
            .EnumerateArray()
            .Select(x => x.GetProperty("code").GetInt32())
            .ToHashSet();

        foreach (var command in doc.RootElement.GetProperty("commands").EnumerateArray())
        {
            Assert.False(command.GetProperty("writesToDb").GetBoolean());
            foreach (var code in command.GetProperty("exitCodes").EnumerateArray().Select(x => x.GetInt32()))
                Assert.Contains(code, knownExitCodes);
        }
    }

    [Fact]
    public void Inventory_HasRequiredNonClaims()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(GetInventoryPath()));
        var nonClaims = doc.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        Assert.Contains(nonClaims, x => x.Contains("No production apply enabled claim.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, x => x.Contains("No ownership backfill execution claim.", StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> LoadCommandSetFromInventory()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(GetInventoryPath()));
        return doc.RootElement.GetProperty("commands")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetInventoryPath() =>
        Path.Combine(GetRepoRoot(), "docs", "security", "ownership-backfill-cli-command-inventory.json");

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

    private static string ToCommandName(OwnershipBackfillCommandType commandType) =>
        commandType switch
        {
            OwnershipBackfillCommandType.DryRun => "dry-run",
            OwnershipBackfillCommandType.ValidateEvidence => "validate-evidence",
            OwnershipBackfillCommandType.ValidateApplyReadiness => "validate-apply-readiness",
            OwnershipBackfillCommandType.ValidateProductionPromotion => "validate-production-promotion",
            OwnershipBackfillCommandType.ValidateStagingPreflight => "validate-staging-preflight",
            OwnershipBackfillCommandType.ValidateStagingAcceptance => "validate-staging-acceptance",
            OwnershipBackfillCommandType.Apply => "apply",
            OwnershipBackfillCommandType.PlanApply => "plan-apply",
            OwnershipBackfillCommandType.SignoffPlan => "signoff-plan",
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), commandType, "Unsupported command type.")
        };
}
