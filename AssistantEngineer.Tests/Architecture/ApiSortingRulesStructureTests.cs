using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiSortingRulesStructureTests
{
    [Fact]
    public void SortingRulesLiveInDedicatedNamespaces()
    {
        var allowedNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "AssistantEngineer.Api.Sorting.Projects",
            "AssistantEngineer.Api.Sorting.Buildings",
            "AssistantEngineer.Api.Sorting.Equipment"
        };

        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type.Name.EndsWith("SortRules", StringComparison.Ordinal))
            .Where(type =>
                type.Namespace is null ||
                !allowedNamespaces.Contains(type.Namespace))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Sort rules must live in dedicated sorting namespaces: {string.Join("; ", violations)}.");
    }

}
