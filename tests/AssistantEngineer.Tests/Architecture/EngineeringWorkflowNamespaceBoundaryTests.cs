using System.Text.Json;
using NetArchTest.Rules;

namespace AssistantEngineer.Tests.Architecture;

public sealed class EngineeringWorkflowNamespaceBoundaryTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void EngineeringWorkflowModuleApplicationNamespaceDoesNotUseApiNamespace()
    {
        var moduleApplicationPath = Path.Combine(
            RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application");

        var violations = Directory.GetFiles(moduleApplicationPath, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = ToRepoRelativePath(path),
                Content = File.ReadAllText(path)
            })
            .Where(item => item.Content.Contains("namespace AssistantEngineer.Api.", StringComparison.Ordinal))
            .Select(item => item.Path)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"EngineeringWorkflow module application files must not declare AssistantEngineer.Api namespaces: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ModuleAssembliesDoNotDependOnAssistantEngineerApiNamespace()
    {
        var moduleAssemblies = new[]
        {
            typeof(AssistantEngineer.Modules.Benchmarks.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.Buildings.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.Calculations.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.EngineeringWorkflow.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.Equipment.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.Identity.DependencyInjection).Assembly,
            typeof(AssistantEngineer.Modules.Reporting.DependencyInjection).Assembly
        };

        foreach (var assembly in moduleAssemblies)
        {
            var result = Types
                .InAssembly(assembly)
                .Should()
                .NotHaveDependencyOn("AssistantEngineer.Api")
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                $"{assembly.GetName().Name} depends on AssistantEngineer.Api namespace: {FormatFailingTypes(result)}");
        }
    }

    [Fact]
    public void ApiEngineeringWorkflowServiceShellRemainsExplicitlyAllowlisted()
    {
        var workflowServicePath = Path.Combine(
            RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Workflow");

        var allowlist = ReadAllowlist();
        var allowlistPaths = allowlist
            .Select(item => item.Path)
            .ToHashSet(StringComparer.Ordinal);

        var candidates = Directory.Exists(workflowServicePath)
            ? Directory.GetFiles(workflowServicePath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(ToRepoRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];

        var missingAllowlist = candidates
            .Where(path => !allowlistPaths.Contains(path))
            .ToArray();

        Assert.True(
            missingAllowlist.Length == 0,
            $"Workflow service shell files must be explicitly allowlisted with staged migration reason: {string.Join(", ", missingAllowlist)}");

        var malformedEntries = allowlist
            .Where(item => string.IsNullOrWhiteSpace(item.Path) ||
                           string.IsNullOrWhiteSpace(item.Reason) ||
                           string.IsNullOrWhiteSpace(item.ProposedStage))
            .Select(item => item.Path ?? "(null)")
            .ToArray();

        Assert.True(
            malformedEntries.Length == 0,
            $"Allowlist entries must include path/reason/proposedStage: {string.Join(", ", malformedEntries)}");

        var invalidStages = allowlist
            .Where(item => !string.Equals(item.ProposedStage, "P8-03E", StringComparison.Ordinal) &&
                           !string.Equals(item.ProposedStage, "P8-03F", StringComparison.Ordinal))
            .Select(item => item.Path ?? "(null)")
            .ToArray();

        Assert.True(
            invalidStages.Length == 0,
            $"EngineeringWorkflow allowlist entries must be staged to P8-03E or P8-03F: {string.Join(", ", invalidStages)}");

        if (!Directory.Exists(workflowServicePath))
        {
            Assert.True(
                allowlist.Count == 0,
                "EngineeringWorkflow boundary allowlist must be empty when legacy API workflow service shell folder no longer contains service files.");
        }
    }

    [Fact]
    public void P8_03E_MigratedWorkflowHelpersLiveInModuleApplicationNamespace()
    {
        var moduleWorkflowPath = Path.Combine(
            RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EngineeringWorkflow",
            "Application",
            "Workflow");

        var migratedFiles = new[]
        {
            "EngineeringWorkflowDiagnosticsService.cs",
            "EngineeringWorkflowStateBuilder.cs",
            "EngineeringWorkflowSubmissionService.cs",
            "IEngineeringWorkflowDiagnosticsService.cs",
            "IEngineeringWorkflowStateBuilder.cs",
            "IEngineeringWorkflowSubmissionService.cs"
        };

        foreach (var fileName in migratedFiles)
        {
            var filePath = Path.Combine(moduleWorkflowPath, fileName);
            Assert.True(File.Exists(filePath), $"Missing migrated workflow helper file: {filePath}");

            var source = File.ReadAllText(filePath);
            Assert.Contains(
                "namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain("namespace AssistantEngineer.Api.", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using AssistantEngineer.Api.", source, StringComparison.Ordinal);
        }

        var legacyApiPath = Path.Combine(
            RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Workflow");

        foreach (var fileName in migratedFiles)
        {
            var legacyPath = Path.Combine(legacyApiPath, fileName);
            Assert.False(File.Exists(legacyPath), $"Legacy API workflow helper path should be removed after P8-03E: {legacyPath}");
        }
    }

    private static IReadOnlyList<BoundaryAllowlistEntry> ReadAllowlist()
    {
        var allowlistPath = Path.Combine(
            RepoRoot,
            "tests",
            "fixtures",
            "architecture",
            "engineeringworkflow-boundary-allowlist.json");

        Assert.True(File.Exists(allowlistPath), $"Missing EngineeringWorkflow boundary allowlist: {allowlistPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(allowlistPath));
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);

        return root.EnumerateArray()
            .Select(item => new BoundaryAllowlistEntry(
                item.TryGetProperty("path", out var path) ? path.GetString() : null,
                item.TryGetProperty("reason", out var reason) ? reason.GetString() : null,
                item.TryGetProperty("proposedStage", out var stage) ? stage.GetString() : null))
            .ToArray();
    }

    private static string ToRepoRelativePath(string absolutePath) =>
        Path.GetRelativePath(RepoRoot, absolutePath).Replace('\\', '/');

    private static string FormatFailingTypes(TestResult result) =>
        result.FailingTypeNames is null
            ? "no failing type details returned"
            : string.Join(", ", result.FailingTypeNames);

    private sealed record BoundaryAllowlistEntry(
        string? Path,
        string? Reason,
        string? ProposedStage);
}
