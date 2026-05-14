using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture;

public class EngineeringWorkflowApiServicePlacementGuardTests
{
    private const string AllowlistPath = "tests/fixtures/architecture/engineering-workflow-api-service-allowlist.txt";
    private const string ExceptionsPath = "docs/architecture/engineering-workflow-api-service-exceptions.md";

    [Fact]
    public void WorkflowScenarioAndJobServiceLikeFilesInApiRequireAllowlistOrExplicitException()
    {
        var discoveredPaths = Directory
            .GetFiles(
                Path.Combine(TestPaths.ApiProjectPath, "Services", "Calculations"),
                "*.cs",
                SearchOption.AllDirectories)
            .Select(ToRepoRelativePath)
            .Where(IsWorkflowServiceLikeCandidate)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var allowlist = ReadListFile(AllowlistPath);
        var explicitExceptions = ReadExceptionPaths(ExceptionsPath);

        var violations = discoveredPaths
            .Where(path => !allowlist.Contains(path) && !explicitExceptions.Contains(path))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Workflow/scenario/job service-like files must migrate into AssistantEngineer.Modules.EngineeringWorkflow. " +
            $"If temporarily unavoidable, add an explicit exception in {ExceptionsPath}. Violations: {string.Join(", ", violations)}");
    }

    private static bool IsWorkflowServiceLikeCandidate(string repoRelativePath)
    {
        var normalized = NormalizePath(repoRelativePath);
        var fileName = Path.GetFileName(normalized);

        var inWorkflowArea =
            normalized.Contains("/Services/Calculations/Workflow/", StringComparison.Ordinal) ||
            normalized.Contains("/Services/Calculations/ScenarioExecution/", StringComparison.Ordinal) ||
            normalized.Contains("/Services/Calculations/Jobs/", StringComparison.Ordinal);

        var isLifecycleRoot =
            normalized.EndsWith("/Services/Calculations/EngineeringCalculationJobService.cs", StringComparison.Ordinal) ||
            normalized.EndsWith("/Services/Calculations/IEngineeringCalculationJobService.cs", StringComparison.Ordinal) ||
            normalized.EndsWith("/Services/Calculations/EngineeringCalculationScenarioRunner.cs", StringComparison.Ordinal) ||
            normalized.EndsWith("/Services/Calculations/IEngineeringCalculationScenarioRunner.cs", StringComparison.Ordinal);

        var isServiceLike = Regex.IsMatch(
            fileName,
            "(Service|Runner|Worker|Orchestrator|StateBuilder|Executor|Validator|Recorder|Codec|Policy|Builder|Step)\\.cs$",
            RegexOptions.CultureInvariant);

        return isServiceLike && (inWorkflowArea || isLifecycleRoot);
    }

    private static HashSet<string> ReadListFile(string relativePath)
    {
        var absolutePath = Path.Combine(TestPaths.RepoRoot, relativePath);
        Assert.True(File.Exists(absolutePath), $"Guard allowlist is missing: {absolutePath}");

        var items = File.ReadAllLines(absolutePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Select(NormalizePath);

        return new HashSet<string>(items, StringComparer.Ordinal);
    }

    private static HashSet<string> ReadExceptionPaths(string relativePath)
    {
        var absolutePath = Path.Combine(TestPaths.RepoRoot, relativePath);
        Assert.True(File.Exists(absolutePath), $"Guard exceptions registry is missing: {absolutePath}");

        var exceptionPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in File.ReadAllLines(absolutePath))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("- path:", StringComparison.Ordinal))
            {
                continue;
            }

            var pathPart = line.Substring("- path:".Length).Trim();
            var pipeIndex = pathPart.IndexOf('|');
            if (pipeIndex >= 0)
            {
                pathPart = pathPart[..pipeIndex].Trim();
            }

            if (pathPart.Length > 0)
            {
                exceptionPaths.Add(NormalizePath(pathPart));
            }
        }

        return exceptionPaths;
    }

    private static string ToRepoRelativePath(string absolutePath)
    {
        var relative = Path.GetRelativePath(TestPaths.RepoRoot, absolutePath);
        return NormalizePath(relative);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');
}
