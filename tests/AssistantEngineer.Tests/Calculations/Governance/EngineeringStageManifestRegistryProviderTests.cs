using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringStageManifestRegistryProviderTests
{
    private readonly EngineeringStageManifestRegistryProvider _provider = new();

    [Fact]
    public void Registry_ContainsRequiredStagesFromFixture()
    {
        var registry = _provider.BuildRegistry(TestPaths.RepoRoot);
        var requiredStageIds = LoadRequiredStageIds();

        foreach (var stageId in requiredStageIds)
            Assert.Contains(stageId, registry.StagesById.Keys);
    }

    [Fact]
    public void Registry_StageIdsAreUnique()
    {
        var registry = _provider.BuildRegistry(TestPaths.RepoRoot);
        var duplicates = registry.Stages
            .GroupBy(stage => stage.StageId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Registry_ManifestsHavePaths()
    {
        var registry = _provider.BuildRegistry(TestPaths.RepoRoot);

        Assert.NotEmpty(registry.Stages);
        Assert.All(registry.Stages, stage => Assert.False(string.IsNullOrWhiteSpace(stage.ManifestPath)));
    }

    [Fact]
    public void Registry_DependenciesResolveOrAreLegacy()
    {
        var registry = _provider.BuildRegistry(TestPaths.RepoRoot);

        foreach (var stage in registry.Stages)
        {
            foreach (var dependency in stage.DependsOn)
            {
                if (dependency.IsExternalReference || registry.StagesById.ContainsKey(dependency.StageId))
                    continue;

                Assert.True(
                    stage.StageId.StartsWith("AE-ISO52016-002-STEP", StringComparison.Ordinal),
                    $"Dependency '{dependency.StageId}' from stage '{stage.StageId}' is unresolved and not treated as an external reference.");
            }
        }
    }

    private static IReadOnlyList<string> LoadRequiredStageIds()
    {
        var fixturePath = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "governance",
            "stage-registry-required-stages.json");

        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
        return document.RootElement
            .GetProperty("requiredStageIds")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }
}
