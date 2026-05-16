using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class ValidationTolerancePolicyDocumentTests
{
    [Fact]
    public void TolerancePolicyDocumentExists()
    {
        Assert.True(File.Exists(TolerancePolicyPath), $"Validation tolerance policy document is missing: {TolerancePolicyPath}");
    }

    [Fact]
    public void TolerancePolicyContainsRequiredSections()
    {
        var content = File.ReadAllText(TolerancePolicyPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## General numeric comparison rule",
            "## Zero and near-zero values",
            "## Units policy",
            "## Recommended default tolerances",
            "## Rounding policy",
            "## Tolerance file schema",
            "## Fixture requirements",
            "## Promotion rules"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void TolerancePolicyContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(TolerancePolicyPath);

        var requiredNonClaims = new[]
        {
            "No ASHRAE 140 compliance claim",
            "No exact EnergyPlus equivalence claim",
            "No pyBuilding\\u0045nergy parity claim",
            "No full ISO/EN compliance claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredNonClaims)
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TolerancePolicyContainsRelativeFloorGuidance()
    {
        var content = File.ReadAllText(TolerancePolicyPath);

        Assert.True(
            content.Contains("relativeFloor = 1e-9", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("\"relativeFloor\": 1e-9", StringComparison.OrdinalIgnoreCase),
            "Tolerance policy must declare relativeFloor = 1e-9 guidance.");
    }

    [Fact]
    public void TolerancePolicyContainsRequiredUnits()
    {
        var content = File.ReadAllText(TolerancePolicyPath);

        var requiredUnits = new[]
        {
            "W",
            "kW",
            "Wh",
            "kWh",
            "\u00B0C",
            "K",
            "m\u00B2",
            "m\u00B3",
            "m\u00B3/h",
            "ACH",
            "W/(m\u00B2\u00B7K)",
            "W/m\u00B2"
        };

        foreach (var unit in requiredUnits)
            Assert.Contains(unit, content, StringComparison.Ordinal);
    }

    [Fact]
    public void TolerancePolicySchemaDescriptorExistsAndContainsRequiredFields()
    {
        Assert.True(File.Exists(TolerancePolicySchemaPath), $"Tolerance policy schema descriptor is missing: {TolerancePolicySchemaPath}");

        using var schema = JsonDocument.Parse(File.ReadAllText(TolerancePolicySchemaPath));
        var root = schema.RootElement;

        var requiredFields = root
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("relativeFloor", requiredFields);
        Assert.Contains("relativeTolerance", requiredFields);
        Assert.Contains("absoluteTolerances", requiredFields);
        Assert.Contains("rationale", requiredFields);

        var fieldDescriptors = root.GetProperty("fieldDescriptors");
        Assert.True(fieldDescriptors.TryGetProperty("relativeFloor", out _));
        Assert.True(fieldDescriptors.TryGetProperty("relativeTolerance", out _));
        Assert.True(fieldDescriptors.TryGetProperty("absoluteTolerances", out _));
        Assert.True(fieldDescriptors.TryGetProperty("rationale", out _));
    }

    [Fact]
    public void ManualFixturesIndexReferencesTolerancePolicy()
    {
        var content = File.ReadAllText(ManualFixtureIndexPath);

        Assert.Contains("docs/validation/validation-tolerance-policy.md", content, StringComparison.Ordinal);
        Assert.Contains("strict exact-arithmetic tolerances", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExternalRoadmapReferencesTolerancePolicy()
    {
        var content = File.ReadAllText(ExternalRoadmapPath);

        Assert.Contains("docs/validation/validation-tolerance-policy.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TolerancePolicyDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                TolerancePolicyPath,
                TolerancePolicySchemaPath,
                ManualFixtureIndexPath,
                ExternalRoadmapPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string TolerancePolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-tolerance-policy.md");

    private static string TolerancePolicySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-tolerance-policy.schema.json");

    private static string ManualFixtureIndexPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "manual-engineering-fixtures.md");

    private static string ExternalRoadmapPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "external-numerical-validation-roadmap.md");
}
