using System.Text.Json;
using System.Xml.Linq;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ModuleBoundaryMatrixTests
{
    private static readonly StringComparer Comparer = StringComparer.Ordinal;

    [Fact]
    public void EveryBackendProjectAppearsInBoundaryMatrix()
    {
        var matrix = ReadMatrix();
        var backendProjects = DiscoverProjectNames(Path.Combine(TestPaths.RepoRoot, "src", "Backend"));

        foreach (var project in backendProjects)
            Assert.Contains(project, matrix.Keys);
    }

    [Fact]
    public void EveryToolProjectAppearsInBoundaryMatrix()
    {
        var matrix = ReadMatrix();
        var toolProjects = DiscoverProjectNames(Path.Combine(TestPaths.RepoRoot, "tools"));

        foreach (var project in toolProjects)
            Assert.Contains(project, matrix.Keys);
    }

    [Fact]
    public void EngineeringWorkflowModuleAppearsExplicitlyInBoundaryMatrix()
    {
        var matrix = ReadMatrix();
        Assert.True(matrix.ContainsKey("AssistantEngineer.Modules.EngineeringWorkflow"));
    }

    [Fact]
    public void DomainModulesMustNotReferenceAssistantEngineerApi()
    {
        var projects = DiscoverProjectsWithReferences();

        foreach (var project in projects.Where(p => IsDomainModule(p.Name)))
        {
            Assert.DoesNotContain(
                "AssistantEngineer.Api",
                project.References);
        }
    }

    [Fact]
    public void DomainModulesMustNotReferenceTools()
    {
        var projects = DiscoverProjectsWithReferences();

        foreach (var project in projects.Where(p => IsDomainModule(p.Name)))
        {
            var toolRefs = project.References
                .Where(IsToolProject)
                .ToArray();

            Assert.True(
                toolRefs.Length == 0,
                $"{project.Name} references tools: {string.Join(", ", toolRefs)}");
        }
    }

    [Fact]
    public void RuntimeBackendProjectsMustNotReferenceTools()
    {
        var projects = DiscoverProjectsWithReferences()
            .Where(p => IsRuntimeBackendProject(p.Name))
            .ToArray();

        foreach (var project in projects)
        {
            var toolRefs = project.References
                .Where(IsToolProject)
                .ToArray();

            Assert.True(
                toolRefs.Length == 0,
                $"{project.Name} references tools: {string.Join(", ", toolRefs)}");
        }
    }

    [Fact]
    public void ApiMayReferenceModulesButMustNotReferenceTools()
    {
        var projects = DiscoverProjectsWithReferences();
        var api = projects.Single(project => project.Name == "AssistantEngineer.Api");

        Assert.Contains("AssistantEngineer.Modules.Buildings", api.References);
        Assert.Contains("AssistantEngineer.Modules.Calculations", api.References);
        Assert.Contains("AssistantEngineer.Modules.EngineeringWorkflow", api.References);
        Assert.Contains("AssistantEngineer.Modules.Identity", api.References);

        Assert.DoesNotContain(api.References, IsToolProject);
    }

    [Fact]
    public void InfrastructureReferencesMustMatchMatrixDirection()
    {
        var matrix = ReadMatrix();
        var projects = DiscoverProjectsWithReferences();
        var infra = projects.Single(project => project.Name == "AssistantEngineer.Infrastructure");

        var allowed = matrix[infra.Name].AllowedDependencies.ToHashSet(Comparer);
        var violations = infra.References
            .Where(IsAssistantEngineerProject)
            .Where(reference => !allowed.Contains(reference))
            .OrderBy(value => value, Comparer)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Infrastructure references are outside matrix allow-set: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ToolReferencesToRuntimeAssembliesMustBeAllowedByMatrix()
    {
        var matrix = ReadMatrix();
        var projects = DiscoverProjectsWithReferences();

        foreach (var tool in projects.Where(p => IsToolProject(p.Name)))
        {
            var allowed = matrix[tool.Name].AllowedDependencies.ToHashSet(Comparer);
            var runtimeRefs = tool.References
                .Where(reference => IsRuntimeBackendProject(reference))
                .ToArray();

            var violations = runtimeRefs
                .Where(reference => !allowed.Contains(reference))
                .OrderBy(value => value, Comparer)
                .ToArray();

            Assert.True(
                violations.Length == 0,
                $"{tool.Name} runtime references are outside matrix allow-set: {string.Join(", ", violations)}");
        }
    }

    [Fact]
    public void MatrixAllowedDependenciesMustCoverActualInternalProjectReferences()
    {
        var matrix = ReadMatrix();
        var projects = DiscoverProjectsWithReferences();

        foreach (var project in projects.Where(p => matrix.ContainsKey(p.Name)))
        {
            var allowed = matrix[project.Name].AllowedDependencies.ToHashSet(Comparer);
            var internalRefs = project.References
                .Where(IsAssistantEngineerProject)
                .ToArray();

            var violations = internalRefs
                .Where(reference => !allowed.Contains(reference))
                .OrderBy(value => value, Comparer)
                .ToArray();

            Assert.True(
                violations.Length == 0,
                $"{project.Name} references not present in matrix allowedDependencies: {string.Join(", ", violations)}");
        }
    }

    [Fact]
    public void ModuleSourceNamespacesMustNotUseAssistantEngineerApiPrefix()
    {
        var moduleRoots = Directory
            .GetDirectories(Path.Combine(TestPaths.RepoRoot, "src", "Backend"), "AssistantEngineer.Modules.*", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, Comparer)
            .ToArray();

        var violations = new List<string>();

        foreach (var moduleRoot in moduleRoots)
        {
            var files = Directory.GetFiles(moduleRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index].TrimStart();
                    if (!line.StartsWith("namespace ", StringComparison.Ordinal))
                        continue;

                    if (line.Contains("AssistantEngineer.Api.", StringComparison.Ordinal))
                    {
                        var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                        violations.Add($"{relative}:{index + 1}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Module namespaces must not start with AssistantEngineer.Api.*: " + string.Join(", ", violations));
    }

    [Fact]
    public void ModuleApplicationOrDomainSourcesMustNotUseAssistantEngineerApiNamespace()
    {
        var moduleRoots = Directory
            .GetDirectories(Path.Combine(TestPaths.RepoRoot, "src", "Backend"), "AssistantEngineer.Modules.*", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, Comparer)
            .ToArray();

        var violations = new List<string>();
        foreach (var moduleRoot in moduleRoots)
        {
            var applicationPath = Path.Combine(moduleRoot, "Application");
            var domainPath = Path.Combine(moduleRoot, "Domain");
            var candidateRoots = new[] { applicationPath, domainPath }
                .Where(Directory.Exists);

            foreach (var root in candidateRoots)
            {
                foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    if (content.Contains("using AssistantEngineer.Api.", StringComparison.Ordinal) ||
                        content.Contains("using AssistantEngineer.Api;", StringComparison.Ordinal))
                    {
                        var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                        violations.Add(relative);
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Module Application/Domain sources must not use AssistantEngineer.Api namespace: " + string.Join(", ", violations));
    }

    [Fact]
    public void ModuleBoundaryAllowlistEntriesMustIncludeReasonAndStageWhenPresent()
    {
        var allowlistPath = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "architecture",
            "module-boundary-allowlist.json");

        Assert.True(File.Exists(allowlistPath), $"Missing module-boundary allowlist: {allowlistPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(allowlistPath));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("source").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("target").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("reason").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("proposedStage").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("expiresWhen").GetString()));
        }
    }

    private static IReadOnlyDictionary<string, MatrixComponent> ReadMatrix()
    {
        var matrixPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "architecture",
            "module-boundary-matrix.json");

        using var document = JsonDocument.Parse(File.ReadAllText(matrixPath));
        var components = document.RootElement
            .GetProperty("components")
            .EnumerateArray()
            .Select(component => new MatrixComponent(
                component.GetProperty("project").GetString() ?? string.Empty,
                component.GetProperty("allowedDependencies").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray()))
            .ToArray();

        return components.ToDictionary(component => component.Project, component => component, Comparer);
    }

    private static IReadOnlyList<ProjectDefinition> DiscoverProjectsWithReferences()
    {
        var projectFiles = Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "src", "Backend"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "tools"), "*.csproj", SearchOption.AllDirectories))
            .OrderBy(path => path, Comparer)
            .ToArray();

        return projectFiles
            .Select(path =>
            {
                var document = XDocument.Load(path);
                var references = document.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                    .Select(element => element.Attribute("Include")?.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => Path.GetFileNameWithoutExtension(value!))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(Comparer)
                    .OrderBy(value => value, Comparer)
                    .ToArray();

                return new ProjectDefinition(
                    Path.GetFileNameWithoutExtension(path),
                    references);
            })
            .ToArray();
    }

    private static IReadOnlyCollection<string> DiscoverProjectNames(string root)
    {
        return Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, Comparer)
            .ToArray()!;
    }

    private static bool IsAssistantEngineerProject(string projectName) =>
        projectName.StartsWith("AssistantEngineer.", StringComparison.Ordinal);

    private static bool IsToolProject(string projectName) =>
        projectName.StartsWith("AssistantEngineer.Tools.", StringComparison.Ordinal);

    private static bool IsDomainModule(string projectName) =>
        projectName.StartsWith("AssistantEngineer.Modules.", StringComparison.Ordinal);

    private static bool IsRuntimeBackendProject(string projectName) =>
        projectName.StartsWith("AssistantEngineer.Modules.", StringComparison.Ordinal) ||
        string.Equals(projectName, "AssistantEngineer.Api", StringComparison.Ordinal) ||
        string.Equals(projectName, "AssistantEngineer.Infrastructure", StringComparison.Ordinal) ||
        string.Equals(projectName, "AssistantEngineer.SharedKernel", StringComparison.Ordinal);

    private sealed record ProjectDefinition(
        string Name,
        IReadOnlyList<string> References);

    private sealed record MatrixComponent(
        string Project,
        IReadOnlyList<string> AllowedDependencies);
}
