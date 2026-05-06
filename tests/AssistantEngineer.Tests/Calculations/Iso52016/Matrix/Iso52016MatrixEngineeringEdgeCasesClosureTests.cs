namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixEngineeringEdgeCasesClosureTests
{
    [Fact]
    public void ClosureDocs_KeepClaimsHonest()
    {
        var repoRoot = FindRepositoryRoot();

        var releaseNotesPath = Path.Combine(
            repoRoot,
            "docs",
            "releases",
            "Iso52016MatrixEngineeringEdgeCasesReleaseNotes.md");

        var mergeRunbookPath = Path.Combine(
            repoRoot,
            "docs",
            "runbooks",
            "Iso52016MatrixEngineeringEdgeCasesMergeRunbook.md");

        Assert.True(File.Exists(releaseNotesPath), $"Release notes were not found: {releaseNotesPath}");
        Assert.True(File.Exists(mergeRunbookPath), $"Merge runbook was not found: {mergeRunbookPath}");

        var releaseNotes = File.ReadAllText(releaseNotesPath);
        var mergeRunbook = File.ReadAllText(mergeRunbookPath);

        Assert.Contains("Engineering edge-case hardening only.", releaseNotes);
        Assert.Contains("Validation anchors only, not full parity.", releaseNotes);
        Assert.Contains("No pyBuildingEnergy parity claim.", releaseNotes);
        Assert.Contains("No EnergyPlus parity claim.", releaseNotes);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", releaseNotes);
        Assert.Contains("No full ISO 52016 parity claim.", releaseNotes);

        Assert.Contains("Engineering edge-case hardening only.", mergeRunbook);
        Assert.Contains("Validation anchors only, not full parity.", mergeRunbook);
        Assert.Contains("must not be committed", mergeRunbook);
        Assert.Contains("assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1", mergeRunbook);
    }

    [Fact]
    public void GeneratedEngineeringEdgeCaseArtifacts_AreIgnoredAndGuarded()
    {
        var repoRoot = FindRepositoryRoot();

        var gitIgnorePath = Path.Combine(repoRoot, ".gitignore");

        Assert.True(File.Exists(gitIgnorePath), ".gitignore was not found.");

        var gitIgnore = File.ReadAllText(gitIgnorePath);

        Assert.Contains("artifacts/iso52016/engineering-edge-cases/", gitIgnore);
        Assert.Contains("artifacts/iso52016/engineering-edge-cases", ReadIso52016VerificationRegistry());
    }

    [Fact]
    public void MergeSummaryScript_WritesGeneratedArtifactOnly()
    {
        var repoRoot = FindRepositoryRoot();

        var summaryScriptPath = Path.Combine(
            repoRoot,
            "scripts",
            "iso52016",
            "write-iso52016-matrix-engineering-edge-cases-merge-summary.ps1");

        Assert.True(File.Exists(summaryScriptPath), $"Merge summary script was not found: {summaryScriptPath}");

        var script = File.ReadAllText(summaryScriptPath);

        Assert.Contains(@"artifacts\iso52016\engineering-edge-cases\merge-summary.json", script);
        Assert.Contains("generatedArtifactsCommitted", script);
        Assert.Contains("Engineering edge-case hardening only.", script);
        Assert.Contains("Validation anchors only, not full parity.", script);
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
