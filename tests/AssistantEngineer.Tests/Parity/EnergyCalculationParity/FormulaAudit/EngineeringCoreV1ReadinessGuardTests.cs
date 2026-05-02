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
            "HVAC.GROUND.SIMPLIFIED",
            "HVAC.ADJACENT_ZONE.SIMPLIFIED",
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
    public void EngineeringCoreV1HasNoP0OrP1FormulaBlockers()
    {
        var blockers = FormulaAuditMatrix.Features
            .Where(feature =>
                feature.Priority is FormulaAuditPriority.P0 or FormulaAuditPriority.P1 &&
                feature.Status != FormulaAuditStatus.ClosedV1 &&
                feature.Status != FormulaAuditStatus.OutOfScopeV1)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            blockers.Length == 0,
            $"Engineering-core v1 must not be declared closed while P0/P1 formula blockers remain: {string.Join(", ", blockers)}.");
    }

    [Fact]
    public void EngineeringCoreV1CanBeDeclaredClosedWithDocumentedLimitations()
    {
        var p0AndP1Features = FormulaAuditMatrix.Features
            .Where(feature => feature.Priority is FormulaAuditPriority.P0 or FormulaAuditPriority.P1)
            .ToArray();

        Assert.NotEmpty(p0AndP1Features);

        Assert.All(p0AndP1Features, feature =>
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

        var p0OrP1PartialCount = FormulaAuditMatrix.Features
            .Count(feature =>
                feature.Priority is FormulaAuditPriority.P0 or FormulaAuditPriority.P1 &&
                feature.Status == FormulaAuditStatus.Partial);

        var outOfScopeCount = FormulaAuditMatrix.Features
            .Count(feature => feature.Status == FormulaAuditStatus.OutOfScopeV1);

        var plannedValidationCount = FormulaAuditMatrix.Features
            .Count(feature => feature.Status == FormulaAuditStatus.PlannedValidation);

        Assert.True(
            closedV1Count >= 17,
            $"Expected at least 17 ClosedV1 features after validation/weather/annual/hourly-zone/ground/adjacent gates, but found {closedV1Count}.");

        Assert.Equal(0, p0OrP1PartialCount);
        Assert.True(outOfScopeCount >= 2);
        Assert.Equal(1, plannedValidationCount);
    }

    [Fact]
    public void EngineeringCoreV1HasNoRemainingPartialFormulaItems()
    {
        var partialFeatures = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.Partial)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            partialFeatures.Length == 0,
            $"Engineering-core v1 formula matrix must not contain remaining Partial items after ground and adjacent closure: {string.Join(", ", partialFeatures)}.");
    }
}