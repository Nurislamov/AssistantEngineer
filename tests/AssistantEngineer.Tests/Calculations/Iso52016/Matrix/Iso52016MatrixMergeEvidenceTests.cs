using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixMergeEvidenceTests
{
    [Fact]
    public void ReleaseNotes_RecordMatrixScopeVerificationAndNonClaims()
    {
        var repoRoot = FindRepositoryRoot();

        var releaseNotesPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixReleaseNotes.md");

        Assert.True(File.Exists(releaseNotesPath), $"Release notes were not found: {releaseNotesPath}");

        var releaseNotes = File.ReadAllText(releaseNotesPath);

        Assert.Contains("ISO 52016 Matrix", releaseNotes);
        Assert.Contains("Removed the old simplified `Legacy` solver path", releaseNotes);
        Assert.Contains("Removed the temporary `simulationEngine` selector", releaseNotes);
        Assert.Contains("assert-iso52016-matrix-release-ready.ps1", releaseNotes);
        Assert.Contains("No exact StandardReference numerical equivalence", releaseNotes);
        Assert.Contains("No exact EnergyPlus numerical equivalence", releaseNotes);
        Assert.Contains("ASHRAE 140", releaseNotes);
    }

    [Fact]
    public void MergeRunbook_DocumentsReleaseReadyGeneratedArtifactAndMergeChecks()
    {
        var repoRoot = FindRepositoryRoot();

        var runbookPath = Path.Combine(
            repoRoot,
            "docs",
            "runbooks",
            "Iso52016MatrixMergeRunbook.md");

        Assert.True(File.Exists(runbookPath), $"Merge runbook was not found: {runbookPath}");

        var runbook = File.ReadAllText(runbookPath);

        Assert.Contains("assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit", runbook);
        Assert.Contains("write-iso52016-matrix-merge-summary.ps1", runbook);
        Assert.Contains("git ls-files artifacts/iso52016/matrix-baselines", runbook);
        Assert.Contains("git ls-files artifacts/iso52016/matrix-merge-summary", runbook);
        Assert.Contains("should not be committed", runbook);
    }

    [Fact]
    public void MergeSummaryWriter_ProducesMarkdownAndJsonReviewArtifacts()
    {
        var repoRoot = FindRepositoryRoot();

        var scriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "write-iso52016-matrix-merge-summary.ps1");

        Assert.True(File.Exists(scriptPath), $"Merge summary script was not found: {scriptPath}");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("merge-summary.json", script);
        Assert.Contains("merge-summary.md", script);
        Assert.Contains("git log --oneline -20", script);
        Assert.Contains("assert-iso52016-matrix-release-ready.ps1", script);
        Assert.Contains("No exact StandardReference numerical equivalence claim.", script);
    }

    [Fact]
    public void GitIgnore_ExcludesGeneratedMatrixMergeSummaryArtifacts()
    {
        var repoRoot = FindRepositoryRoot();

        var gitIgnorePath = Path.Combine(repoRoot, ".gitignore");

        Assert.True(File.Exists(gitIgnorePath), ".gitignore was not found.");

        var gitIgnore = File.ReadAllText(gitIgnorePath);

        Assert.Contains("artifacts/iso52016/matrix-merge-summary/", gitIgnore);
    }

    [Theory]
    [InlineData("docs/releases/Iso52016MatrixSolverStageManifest.json")]
    [InlineData("docs/releases/Iso52016MatrixBaselineFixturesManifest.json")]
    public void MatrixManifests_ReferenceMergeEvidencePack(
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
            root.GetProperty("mergeEvidenceIntegrated").GetBoolean());

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

        Assert.Contains("docs/releases/Iso52016MatrixReleaseNotes.md", documentationFiles);
        Assert.Contains("docs/runbooks/Iso52016MatrixMergeRunbook.md", documentationFiles);
        Assert.Contains("scripts/iso52016/write-iso52016-matrix-merge-summary.ps1", verificationScripts);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixMergeEvidenceTests.cs", testGuards);
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