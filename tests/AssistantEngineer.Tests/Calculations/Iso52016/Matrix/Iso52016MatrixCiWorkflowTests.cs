using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixCiWorkflowTests
{
    [Fact]
    public void CiWorkflow_RunsIso52016MatrixReleaseReadyGate()
    {
        var repoRoot = FindRepositoryRoot();

        var workflowPath = Path.Combine(
            repoRoot,
            ".github",
            "workflows",
            "iso52016-matrix-release-ready.yml");

        Assert.True(File.Exists(workflowPath), $"Workflow was not found: {workflowPath}");

        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("ISO52016 Matrix release-ready", workflow);
        Assert.Contains("windows-latest", workflow);
        Assert.Contains("actions/checkout@v4", workflow);
        Assert.Contains("actions/setup-dotnet@v4", workflow);
        Assert.Contains("10.0.x", workflow);
        Assert.Contains("assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit", workflow);
    }

    [Fact]
    public void CiWorkflow_IsScopedToIso52016RelevantPaths()
    {
        var repoRoot = FindRepositoryRoot();

        var workflowPath = Path.Combine(
            repoRoot,
            ".github",
            "workflows",
            "iso52016-matrix-release-ready.yml");

        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("src/Backend/AssistantEngineer.Modules.Calculations/**", workflow);
        Assert.Contains("src/Backend/AssistantEngineer.Api/**", workflow);
        Assert.Contains("tests/AssistantEngineer.Tests/**", workflow);
        Assert.Contains("docs/calculations/Iso52016Matrix*.md", workflow);
        Assert.Contains("scripts/iso52016/**", workflow);
        Assert.Contains("workflow_dispatch", workflow);
    }

    [Fact]
    public void CiDocumentation_ExplainsReleaseReadyCommandAndGeneratedArtifactPolicy()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "ci",
            "Iso52016MatrixCI.md");

        Assert.True(File.Exists(docPath), $"CI documentation was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("iso52016-matrix-release-ready.yml", doc);
        Assert.Contains("assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit", doc);
        Assert.Contains("Generated files", doc);
        Assert.Contains("must not be tracked by git", doc);
    }

    [Theory]
    [InlineData("docs/releases/Iso52016MatrixSolverStageManifest.json")]
    [InlineData("docs/releases/Iso52016MatrixBaselineFixturesManifest.json")]
    public void MatrixManifests_ReferenceCiGate(
        string relativeManifestPath)
    {
        var repoRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            relativeManifestPath.Split('/').Prepend(repoRoot).ToArray());

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(manifestPath));

        var root = document.RootElement;

        Assert.True(
            root.GetProperty("ciGateIntegrated").GetBoolean());

        var documentationFiles = root
            .GetProperty("documentationFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var testGuards = root
            .GetProperty("testGuards")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("docs/ci/Iso52016MatrixCI.md", documentationFiles);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixCiWorkflowTests.cs", testGuards);
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