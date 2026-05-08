namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1MainNodeToolchainWorkflowTests
{
    [Fact]
    public void MainEngineeringCoreWorkflow_SetupsNodeAndInstallsFrontendDependenciesBeforeVerification()
    {
        var repoRoot = FindRepositoryRoot();

        var workflowPath = Path.Combine(
            repoRoot,
            ".github",
            "workflows",
            "engineering-core-v1.yml");

        Assert.True(File.Exists(workflowPath), $"Workflow was not found: {workflowPath}");

        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("actions/setup-node@v4", workflow);
        Assert.Contains("node-version: 22", workflow);
        Assert.Contains("cache-dependency-path: src/Frontend/package-lock.json", workflow);
        Assert.Contains("npm --version", workflow);
        Assert.Contains("npm ci --prefix .\\src\\Frontend", workflow);
        Assert.Contains("verify-engineering-core-v1.ps1", workflow);
    }

    [Fact]
    public void MainEngineeringCoreVerificationScript_FailsWithClearMessageWhenNpmIsMissing()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "engineering-core",
            "verify-engineering-core-v1.ps1");

        Assert.True(File.Exists(scriptPath), $"Verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("Get-Command npm -ErrorAction SilentlyContinue", script);
        Assert.Contains("npm was not found on PATH", script);
        Assert.Contains("actions/setup-node", script);
        Assert.Contains("Engineering Core V1 frontend verification", script);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}