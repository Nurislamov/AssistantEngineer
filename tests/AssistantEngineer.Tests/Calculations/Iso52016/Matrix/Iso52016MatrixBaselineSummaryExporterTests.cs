using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixBaselineSummaryExporterTests
{
    [Fact]
    public void BaselineSummaryExporterScriptExistsAndDocumentsExpectedOutputs()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "export-iso52016-matrix-baseline-summary.ps1");

        Assert.True(File.Exists(scriptPath), $"Exporter script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("summary.json", script);
        Assert.Contains("summary.md", script);
        Assert.Contains("AnnualHeatingEnergyKWh", script);
        Assert.Contains("AnnualCoolingEnergyKWh", script);
        Assert.Contains("PeakHeatingLoadW", script);
        Assert.Contains("PeakCoolingLoadW", script);
    }

    [Fact]
    public void BaselineVerifierRequiresSummaryExporterScript()
    {
        var repoRoot = FindRepositoryRoot();

        var verifierPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "verify-iso52016-matrix-baselines.ps1");

        Assert.True(File.Exists(verifierPath), $"Verifier script was not found: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);

        Assert.Contains("export-iso52016-matrix-baseline-summary.ps1", verifier);
    }

    [Fact]
    public void BaselineManifestReferencesSummaryExporterArtifacts()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixBaselineFixturesManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(
            File.ReadAllText(manifestPath));

        var root = document.RootElement;

        Assert.True(
            root.GetProperty("summaryExporterIntegrated").GetBoolean());

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

        Assert.Contains("docs/calculations/Iso52016MatrixBaselineSummaryExporter.md", documentationFiles);
        Assert.Contains("scripts/iso52016/export-iso52016-matrix-baseline-summary.ps1", verificationScripts);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixBaselineSummaryExporterTests.cs", testGuards);
    }

    [Fact]
    public void BaselineSummaryExporterDocumentationExplainsItDoesNotUpdateBaselines()
    {
        var repoRoot = FindRepositoryRoot();

        var docPath = Path.Combine(
            repoRoot,
            "docs",
            "calculations",
            "Iso52016MatrixBaselineSummaryExporter.md");

        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("does not create or update authoritative baseline values", doc);
        Assert.Contains("summary.json", doc);
        Assert.Contains("summary.md", doc);
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