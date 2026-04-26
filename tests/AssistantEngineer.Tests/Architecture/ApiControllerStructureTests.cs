using AssistantEngineer.Api;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Architecture;

public class ApiControllerStructureTests
{
    private static readonly string[] AllowedControllerNamespaces =
    [
        "AssistantEngineer.Api.Controllers.Projects",
        "AssistantEngineer.Api.Controllers.Buildings",
        "AssistantEngineer.Api.Controllers.Calculations",
        "AssistantEngineer.Api.Controllers.Analysis",
        "AssistantEngineer.Api.Controllers.Equipment",
        "AssistantEngineer.Api.Controllers.Reports",
        "AssistantEngineer.Api.Controllers.ReferenceData",
        "AssistantEngineer.Api.Controllers.Profiles",
        "AssistantEngineer.Api.Controllers.Benchmarks"
    ];

    [Fact]
    public void ControllersAreGroupedByFeatureNamespace()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type =>
                type.Namespace is null ||
                !AllowedControllerNamespaces.Contains(type.Namespace, StringComparer.Ordinal))
            .Select(type => $"{type.FullName} has invalid namespace '{type.Namespace}'.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers must be grouped by feature namespace: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void RootControllersNamespaceDoesNotContainControllers()
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                string.Equals(
                    type.Namespace,
                    "AssistantEngineer.Api.Controllers",
                    StringComparison.Ordinal))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Root Controllers namespace must not contain controllers: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ReportControllersLiveOnlyInReportsNamespace()
    {
        AssertControllersWithSuffixLiveInNamespace(
            "ReportsController",
            "AssistantEngineer.Api.Controllers.Reports");
    }

    [Fact]
    public void AnalysisControllersLiveOnlyInAnalysisNamespace()
    {
        AssertControllersWithSuffixLiveInNamespace(
            "AnalysisController",
            "AssistantEngineer.Api.Controllers.Analysis");
    }

    private static void AssertControllersWithSuffixLiveInNamespace(
        string suffix,
        string expectedNamespace)
    {
        var violations = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type.Name.EndsWith(suffix, StringComparison.Ordinal) &&
                !string.Equals(type.Namespace, expectedNamespace, StringComparison.Ordinal))
            .Select(type => $"{type.FullName} expected namespace {expectedNamespace}.")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Controllers are in wrong namespace: {string.Join("; ", violations)}.");
    }
}