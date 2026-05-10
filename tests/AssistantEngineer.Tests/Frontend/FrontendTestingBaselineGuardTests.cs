using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class FrontendTestingBaselineGuardTests
{
    [Fact]
    public void FrontendPackageDefinesVitestScriptsAndDependencies()
    {
        var packageJson = ReadRepoFile("src", "Frontend", "package.json");

        Assert.Contains("\"test\": \"vitest run\"", packageJson, StringComparison.Ordinal);
        Assert.Contains("\"test:watch\": \"vitest\"", packageJson, StringComparison.Ordinal);
        Assert.Contains("\"vitest\"", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"@testing-library/react\"", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"@testing-library/jest-dom\"", packageJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"jsdom\"", packageJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FrontendViteConfigRegistersJsdomTestEnvironmentAndSetupFile()
    {
        var viteConfig = ReadRepoFile("src", "Frontend", "vite.config.ts");

        Assert.Contains("test:", viteConfig, StringComparison.Ordinal);
        Assert.Contains("environment: \"jsdom\"", viteConfig, StringComparison.Ordinal);
        Assert.Contains("setupFiles: \"./src/test/setup.ts\"", viteConfig, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendWorkflowClientAndDiagnosticsPanelTestsExist()
    {
        Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src", "entities", "engineering-workflow", "api", "engineeringWorkflowClient.test.ts")));
        Assert.True(File.Exists(Path.Combine(TestPaths.RepoRoot, "src", "Frontend", "src", "widgets", "engineering-workflow", "ui", "WorkflowDiagnosticsPanel.test.tsx")));
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(
            parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
