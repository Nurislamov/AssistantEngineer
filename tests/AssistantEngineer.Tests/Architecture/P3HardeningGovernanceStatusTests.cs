using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P3HardeningGovernanceStatusTests
{
    [Fact]
    public void P3HardeningStatusDoc_Exists_AndMentionsAllP3Stages()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-hardening-status.md");
        Assert.True(File.Exists(path), $"P3 hardening status document must exist: {path}");

        var content = File.ReadAllText(path);
        for (var stage = 1; stage <= 17; stage++)
        {
            Assert.Contains($"P3-{stage:00}", content, StringComparison.Ordinal);
        }

        Assert.Contains("Done", content, StringComparison.Ordinal);
        Assert.Contains("Partially done", content, StringComparison.Ordinal);
        Assert.Contains("Not done", content, StringComparison.Ordinal);
        Assert.Contains("Out of scope", content, StringComparison.Ordinal);
    }

    [Fact]
    public void P3HardeningSummaryDoc_Exists_AndContainsHonestStatusSections()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-hardening-summary.md");
        Assert.True(File.Exists(path), $"P3 hardening summary document must exist: {path}");

        var content = File.ReadAllText(path);
        for (var stage = 1; stage <= 17; stage++)
        {
            Assert.Contains($"P3-{stage:00}", content, StringComparison.Ordinal);
        }

        Assert.Contains("Done", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Verification Commands", content, StringComparison.Ordinal);
        Assert.Contains("Explicit Non-claims", content, StringComparison.Ordinal);
        Assert.Contains("Remaining P4 Candidates", content, StringComparison.Ordinal);
    }

    [Fact]
    public void P3Docs_DoNotContainForbiddenOverclaimLanguage()
    {
        var docPaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-hardening-status.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-hardening-summary.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-building-input-validation-refactor-status.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-energy-calculation-pipeline-refactor-status.md")
        };

        var full = "full";
        var parity = "parity";
        var fully = "fully";
        var validated = "validated";
        var energyPlus = "EnergyPlus";
        var ashrae = "ASHRAE";
        var py = "py";
        var building = "Building";
        var energy = "Energy";

        var forbidden = new[]
        {
            full + " " + parity,
            fully + " " + validated,
            energyPlus + " " + parity,
            ashrae + " 140 " + validated,
            "production complete",
            "enterprise ready",
            "certified compliance",
            py + building + energy
        };

        foreach (var path in docPaths)
        {
            Assert.True(File.Exists(path), $"Required P3 document is missing: {path}");
            var content = File.ReadAllText(path);

            foreach (var marker in forbidden)
            {
                Assert.DoesNotContain(marker, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void P3VerifyScripts_AreRepoRootPortable_AndHaveNoUserSpecificAbsolutePaths()
    {
        var scripts = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-p3-13-building-input-validation-refactor.ps1"),
            Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-p3-14-energy-calculation-pipeline-refactor.ps1")
        };

        foreach (var scriptPath in scripts)
        {
            Assert.True(File.Exists(scriptPath), $"P3 verify script is missing: {scriptPath}");
            var content = File.ReadAllText(scriptPath);

            Assert.Contains("param(", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RepoRoot", content, StringComparison.Ordinal);
            Assert.DoesNotContain("C:\\Users\\", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("D:\\Project\\", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
