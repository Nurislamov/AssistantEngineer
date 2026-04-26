using AssistantEngineer.Api;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Architecture;

public class ApiFeatureStructureTests
{
    private static readonly string[] AllowedFeatures =
    [
        "Projects",
        "Buildings",
        "Calculations",
        "Analysis",
        "Equipment",
        "Reports",
        "ReferenceData",
        "Profiles",
        "Benchmarks"
    ];

    [Fact]
    public void ControllerFeatureNamespacesUseKnownFeatures()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .Select(type => type.Namespace)
            .Where(namespaceName => namespaceName is not null)
            .Select(namespaceName => namespaceName!.Split('.').Last())
            .Distinct(StringComparer.Ordinal)
            .Where(feature =>
                !AllowedFeatures.Contains(feature, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Unknown API controller feature namespaces: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ContractFeatureNamespacesUseKnownFeatures()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                type.Namespace?.StartsWith(
                    "AssistantEngineer.Api.Contracts.",
                    StringComparison.Ordinal) == true)
            .Select(type => type.Namespace)
            .Where(namespaceName => namespaceName is not null)
            .Select(namespaceName => namespaceName!.Split('.').Last())
            .Distinct(StringComparer.Ordinal)
            .Where(feature =>
                feature != "Common" &&
                !AllowedFeatures.Contains(feature, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Unknown API contract feature namespaces: {string.Join(", ", violations)}.");
    }
}