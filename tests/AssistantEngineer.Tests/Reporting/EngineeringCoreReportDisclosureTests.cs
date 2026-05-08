using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;

namespace AssistantEngineer.Tests.Reporting;

public class EngineeringCoreReportDisclosureTests
{
    [Fact]
    public void CoolingReportHasEngineeringCoreV1DisclosureByDefault()
    {
        var report = new BuildingCoolingReport();

        AssertDisclosure(report.CalculationDisclosure);
        Assert.Equal("Engineering-core v1 cooling design-point report.", report.CalculationDisclosure.CalculationScope);
        Assert.Contains(report.CalculationDisclosure.Warnings, item =>
            item.Contains("Cooling report uses engineering design-point", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HeatingReportHasEngineeringCoreV1DisclosureByDefault()
    {
        var report = new BuildingHeatingReport();

        AssertDisclosure(report.CalculationDisclosure);
        Assert.Equal("Engineering-core v1 heating design-point report.", report.CalculationDisclosure.CalculationScope);
        Assert.Contains(report.CalculationDisclosure.Warnings, item =>
            item.Contains("Heating report uses engineering design-point", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CoolingDisclosurePreservesCalculationMethodAndActualMethod()
    {
        var disclosure = EngineeringCoreReportDisclosures.CoolingDesignPoint(
            calculationMethod: "Simplified",
            actualMethod: "EngineeringCoreV1.DesignPointCooling");

        Assert.Equal("ClosedV1", disclosure.CoreStatus);
        Assert.Equal("Simplified", disclosure.CalculationMethod);
        Assert.Equal("EngineeringCoreV1.DesignPointCooling", disclosure.ActualMethod);
    }

    [Fact]
    public void HeatingDisclosurePreservesCalculationMethodAndActualMethod()
    {
        var disclosure = EngineeringCoreReportDisclosures.HeatingDesignPoint(
            calculationMethod: "En12831",
            actualMethod: "EngineeringCoreV1.DesignPointHeating");

        Assert.Equal("ClosedV1", disclosure.CoreStatus);
        Assert.Equal("En12831", disclosure.CalculationMethod);
        Assert.Equal("EngineeringCoreV1.DesignPointHeating", disclosure.ActualMethod);
    }

    [Fact]
    public void AnnualEnergyDisclosurePublishesTrueHourly8760Requirements()
    {
        var disclosure = EngineeringCoreReportDisclosures.AnnualEnergy(
            calculationMethod: "TrueHourlySimulation",
            actualMethod: "EngineeringCoreV1.TrueHourly8760");

        AssertDisclosure(disclosure);

        Assert.Contains(disclosure.Warnings, item =>
            item.Contains("EnergyDataSource=TrueHourlySimulation", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.Warnings, item =>
            item.Contains("IsTrueHourly8760=true", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.Warnings, item =>
            item.Contains("HourlyRecordCount=8760", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportDisclosuresExposeExplicitNonClaims()
    {
        var disclosure = EngineeringCoreReportDisclosures.CoolingDesignPoint();

        Assert.Contains(disclosure.ExplicitNonClaims, item =>
            item.Contains("StandardReference", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.ExplicitNonClaims, item =>
            item.Contains("EnergyPlus", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.ExplicitNonClaims, item =>
            item.Contains("ASHRAE 140", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.ExplicitNonClaims, item =>
            item.Contains("ISO 52016", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.ExplicitNonClaims, item =>
            item.Contains("latent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportDisclosuresExposeOutOfScopeAndDocumentationFiles()
    {
        var disclosure = EngineeringCoreReportDisclosures.HeatingDesignPoint();

        Assert.Contains("HVAC.LATENT_LOAD", disclosure.OutOfScopeV1);
        Assert.Contains("HVAC.MOISTURE_BALANCE", disclosure.OutOfScopeV1);

        Assert.Contains("docs/calculations/EngineeringCoreV1Scope.md", disclosure.DocumentationFiles);
        Assert.Contains("docs/calculations/EngineeringCoreV1ReleaseNotes.md", disclosure.DocumentationFiles);
        Assert.Contains("docs/calculations/EnergyPlusAshrae140ValidationPlan.md", disclosure.DocumentationFiles);
    }

    private static void AssertDisclosure(CalculationDisclosure disclosure)
    {
        Assert.Equal("ClosedV1", disclosure.CoreStatus);
        Assert.False(string.IsNullOrWhiteSpace(disclosure.CalculationScope));
        Assert.False(string.IsNullOrWhiteSpace(disclosure.CalculationMethod));
        Assert.False(string.IsNullOrWhiteSpace(disclosure.ActualMethod));
        Assert.NotEmpty(disclosure.Warnings);
        Assert.NotEmpty(disclosure.Assumptions);
        Assert.NotEmpty(disclosure.ExplicitNonClaims);
        Assert.NotEmpty(disclosure.OutOfScopeV1);
        Assert.NotEmpty(disclosure.DocumentationFiles);

        Assert.Contains(disclosure.ExplicitNonClaims, claim =>
            claim.Contains("No exact EnergyPlus", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(disclosure.ExplicitNonClaims, claim =>
            claim.Contains("No ASHRAE 140", StringComparison.OrdinalIgnoreCase));
    }
}