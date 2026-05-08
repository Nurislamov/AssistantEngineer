using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixApplicationIntegrationHardeningClosureTests
{
    [Fact]
    public void ClosureDocs_KeepClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();

        var releaseNotesPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixApplicationIntegrationHardeningReleaseNotes.md");

        var mergeRunbookPath = Path.Combine(
            repoRoot,
            "docs",
            "runbooks",
            "Iso52016MatrixApplicationIntegrationHardeningMergeRunbook.md");

        Assert.True(File.Exists(releaseNotesPath), $"Release notes were not found: {releaseNotesPath}");
        Assert.True(File.Exists(mergeRunbookPath), $"Merge runbook was not found: {mergeRunbookPath}");

        var releaseNotes = File.ReadAllText(releaseNotesPath);
        var mergeRunbook = File.ReadAllText(mergeRunbookPath);

        Assert.Contains("Application integration hardening only.", releaseNotes);
        Assert.Contains("Validation anchors only, not full equivalence claim.", releaseNotes);
        Assert.Contains("No StandardReference equivalence claim.", releaseNotes);
        Assert.Contains("No EnergyPlus comparison workflow claim.", releaseNotes);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor coverage claim.", releaseNotes);
        Assert.Contains("No full ISO 52016 equivalence claim.", releaseNotes);

        Assert.Contains("Application integration hardening only.", mergeRunbook);
        Assert.Contains("Validation anchors only, not full equivalence claim.", mergeRunbook);
        Assert.Contains("must not be committed", mergeRunbook);
        Assert.Contains("assert-iso52016-matrix-application-integration-hardening-release-ready.ps1", mergeRunbook);
    }

    [Fact]
    public void ClosureManifest_ReferencesReleaseNotesRunbookSummaryAndClosureGuard()
    {
        var repoRoot = FindRepositoryRoot();

        var manifestPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixApplicationIntegrationHardeningReleaseManifest.json");

        Assert.True(File.Exists(manifestPath), $"Release manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("ApplicationIntegrationHardeningOnly", root.GetProperty("scope").GetString());
        Assert.False(root.GetProperty("generatedArtifactsCommitted").GetBoolean());
        Assert.True(root.GetProperty("mergeEvidenceIntegrated").GetBoolean());

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

        Assert.Contains("docs/releases/Iso52016MatrixApplicationIntegrationHardeningReleaseNotes.md", documentationFiles);
        Assert.Contains("docs/runbooks/Iso52016MatrixApplicationIntegrationHardeningMergeRunbook.md", documentationFiles);
        Assert.Contains("scripts/iso52016/write-iso52016-matrix-application-integration-hardening-merge-summary.ps1", verificationScripts);
        Assert.Contains("tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixApplicationIntegrationHardeningClosureTests.cs", testGuards);
    }

    [Fact]
    public void GeneratedApplicationIntegrationHardeningArtifacts_AreIgnoredAndGuarded()
    {
        var repoRoot = FindRepositoryRoot();

        var gitIgnorePath = Path.Combine(repoRoot, ".gitignore");

        Assert.True(File.Exists(gitIgnorePath), ".gitignore was not found.");

        var gitIgnore = File.ReadAllText(gitIgnorePath);

        Assert.Contains("artifacts/iso52016/application-integration-hardening/", gitIgnore);
        Assert.Contains("artifacts/iso52016/application-integration-hardening", ReadIso52016VerificationRegistry());
    }

    [Fact]
    public void MergeSummaryScript_WritesGeneratedArtifactOnly()
    {
        var repoRoot = FindRepositoryRoot();

        var summaryScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "write-iso52016-matrix-application-integration-hardening-merge-summary.ps1");

        Assert.True(File.Exists(summaryScriptPath), $"Merge summary script was not found: {summaryScriptPath}");

        var script = File.ReadAllText(summaryScriptPath);

        Assert.Contains("artifacts\\iso52016\\application-integration-hardening\\merge-summary.json", script);
        Assert.Contains("generatedArtifactsCommitted", script);
        Assert.Contains("ApplicationIntegrationHardeningOnly", script);
        Assert.Contains("Validation anchors only, not full equivalence claim.", script);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(directory.FullName, "src", "Backend", "AssistantEngineer.Modules.Calculations");
            var tests = Path.Combine(directory.FullName, "tests", "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AssistantEngineer repository root from test base directory.");
    }
}
