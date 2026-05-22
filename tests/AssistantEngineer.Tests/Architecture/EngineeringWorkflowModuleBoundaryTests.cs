using System.Text.Json;
using NetArchTest.Rules;

namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringWorkflowModuleBoundaryTests
{
    [Fact]
    public void EngineeringWorkflowApplicationNamespacesUseExpectedPrefix()
    {
        var applicationPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application");

        var invalidFiles = FindNamespaceViolations(
            applicationPath,
            "AssistantEngineer.Modules.EngineeringWorkflow.Application.");

        Assert.True(
            invalidFiles.Count == 0,
            "EngineeringWorkflow Application files must use AssistantEngineer.Modules.EngineeringWorkflow.Application.* namespaces: "
            + string.Join(", ", invalidFiles));
    }

    [Fact]
    public void EngineeringWorkflowDomainNamespacesUseExpectedPrefixWhenDomainFolderExists()
    {
        var domainPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Domain");

        if (!Directory.Exists(domainPath))
            return;

        var invalidFiles = FindNamespaceViolations(
            domainPath,
            "AssistantEngineer.Modules.EngineeringWorkflow.Domain.");

        Assert.True(
            invalidFiles.Count == 0,
            "EngineeringWorkflow Domain files must use AssistantEngineer.Modules.EngineeringWorkflow.Domain.* namespaces: "
            + string.Join(", ", invalidFiles));
    }

    [Fact]
    public void EngineeringWorkflowApplicationMustNotUseAssistantEngineerApiNamespace()
    {
        var assembly = typeof(AssistantEngineer.Modules.EngineeringWorkflow.DependencyInjection).Assembly;
        var result = Types
            .InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn("AssistantEngineer.Api")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"EngineeringWorkflow Application depends on AssistantEngineer.Api: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void EngineeringWorkflowModuleIsPresentInModuleBoundaryMatrix()
    {
        using var matrix = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "architecture",
            "module-boundary-matrix.json")));

        var projects = matrix.RootElement
            .GetProperty("components")
            .EnumerateArray()
            .Select(item => item.GetProperty("project").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AssistantEngineer.Modules.EngineeringWorkflow", projects);
    }

    [Fact]
    public void EngineeringWorkflowBoundaryAllowlistEntriesAreStagedForP8_03EOrP8_03F()
    {
        var allowlistPath = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "architecture",
            "engineeringworkflow-boundary-allowlist.json");

        using var allowlist = JsonDocument.Parse(File.ReadAllText(allowlistPath));
        foreach (var entry in allowlist.RootElement.EnumerateArray())
        {
            var reason = entry.GetProperty("reason").GetString();
            var stage = entry.GetProperty("proposedStage").GetString();

            Assert.False(string.IsNullOrWhiteSpace(reason));
            Assert.False(string.IsNullOrWhiteSpace(stage));
            Assert.True(
                string.Equals(stage, "P8-03E", StringComparison.Ordinal) ||
                string.Equals(stage, "P8-03F", StringComparison.Ordinal),
                $"EngineeringWorkflow allowlist stage must be P8-03E or P8-03F, got: {stage}");
        }
    }

    private static List<string> FindNamespaceViolations(string rootPath, string requiredPrefix)
    {
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (!line.StartsWith("namespace ", StringComparison.Ordinal))
                    continue;

                if (!line.Contains(requiredPrefix, StringComparison.Ordinal))
                {
                    var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                    violations.Add($"{relative}:{index + 1}");
                }

                break;
            }
        }

        return violations.OrderBy(value => value, StringComparer.Ordinal).ToList();
    }

    private static string FormatFailingTypes(TestResult result) =>
        result.FailingTypeNames is null
            ? "no failing type details returned"
            : string.Join(", ", result.FailingTypeNames);
}
