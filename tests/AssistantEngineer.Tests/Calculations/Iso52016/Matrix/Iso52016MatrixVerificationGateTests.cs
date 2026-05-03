using System.Text.Json;
namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixVerificationGateTests
{
    [Fact]
    public void MatrixStageVerifier_CallsBaselineVerifier()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-solver-stage.ps1");

        Assert.True(File.Exists(scriptPath), $"Verifier was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("verify-iso52016-matrix-baselines.ps1", script);
        Assert.Contains("SkipBaselines", script);
        Assert.Contains("Iso52016MatrixBaselineFixtureTests.cs", script);
    }

    [Fact]
    public void MatrixStageManifest_ReferencesBaselineManifestAndVerificationScript()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixSolverStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        var manifest = File.ReadAllText(manifestPath);

        using var document = JsonDocument.Parse(manifest);

        Assert.True(
            document.RootElement.GetProperty("baselineVerificationIntegrated").GetBoolean());
        Assert.Contains("Iso52016MatrixBaselineFixturesManifest.json", manifest);
        Assert.Contains("verify-iso52016-matrix-baselines.ps1", manifest);
        Assert.Contains("Iso52016MatrixBaselineFixtureTests.cs", manifest);
    }

    [Fact]
    public void MatrixVerificationGateDocumentation_ExplainsBaselineLayer()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixVerificationGate.md");

        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("Structural/stage verification", doc);
        Assert.Contains("Baseline verification", doc);
        Assert.Contains("verify-iso52016-matrix-solver-stage.ps1", doc);
        Assert.Contains("verify-iso52016-matrix-baselines.ps1", doc);
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