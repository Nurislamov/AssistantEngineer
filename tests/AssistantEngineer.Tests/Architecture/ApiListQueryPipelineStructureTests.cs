using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiListQueryPipelineStructureTests
{
    [Fact]
    public void ListQueryPipelinesLiveInDedicatedNamespaces()
    {
        var allowedNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssistantEngineer.Api.Querying.Projects",
            "AssistantEngineer.Api.Querying.Buildings",
            "AssistantEngineer.Api.Querying.Equipment"
        };

        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Name.EndsWith("ListQueryExtensions", StringComparison.Ordinal))
            .Where(type =>
                type.Namespace is null ||
                !allowedNamespaces.Contains(type.Namespace))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"List query pipelines must live in dedicated querying namespaces: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void ControllersDoNotManuallyComposeFilterSearchSortPipeline()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var controllerFiles = Directory
            .EnumerateFiles(
                Path.Combine(apiProjectPath, "Controllers"),
                "*.cs",
                SearchOption.AllDirectories)
            .ToArray();

        var forbiddenPatterns = new[]
        {
            ".ApplyBuildingFilters(",
            ".ApplyBuildingArchetypeFilters(",
            ".ApplyRoomFilters(",
            ".ApplyWindowFilters(",
            ".ApplyWallFilters(",
            ".ApplyEquipmentCatalogFilters(",

            ".ApplyProjectSearch(",
            ".ApplyBuildingSearch(",
            ".ApplyBuildingArchetypeSearch(",
            ".ApplyFloorSearch(",
            ".ApplyRoomSearch(",
            ".ApplyWindowSearch(",
            ".ApplyWallSearch(",
            ".ApplyThermalZoneSearch(",
            ".ApplyEquipmentCatalogSearch(",

            ".ApplySort("
        };

        var violations = controllerFiles
            .Where(path =>
            {
                var text = File.ReadAllText(path);

                return forbiddenPatterns.Any(pattern =>
                    text.Contains(pattern, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must use feature list query pipelines instead of composing filter/search/sort manually: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ControllersUseFeatureListQueryPipelinesForPagedEndpoints()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var controllerFiles = Directory
            .EnumerateFiles(
                Path.Combine(apiProjectPath, "Controllers"),
                "*.cs",
                SearchOption.AllDirectories)
            .ToArray();

        var filesWithPagedResponse = controllerFiles
            .Where(path =>
            {
                var text = File.ReadAllText(path);

                return text.Contains("PagedResponse<", StringComparison.Ordinal);
            })
            .ToArray();

        var violations = filesWithPagedResponse
            .Where(path =>
            {
                var text = File.ReadAllText(path);

                return !text.Contains("ApplyProjectListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyBuildingListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyBuildingArchetypeListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyFloorListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyRoomListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyWindowListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyWallListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyThermalZoneListQuery(", StringComparison.Ordinal) &&
                       !text.Contains("ApplyEquipmentCatalogListQuery(", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(apiProjectPath, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Paged endpoints should use feature list query pipelines: {string.Join(", ", violations)}.");
    }
}