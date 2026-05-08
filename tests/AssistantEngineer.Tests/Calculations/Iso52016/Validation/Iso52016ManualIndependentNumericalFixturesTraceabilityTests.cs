using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ManualIndependentNumericalFixturesTraceabilityTests
{
    [Fact]
    public void StageManifest_DeclaresStep02ClaimBoundaryAndDependencies()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso52016ManualIndependentNumericalFixturesManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-VALIDATION-ISO52016-002", root.GetProperty("stageId").GetString());
        Assert.Equal("manual-independent-validation-anchors", root.GetProperty("status").GetString());

        var dependsOn = root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("AE-VALIDATION-ISO52016-001", dependsOn);
    }

    [Fact]
    public void StageDocumentation_StatesValidationOnlyAndNonClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "Iso52016ManualIndependentNumericalFixtures.md");

        Assert.True(File.Exists(docPath), $"Stage document was not found: {docPath}");

        var text = File.ReadAllText(docPath);
        Assert.Contains("AE-VALIDATION-ISO52016-002", text);
        Assert.Contains("Validation/internal engineering anchors only.", text);
        Assert.Contains("Manual independent reference fixtures only.", text);
        Assert.Contains("not a equivalence claim", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external certification", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registry_ContainsStep02WithManualIndependentTestFilter()
    {
        var registryPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "verification",
            "Iso52016VerificationRegistry.json");

        using var document = JsonDocument.Parse(File.ReadAllText(registryPath));
        var step = document.RootElement
            .GetProperty("stages")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == "AE-VALIDATION-ISO52016-002");

        var filters = step.GetProperty("testFilters").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("FullyQualifiedName~Iso52016ManualIndependent", filters);
    }
}
