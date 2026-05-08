namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public class ExternalReferenceValidationDocumentationTests
{
    [Fact]
    public void ExternalReferenceValidationPlanExists()
    {
        var path = GetExternalReferenceValidationPlanPath();

        Assert.True(
            File.Exists(path),
            $"External Reference Validation Plan was not found: {path}");
    }

    [Fact]
    public void ExternalReferenceValidationPlanContainsCoreSections()
    {
        var text = File.ReadAllText(GetExternalReferenceValidationPlanPath());

        var requiredSections = new[]
        {
            "# External Reference Validation Plan",
            "## Naming Rule",
            "## Claim Boundary",
            "## P0 - Core Calculation Scope",
            "## P1 - Scope Expansion",
            "## P3 - Out of Scope",
            "## Real Application Pipeline",
            "## Implementation Order",
            "## Fixture Policy",
            "## Tolerance Policy"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(requiredSection, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ExternalReferenceValidationPlanContainsMatrixCoreCodes()
    {
        var text = File.ReadAllText(GetExternalReferenceValidationPlanPath());

        var requiredCodes = new[]
        {
            "STANDARD_REFERENCE.TRANSMISSION_HEAT_TRANSFER",
            "STANDARD_REFERENCE.WINDOW_SOLAR_GAINS",
            "STANDARD_REFERENCE.VENTILATION_INFILTRATION_LOADS",
            "STANDARD_REFERENCE.INTERNAL_GAINS",
            "STANDARD_REFERENCE.ROOM_HEATING_LOAD",
            "STANDARD_REFERENCE.ROOM_COOLING_LOAD",
            "STANDARD_REFERENCE.THERMAL_ZONE_AGGREGATION",
            "STANDARD_REFERENCE.FLOOR_AGGREGATION",
            "STANDARD_REFERENCE.BUILDING_AGGREGATION",
            "STANDARD_REFERENCE.ANNUAL_ENERGY_BALANCE",
            "STANDARD_REFERENCE.SIGNED_COMPONENT_BALANCE",
            "STANDARD_REFERENCE.DHW_DEMAND",
            "STANDARD_REFERENCE.SYSTEM_ENERGY",
            "STANDARD_REFERENCE.EQUIPMENT_SIZING_INTEGRATION"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(requiredCode, text, StringComparison.Ordinal);
        }
    }

    private static string GetExternalReferenceValidationPlanPath() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "architecture",
            "ExternalReferenceValidationPlan.md");
}
