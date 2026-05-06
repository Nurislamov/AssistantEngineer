global using static AssistantEngineer.Tests.Calculations.Iso52016.Iso52016VerificationRegistryAssert;

using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

internal static class Iso52016VerificationRegistryAssert
{
    public static void RegistryContainsStageFile(string stageId, string propertyName, string expectedPath)
    {
        var stage = FindStage(stageId);

        var files = stage
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains(expectedPath, files);
    }

    public static void RegistryContainsTestFilter(string stageId, string expectedFilter)
    {
        var stage = FindStage(stageId);

        var filters = stage
            .GetProperty("testFilters")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains(expectedFilter, filters);
    }

    public static void RegistryContainsAlias(string stageId, string expectedScriptPath)
    {
        var stage = FindStage(stageId);

        var aliases = stage
            .GetProperty("deprecatedWrapperAliases")
            .EnumerateArray()
            .Select(item => item.GetProperty("path").GetString())
            .ToArray();

        Assert.Contains(expectedScriptPath, aliases);
    }

    public static string ReadIso52016VerificationRegistry()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "verification",
            "Iso52016VerificationRegistry.json");

        Assert.True(File.Exists(path), $"Registry does not exist: {path}");
        return File.ReadAllText(path);
    }

    private static JsonElement FindStage(string stageId)
    {
        using var document = JsonDocument.Parse(ReadIso52016VerificationRegistry());

        foreach (var stage in document.RootElement.GetProperty("stages").EnumerateArray())
        {
            if (stage.GetProperty("id").GetString() == stageId)
                return stage.Clone();
        }

        throw new Xunit.Sdk.XunitException($"Registry stage was not found: {stageId}");
    }
}
