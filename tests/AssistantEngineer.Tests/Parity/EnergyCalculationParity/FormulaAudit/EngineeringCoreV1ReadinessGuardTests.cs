namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ReadinessGuardTests
{
    [Fact]
    public void EngineeringCoreV1ClosedFormulaGatesAreExplicitlyClosed()
    {
        var requiredClosedV1Ids = new[]
        {
            "HVAC.TRANSMISSION.SIMPLE_UA",
            "HVAC.VENTILATION.SENSIBLE_AIRFLOW",
            "HVAC.INTERNAL_GAINS.SENSIBLE",
            "HVAC.WINDOW_SOLAR.SIMPLE_SHGC",
            "HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC",
            "HVAC.ROOM_LOAD.DESIGN_POINT",
            "HVAC.AGGREGATION.LOAD_SUMMARY",
            "HVAC.ANNUAL_ENERGY.HOURLY_KWH",
            "WEATHER.EPW_8760",
            "WEATHER.PVGIS_8760",
            "HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC",
            "HVAC.THERMAL_ZONE.SINGLE_ZONE",
            "HVAC.DHW.SIMPLIFIED",
            "HVAC.SYSTEM_ENERGY.SIMPLIFIED",
            "HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN"
        };

        foreach (var calculationId in requiredClosedV1Ids)
        {
            var feature = Assert.Single(
                FormulaAuditMatrix.Features,
                item => item.CalculationId == calculationId);

            Assert.Equal(
                FormulaAuditStatus.ClosedV1,
                feature.Status);
        }
    }

    [Fact]
    public void EngineeringCoreV1HasNoP0FormulaBlockers()
    {
        var p0Blockers = FormulaAuditMatrix.Features
            .Where(feature =>
                feature.Priority == FormulaAuditPriority.P0 &&
                feature.Status != FormulaAuditStatus.ClosedV1 &&
                feature.Status != FormulaAuditStatus.OutOfScopeV1)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            p0Blockers.Length == 0,
            $"Engineering-core v1 must not be declared closed while P0 formula blockers remain: {string.Join(", ", p0Blockers)}.");
    }

    [Fact]
    public void EngineeringCoreV1CanBeDeclaredClosedWithDocumentedLimitations()
    {
        var p0Features = FormulaAuditMatrix.Features
            .Where(feature => feature.Priority == FormulaAuditPriority.P0)
            .ToArray();

        Assert.NotEmpty(p0Features);

        Assert.All(p0Features, feature =>
        {
            Assert.Equal(
                FormulaAuditStatus.ClosedV1,
                feature.Status);

            Assert.False(
                string.IsNullOrWhiteSpace(feature.Formula));

            Assert.False(
                string.IsNullOrWhiteSpace(feature.Units));

            Assert.False(
                string.IsNullOrWhiteSpace(feature.Tests));

            Assert.False(
                string.IsNullOrWhiteSpace(feature.Diagnostics));

            Assert.False(
                string.IsNullOrWhiteSpace(feature.Limitations));
        });
    }

    [Fact]
    public void EngineeringCoreV1FutureValidationIsNotAFormulaClosureBlocker()
    {
        var validationFeature = Assert.Single(
            FormulaAuditMatrix.Features,
            item => item.CalculationId == "VALIDATION.ENERGYPLUS_ASHRAE140");

        Assert.Equal(
            FormulaAuditStatus.PlannedValidation,
            validationFeature.Status);

        Assert.Equal(
            FormulaAuditPriority.P2,
            validationFeature.Priority);

        Assert.Contains(
            "Not required to close formula implementation v1",
            validationFeature.Limitations,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EngineeringCoreV1OutOfScopeItemsAreNotReadinessBlockers()
    {
        var outOfScopeFeatures = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.OutOfScopeV1)
            .ToArray();

        Assert.NotEmpty(outOfScopeFeatures);

        Assert.All(outOfScopeFeatures, feature =>
        {
            Assert.Equal(
                FormulaAuditPriority.P3,
                feature.Priority);

            Assert.Contains(
                "not required",
                feature.Limitations,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EngineeringCoreV1CurrentReadinessSummaryIsConsistent()
    {
        var closedV1Count = FormulaAuditMatrix.Features
            .Count(feature => feature.Status == FormulaAuditStatus.ClosedV1);

        var p0PartialCount = FormulaAuditMatrix.Features
            .Count(feature =>
                feature.Priority == FormulaAuditPriority.P0 &&
                feature.Status == FormulaAuditStatus.Partial);

        var outOfScopeCount = FormulaAuditMatrix.Features
            .Count(feature => feature.Status == FormulaAuditStatus.OutOfScopeV1);

        var plannedValidationCount = FormulaAuditMatrix.Features
            .Count(feature => feature.Status == FormulaAuditStatus.PlannedValidation);

        Assert.True(
            closedV1Count >= 15,
            $"Expected at least 15 ClosedV1 features after validation/weather/annual/hourly-zone gates, but found {closedV1Count}.");

        Assert.Equal(0, p0PartialCount);
        Assert.True(outOfScopeCount >= 2);
        Assert.Equal(1, plannedValidationCount);
    }

    [Fact]
    public void EngineeringCoreV1RemainingPartialItemsAreNotP0FormulaBlockers()
    {
        var partialFeatures = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.Partial)
            .ToArray();

        Assert.NotEmpty(partialFeatures);

        Assert.All(partialFeatures, feature =>
        {
            Assert.NotEqual(
                FormulaAuditPriority.P0,
                feature.Priority);

            Assert.Contains(
                "does not claim",
                feature.Limitations,
                StringComparison.OrdinalIgnoreCase);
        });
    }
}