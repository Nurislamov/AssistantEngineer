namespace AssistantEngineer.Tests.Architecture;

public sealed class DevelopmentEndpointSecurityGuardTests
{
    [Fact]
    public void DevelopmentLikeControllersMustBeEnvironmentGatedOrAllowlisted()
    {
        var allowlist = ParseAllowlistEntries(AllowlistPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suspiciousTokens = new[]
        {
            "Development",
            "Demo",
            "DemoData",
            "Seed",
            "Debug",
            "Dev"
        };

        var controllerFiles = Directory.GetFiles(ControllersRootPath, "*Controller.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in controllerFiles)
        {
            var relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
            if (allowlist.Contains(relative))
            {
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!suspiciousTokens.Any(token => fileName.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var hasEnvironmentGate =
                text.Contains("IsDevelopment(", StringComparison.Ordinal) ||
                text.Contains("environment.IsDevelopment()", StringComparison.Ordinal) ||
                text.Contains("IWebHostEnvironment", StringComparison.Ordinal);

            if (!hasEnvironmentGate)
            {
                violations.Add(relative);
            }
        }

        Assert.True(
            violations.Count == 0,
            "Suspicious development/demo controllers must be environment-gated or allowlisted:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void DevelopmentDemoDataControllerContainsExplicitDevelopmentCheck()
    {
        var text = File.ReadAllText(DevelopmentDemoDataControllerPath);

        Assert.Contains("IsDevelopment(", text, StringComparison.Ordinal);
        Assert.Contains("return NotFound();", text, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ParseAllowlistEntries(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();
    }

    private static string ControllersRootPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");

    private static string AllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "security", "development-endpoint-allowlist.txt");

    private static string DevelopmentDemoDataControllerPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "ReferenceData",
            "DevelopmentDemoDataController.cs");
}
