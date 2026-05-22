using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class ApiContractStructureTests
{
    private static readonly string[] AllowedContractNamespaces =
    [
        "AssistantEngineer.Api.Contracts.Common",
        "AssistantEngineer.Api.Contracts.Projects",
        "AssistantEngineer.Api.Contracts.Buildings",
        "AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow",
        "AssistantEngineer.Api.Contracts.Analysis",
        "AssistantEngineer.Api.Contracts.Equipment",
        "AssistantEngineer.Api.Contracts.Reports",
        "AssistantEngineer.Api.Contracts.ReferenceData",
        "AssistantEngineer.Api.Contracts.Profiles",
        "AssistantEngineer.Api.Contracts.Benchmarks"
    ];

    [Fact]
    public void ApiContractsAreGroupedByFeatureNamespace()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                type.Namespace?.StartsWith(
                    "AssistantEngineer.Api.Contracts",
                    StringComparison.Ordinal) == true)
            .Where(type =>
                type.Namespace is null ||
                !AllowedContractNamespaces.Contains(type.Namespace, StringComparer.Ordinal))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"API contracts must be grouped by feature namespace: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void RootApiContractsNamespaceDoesNotContainContracts()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                string.Equals(
                    type.Namespace,
                    "AssistantEngineer.Api.Contracts",
                    StringComparison.Ordinal))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Root API Contracts namespace must not contain contracts: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void CommonContractsContainOnlyReusableInfrastructureDtos()
    {
        var allowedCommonContractNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "CollectionQueryParameters",
            "PagedResponse`1"
        };

        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                string.Equals(
                    type.Namespace,
                    "AssistantEngineer.Api.Contracts.Common",
                    StringComparison.Ordinal))
            .Where(type =>
                !allowedCommonContractNames.Contains(type.Name))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Common API contracts must stay minimal: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void BuildingQueryContractsLiveInBuildingsNamespace()
    {
        AssertContractsWithSuffixLiveInNamespace(
            "ListQueryParameters",
            "AssistantEngineer.Api.Contracts.Buildings",
            [
                "BuildingListQueryParameters",
                "BuildingArchetypeListQueryParameters",
                "RoomListQueryParameters",
                "WindowListQueryParameters",
                "WallListQueryParameters"
            ]);
    }

    [Fact]
    public void EquipmentQueryContractsLiveInEquipmentNamespace()
    {
        AssertContractsWithSuffixLiveInNamespace(
            "ListQueryParameters",
            "AssistantEngineer.Api.Contracts.Equipment",
            [
                "EquipmentCatalogListQueryParameters"
            ]);
    }

    private static void AssertContractsWithSuffixLiveInNamespace(
        string suffix,
        string expectedNamespace,
        IReadOnlyCollection<string> expectedTypeNames)
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                type.Name.EndsWith(suffix, StringComparison.Ordinal) &&
                expectedTypeNames.Contains(type.Name) &&
                !string.Equals(type.Namespace, expectedNamespace, StringComparison.Ordinal))
            .Select(type => $"{type.FullName} expected namespace {expectedNamespace}.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"API contracts are in wrong namespace: {string.Join("; ", violations)}.");
    }
}