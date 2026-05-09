namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370VirtualGroundTraceabilityTests
{
    [Fact]
    public void VirtualGroundDocumentation_ExistsWithClaimBoundaryAndNoUnsupportedClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "Iso13370VirtualGround.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        var docText = File.ReadAllText(docPath);
        Assert.Contains("ISO13370-style virtual ground calculation", docText, StringComparison.Ordinal);
        Assert.Contains("internal analytical anchor", docText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No full validation claim.", docText, StringComparison.Ordinal);
        Assert.Contains("Not full ISO 13370 compliance.", docText, StringComparison.Ordinal);
        Assert.Contains("No external validation claim.", docText, StringComparison.Ordinal);
        Assert.DoesNotContain("full ISO13370 compliance", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("parity", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("equivalence", docText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VirtualGroundFixtures_ExistAndContainInternalAnchorBoundary()
    {
        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "slab-on-ground-basic.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "insulated-slab.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "high-conductivity-ground.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ground", "iso13370", "thermal-bridge-enabled.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.Contains("internal analytical anchor", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No full ISO 13370 compliance claim.", text, StringComparison.Ordinal);
            Assert.Contains("No external validation claim.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExternalReferenceCovered", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("parity", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("equivalence", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
