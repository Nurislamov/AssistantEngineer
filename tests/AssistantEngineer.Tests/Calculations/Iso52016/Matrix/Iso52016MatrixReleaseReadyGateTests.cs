using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixReleaseReadyGateTests
{
    [Fact]
    public void ReleaseReadyScript_IsThinWrapperOverRegistryTool()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "assert-iso52016-matrix-release-ready.ps1");

        Assert.True(File.Exists(scriptPath), $"Release-ready script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
        Assert.Contains("assert-release-ready", script);
        Assert.Contains("--repo-root", script);
        Assert.Contains("RequireCleanGit", script);
        Assert.DoesNotContain("dotnet test", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git ls-files", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseReadyDocumentation_ExplainsGeneratedArtifactsMustNotBeCommitted()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixReleaseReadyGate.md");

        Assert.True(File.Exists(docPath), $"Release-ready doc was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("assert-iso52016-matrix-release-ready.ps1", doc);
        Assert.Contains("AssistantEngineer.Tools.Iso52016Verification", doc);
        Assert.Contains("Generated artifact paths", doc);
        Assert.Contains("must not be committed", doc);
    }

    [Fact]
    public void GitIgnore_ExcludesGeneratedMatrixBaselineSummaries()
    {
        var repoRoot = FindRepositoryRoot();
        var gitIgnorePath = Path.Combine(repoRoot, ".gitignore");

        Assert.True(File.Exists(gitIgnorePath), ".gitignore was not found.");

        var gitIgnore = File.ReadAllText(gitIgnorePath);

        Assert.Contains("artifacts/iso52016/matrix-baselines/", gitIgnore);
    }

    [Theory]
    [InlineData("docs/releases/Iso52016MatrixSolverStageManifest.json")]
    [InlineData("docs/releases/Iso52016MatrixBaselineFixturesManifest.json")]
    public void MatrixManifests_ReferenceReleaseReadyGate(
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
            root.GetProperty("releaseReadyGateIntegrated").GetBoolean());

        var documentationFiles = root
            .GetProperty("documentationFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var verificationScripts = root
            .GetProperty("verificationScripts")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var testGuards = root
            .GetProperty("testGuards")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("docs/calculations/Iso52016MatrixReleaseReadyGate.md", documentationFiles);
        Assert.Contains("scripts/iso52016/assert-iso52016-matrix-release-ready.ps1", verificationScripts);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixReleaseReadyGateTests.cs", testGuards);
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
