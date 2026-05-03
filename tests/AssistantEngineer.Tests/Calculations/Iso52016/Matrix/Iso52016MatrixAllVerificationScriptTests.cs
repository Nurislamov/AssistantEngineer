using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixAllVerificationScriptTests
{
    [Fact]
    public void AllVerificationScript_ReferencesEveryMatrixVerificationLayer()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-all.ps1");

        Assert.True(File.Exists(scriptPath), $"All-verification script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-matrix-solver-stage.ps1", script);
        Assert.Contains("verify-iso52016-matrix-baselines.ps1", script);
        Assert.Contains("verify-iso52016-matrix-application-baselines.ps1", script);
        Assert.Contains("export-iso52016-matrix-baseline-summary.ps1", script);
        Assert.Contains("Iso52016MatrixAllVerificationScript", script);
    }

    [Fact]
    public void VerificationRunbook_DocumentsSingleEntryPointAndGeneratedArtifacts()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixVerificationRunbook.md");

        Assert.True(File.Exists(docPath), $"Runbook was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("verify-iso52016-matrix-all.ps1", doc);
        Assert.Contains("Low-level Matrix solver baseline fixtures", doc);
        Assert.Contains("Application/building-facade Matrix baseline fixtures", doc);
        Assert.Contains("artifacts/iso52016/matrix-baselines", doc);
        Assert.Contains("should not be committed", doc);
    }

    [Theory]
    [InlineData("docs/releases/Iso52016MatrixSolverStageManifest.json")]
    [InlineData("docs/releases/Iso52016MatrixBaselineFixturesManifest.json")]
    public void MatrixManifests_ReferenceAllInOneVerificationScript(
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
            root.GetProperty("allInOneVerificationIntegrated").GetBoolean());

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

        Assert.Contains("docs/calculations/Iso52016MatrixVerificationRunbook.md", documentationFiles);
        Assert.Contains("scripts/iso52016/verify-iso52016-matrix-all.ps1", verificationScripts);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixAllVerificationScriptTests.cs", testGuards);
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