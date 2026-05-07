using AssistantEngineer.Modules.Calculations.Application.Facades;

namespace AssistantEngineer.Tests.Calculations.CoreStatus;

public class EngineeringCoreStatusFacadeTests
{
    [Fact]
    public void GetEngineeringCoreV1StatusReturnsClosedV1Status()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);

        Assert.Equal("AssistantEngineer Engineering Core", result.Value.CoreName);
        Assert.Equal("v1", result.Value.Version);
        Assert.Equal("ClosedV1", result.Value.Status);
        Assert.True(result.Value.FormulaGatesClosed);
        Assert.True(result.Value.Weather8760GatesClosed);
        Assert.True(result.Value.AnnualHourly8760GateClosed);
        Assert.True(result.Value.SuccessfulResultsMustNotContainErrorDiagnostics);
    }

    [Fact]
    public void GetEngineeringCoreV1StatusListsAllClosedFormulaGates()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(17, result.Value.FormulaGates.Count);

        Assert.All(result.Value.FormulaGates, gate =>
        {
            Assert.Equal("ClosedV1", gate.Status);
            Assert.False(string.IsNullOrWhiteSpace(gate.CalculationId));
            Assert.False(string.IsNullOrWhiteSpace(gate.Name));
            Assert.False(string.IsNullOrWhiteSpace(gate.Priority));
            Assert.False(string.IsNullOrWhiteSpace(gate.Scope));
            Assert.False(string.IsNullOrWhiteSpace(gate.Limitation));
        });

        var ids = result.Value.FormulaGates
            .Select(gate => gate.CalculationId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("WEATHER.EPW_8760", ids);
        Assert.Contains("WEATHER.PVGIS_8760", ids);
        Assert.Contains("HVAC.ANNUAL_ENERGY.HOURLY_KWH", ids);
        Assert.Contains("HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC", ids);
        Assert.Contains("HVAC.THERMAL_ZONE.SINGLE_ZONE", ids);
        Assert.Contains("HVAC.GROUND.SIMPLIFIED", ids);
        Assert.Contains("HVAC.ADJACENT_ZONE.SIMPLIFIED", ids);
    }

    [Fact]
    public void GetEngineeringCoreV1StatusKeepsExplicitNonClaimsVisible()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);

        Assert.Contains(result.Value.ExplicitNonClaims, claim =>
            claim.Contains("pyBuildingEnergy", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result.Value.ExplicitNonClaims, claim =>
            claim.Contains("EnergyPlus", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result.Value.ExplicitNonClaims, claim =>
            claim.Contains("ASHRAE 140", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result.Value.ExplicitNonClaims, claim =>
            claim.Contains("ISO 52016", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(result.Value.ExplicitNonClaims, claim =>
            claim.Contains("latent", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("SystemEnergyEngine compatibility path remains default.", result.Value.ExplicitNonClaims);
        Assert.Contains("EN15316-inspired modular chain is opt-in.", result.Value.ExplicitNonClaims);
        Assert.Contains("ISO12831-3-inspired DHW path is opt-in.", result.Value.ExplicitNonClaims);
    }

    [Fact]
    public void GetEngineeringCoreV1StatusPublishesAnnual8760GateFlags()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);

        Assert.Contains("EnergyDataSource = TrueHourlySimulation", result.Value.RequiredAnnual8760Flags);
        Assert.Contains("IsTrueHourly8760 = true", result.Value.RequiredAnnual8760Flags);
        Assert.Contains("HourlyRecordCount = 8760", result.Value.RequiredAnnual8760Flags);
    }

    [Fact]
    public void GetEngineeringCoreV1StatusPublishesOutOfScopeAndPlannedValidation()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);

        Assert.Contains("HVAC.LATENT_LOAD", result.Value.OutOfScopeV1);
        Assert.Contains("HVAC.MOISTURE_BALANCE", result.Value.OutOfScopeV1);
        Assert.Contains("VALIDATION.ENERGYPLUS_ASHRAE140", result.Value.PlannedValidation);
    }

    [Fact]
    public void GetEngineeringCoreV1StatusPublishesDocumentationFiles()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1Status();

        Assert.True(result.IsSuccess, result.Error);

        Assert.Contains("docs/calculations/EngineeringCoreV1Scope.md", result.Value.DocumentationFiles);
        Assert.Contains("docs/calculations/EngineeringCoreV1ReleaseNotes.md", result.Value.DocumentationFiles);
        Assert.Contains("docs/calculations/EnergyPlusAshrae140ValidationPlan.md", result.Value.DocumentationFiles);
    }
}
