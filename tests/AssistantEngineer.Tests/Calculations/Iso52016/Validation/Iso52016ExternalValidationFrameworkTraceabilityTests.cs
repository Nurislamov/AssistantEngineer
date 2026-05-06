using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

public sealed class Iso52016ExternalValidationFrameworkTraceabilityTests
{
    [Fact]
    public void FrameworkManifest_ExistsAndDeclaresClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso52016ExternalNumericalValidationFrameworkManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-VALIDATION-ISO52016-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-validation-framework", root.GetProperty("status").GetString());

        var claimBoundary = root
            .GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Validation/internal engineering anchors only.", claimBoundary);
        Assert.Contains("Manual independent reference fixtures only.", claimBoundary);
        Assert.Contains("No full ISO 52016 parity claim.", claimBoundary);
        Assert.Contains("No pyBuildingEnergy parity claim.", claimBoundary);
        Assert.Contains("No EnergyPlus parity claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 validation claim.", claimBoundary);
        Assert.Contains("ExternalParityCovered is not allowed in this stage.", claimBoundary);
    }

    [Fact]
    public void Registry_ContainsExternalValidationFrameworkStage()
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
            .Single(item => item.GetProperty("id").GetString() == "AE-VALIDATION-ISO52016-001");

        var testFilters = stage
            .GetProperty("testFilters")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Contains("FullyQualifiedName~Iso52016ExternalValidation", testFilters);

        var sourceFiles = stage
            .GetProperty("requiredSourceFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Validation/Iso52016/Iso52016ExternalValidationFixtureLoader.cs",
            sourceFiles);
        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Validation/Iso52016/Iso52016ExternalValidationComparisonEngine.cs",
            sourceFiles);
    }

    [Fact]
    public void FrameworkDocumentation_StatesNonClaimsAndIndependentScope()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "Iso52016ExternalNumericalValidationFramework.md");

        Assert.True(File.Exists(docPath), $"Framework document was not found: {docPath}");

        var doc = File.ReadAllText(docPath);

        Assert.Contains("AE-VALIDATION-ISO52016-001", doc);
        Assert.Contains("Validation/internal engineering anchors only.", doc);
        Assert.Contains("Manual independent reference fixtures only.", doc);
        Assert.Contains("No full ISO 52016 parity claim.", doc);
        Assert.Contains("No pyBuildingEnergy parity claim.", doc);
        Assert.Contains("No EnergyPlus parity claim.", doc);
        Assert.Contains("No ASHRAE 140 validation claim.", doc);
        Assert.Contains("pyBuildingEnergy-inspired methodology alignment lane", doc);
        Assert.Contains("not a parity claim", doc);
        Assert.Contains("not external certification", doc);
    }
}
