using System.Reflection;

namespace AssistantEngineer.Tests.Architecture;

public class ModulePublicSurfaceTests
{
    [Fact]
    public void FacadeModulesExposeOnlyDeliberatePublicApi()
    {
        AssertPublicSurface(
            typeof(AssistantEngineer.Modules.Reporting.DependencyInjection).Assembly,
            "AssistantEngineer.Modules.Reporting",
            [
                "AssistantEngineer.Modules.Reporting.DependencyInjection",
                "AssistantEngineer.Modules.Reporting.Application.Abstractions.",
                "AssistantEngineer.Modules.Reporting.Application.Contracts.",
                "AssistantEngineer.Modules.Reporting.Application.Facades."
            ]);

        AssertPublicSurface(
            typeof(AssistantEngineer.Modules.Benchmarks.DependencyInjection).Assembly,
            "AssistantEngineer.Modules.Benchmarks",
            [
                "AssistantEngineer.Modules.Benchmarks.DependencyInjection",
                "AssistantEngineer.Modules.Benchmarks.Application.Abstractions.",
                "AssistantEngineer.Modules.Benchmarks.Application.Contracts.",
                "AssistantEngineer.Modules.Benchmarks.Application.Facades.",
                "AssistantEngineer.Modules.Benchmarks.Application.Models."
            ]);
    }

    private static void AssertPublicSurface(
        Assembly assembly,
        string modulePrefix,
        IReadOnlyList<string> allowedPrefixes)
    {
        var violations = assembly
            .GetExportedTypes()
            .Where(type => !type.IsNested)
            .Select(type => type.FullName ?? type.Name)
            .Where(typeName =>
                typeName.StartsWith(modulePrefix, StringComparison.Ordinal) &&
                !allowedPrefixes.Any(prefix => typeName.StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{assembly.GetName().Name} exposes non-public-api types: {string.Join(", ", violations)}.");
    }
}
