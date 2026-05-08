namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class FormulaAuditMatrixTests
{
    [Fact]
    public void FormulaAuditMatrixContainsOnlyUniqueCalculationIds()
    {
        var duplicateIds = FormulaAuditMatrix.Features
            .GroupBy(feature => feature.CalculationId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateIds.Length == 0,
            $"Formula audit calculation ids must be unique: {string.Join(", ", duplicateIds)}.");
    }

    [Fact]
    public void FormulaAuditMatrixContainsEngineeringCoreV1P0Formulas()
    {
        var ids = FormulaAuditMatrix.Features
            .Select(feature => feature.CalculationId)
            .ToHashSet(StringComparer.Ordinal);

        var requiredIds = new[]
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
            "HVAC.THERMAL_ZONE.SINGLE_ZONE"
        };

        foreach (var requiredId in requiredIds)
        {
            Assert.Contains(requiredId, ids);
        }
    }

    [Fact]
    public void ClosedV1FeaturesHaveFormulaUnitsImplementationDiagnosticsTestsAndLimitations()
    {
        var violations = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.ClosedV1)
            .Where(feature =>
                string.IsNullOrWhiteSpace(feature.Formula) ||
                string.IsNullOrWhiteSpace(feature.Units) ||
                string.IsNullOrWhiteSpace(feature.SourcePrinciple) ||
                string.IsNullOrWhiteSpace(feature.ImplementationArea) ||
                string.IsNullOrWhiteSpace(feature.Diagnostics) ||
                string.IsNullOrWhiteSpace(feature.Tests) ||
                string.IsNullOrWhiteSpace(feature.Limitations))
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ClosedV1 features must fully document formula, units, source principle, implementation, diagnostics, tests and limitations: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void P0FeaturesAreNotPlannedValidation()
    {
        var violations = FormulaAuditMatrix.Features
            .Where(feature =>
                feature.Priority == FormulaAuditPriority.P0 &&
                feature.Status == FormulaAuditStatus.PlannedValidation)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"P0 calculation formulas must not be only planned validation: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void OutOfScopeFeaturesAreOnlyP3()
    {
        var violations = FormulaAuditMatrix.Features
            .Where(feature =>
                feature.Status == FormulaAuditStatus.OutOfScopeV1 &&
                feature.Priority != FormulaAuditPriority.P3)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Out-of-scope v1 features must be P3 only: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void SimplifiedIsoInspiredFeaturesDoNotClaimFullIsoOrUnsupportedEnergyPlusClaims()
    {
        var simplifiedFeatureIds = new[]
        {
            "HVAC.GROUND.SIMPLIFIED",
            "HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC",
            "HVAC.SYSTEM_ENERGY.SIMPLIFIED",
            "HVAC.DHW.SIMPLIFIED",
            "HVAC.ADJACENT_ZONE.SIMPLIFIED"
        };

        var simplifiedFeatures = FormulaAuditMatrix.Features
            .Where(feature => simplifiedFeatureIds.Contains(feature.CalculationId, StringComparer.Ordinal))
            .ToArray();

        foreach (var feature in simplifiedFeatures)
        {
            Assert.Contains(
                "does not claim",
                feature.Limitations,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void WeatherImportGatesAreClosedV1After8760FixtureCoverage()
    {
        var closedWeatherIds = new[]
        {
            "WEATHER.EPW_8760",
            "WEATHER.PVGIS_8760"
        };

        foreach (var calculationId in closedWeatherIds)
        {
            var feature = Assert.Single(
                FormulaAuditMatrix.Features,
                item => item.CalculationId == calculationId);

            Assert.Equal(
                FormulaAuditStatus.ClosedV1,
                feature.Status);

            Assert.Contains(
                "8760",
                feature.Tests,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AnnualEnergyHourlyKWhGateIsClosedAfterEndToEnd8760ScenarioCoverage()
    {
        var feature = Assert.Single(
            FormulaAuditMatrix.Features,
            item => item.CalculationId == "HVAC.ANNUAL_ENERGY.HOURLY_KWH");

        Assert.Equal(
            FormulaAuditStatus.ClosedV1,
            feature.Status);

        Assert.Contains(
            "end-to-end true hourly 8760 scenario",
            feature.Tests,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "does not claim full ISO 52016",
            feature.Limitations,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnergyPlusAshrae140IsPlannedValidationNotV1FormulaGate()
    {
        var feature = Assert.Single(
            FormulaAuditMatrix.Features,
            item => item.CalculationId == "VALIDATION.ENERGYPLUS_ASHRAE140");

        Assert.Equal(
            FormulaAuditStatus.PlannedValidation,
            feature.Status);

        Assert.Contains(
            "Not required to close formula implementation v1",
            feature.Limitations,
            StringComparison.Ordinal);
    }
}