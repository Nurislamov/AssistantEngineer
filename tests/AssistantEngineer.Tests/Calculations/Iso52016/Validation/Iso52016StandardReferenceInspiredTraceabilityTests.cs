using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016StandardReferenceInspiredTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsWithExpectedStatusAndDependencies()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso52016StandardReferenceInspiredFixtureIntakeManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-VALIDATION-standard-reference-001", root.GetProperty("stageId").GetString());
        Assert.Equal("methodology-alignment-fixture-intake", root.GetProperty("status").GetString());

        var dependencies = root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("AE-VALIDATION-ISO52016-001", dependencies);
        Assert.Contains("AE-VALIDATION-ISO52016-002", dependencies);
    }

    [Fact]
    public void StageDocumentation_ExistsAndStatesNonParityScope()
    {
        var paths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "Iso52016StandardReferenceInspiredFixtureIntake.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "validation", "iso52016", "StandardReference-inspired-methodology-lane.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "validation", "iso52016", "standard-reference-inspired-simple-heating-naming-anchor.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "validation", "iso52016", "standard-reference-inspired-simple-cooling-naming-anchor.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "validation", "iso52016", "standard-reference-inspired-annual-seasonal-naming-anchor.md")
        };

        foreach (var path in paths)
        {
            Assert.True(File.Exists(path), $"Required document was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.Contains("not a equivalence claim", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no copied code", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no runtime dependency", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Registry_ContainsStandardReferenceInspiredStage()
    {
        var registryPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "verification",
            "Iso52016VerificationRegistry.json");

        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var stage = document.RootElement
            .GetProperty("stages")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == "AE-VALIDATION-standard-reference-001");

        var testFilters = stage.GetProperty("testFilters").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("FullyQualifiedName~Iso52016StandardReferenceInspired", testFilters);
        Assert.Contains(
            "No StandardReference numerical equivalence claim.",
            stage.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray());
    }
}

