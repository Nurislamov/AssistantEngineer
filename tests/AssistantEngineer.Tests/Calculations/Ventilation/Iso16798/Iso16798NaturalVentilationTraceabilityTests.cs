using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresStageStatusAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso16798NaturalVentilationStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-VENT-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("ISO16798-inspired natural ventilation engineering calculator.", claimBoundary);
        Assert.Contains("No full ISO 16798 compliance claim.", claimBoundary);
        Assert.Contains("No StandardReference equivalence claim.", claimBoundary);
        Assert.Contains("No EnergyPlus comparison workflow claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor claim.", claimBoundary);
        Assert.Contains("No external certification claim.", claimBoundary);
    }

    [Fact]
    public void RequiredDocsAndFixtures_Exist()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ventilation",
            "Iso16798NaturalVentilationCalculator.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");
        var docText = File.ReadAllText(docPath);
        Assert.DoesNotContain("ISO 16798 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASHRAE 140 / BESTEST-style validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 16798 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus comparison workflow");

        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "iso16798-natural", "closed-openings-zero-flow.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "iso16798-natural", "stack-only-temperature-delta.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "iso16798-natural", "wind-only-open-window.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "ventilation", "iso16798-natural", "stack-plus-wind-ach-clamped.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ExternalReferenceCovered", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("validated against StandardReference", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("matches StandardReference", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("same as StandardReference", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("copied from StandardReference", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ApplicationIntegrationManifestAndDoc_ExistWithNonClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso16798NaturalVentilationApplicationIntegrationManifest.json");
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ventilation",
            "Iso16798NaturalVentilationApplicationIntegration.md");

        Assert.True(File.Exists(manifestPath), $"Manifest file was not found: {manifestPath}");
        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-VENT-002", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-application-integration-anchor", root.GetProperty("status").GetString());

        var dependencies = root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("AE-VENT-001", dependencies);

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("Compatibility behavior preserved by default.", claimBoundary);
        Assert.Contains("No full ISO 16798 compliance claim.", claimBoundary);
        Assert.Contains("No StandardReference equivalence claim.", claimBoundary);
        Assert.Contains("No EnergyPlus comparison workflow claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor claim.", claimBoundary);
        Assert.DoesNotContain("ExternalReferenceCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);

        var docText = File.ReadAllText(docPath);
        Assert.Contains("AE-VENT-002", docText);
        Assert.Contains("Compatibility behavior preserved by default.", docText);
        Assert.DoesNotContain("ISO 16798 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 16798 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus comparison workflow");
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("must not", StringComparison.OrdinalIgnoreCase),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
