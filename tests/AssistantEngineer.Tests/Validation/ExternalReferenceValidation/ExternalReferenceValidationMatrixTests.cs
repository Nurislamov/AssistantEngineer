namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public class ExternalReferenceValidationMatrixTests
{
    [Fact]
    public void ExternalReferenceValidationMatrixContainsOnlyUniqueFeatureCodes()
    {
        var duplicateCodes = ExternalReferenceValidationMatrix.Features
            .GroupBy(feature => feature.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateCodes.Length == 0,
            $"equivalence feature codes must be unique: {string.Join(", ", duplicateCodes)}.");
    }

    [Fact]
    public void ImplementedReferenceFeaturesAreNotMarkedOutOfScope()
    {
        var violations = ExternalReferenceValidationMatrix.Features
            .Where(feature =>
                feature.ReferenceStatus == ReferenceFeatureStatus.Implemented &&
                feature.AssistantEngineerStatus == AssistantEngineerFeatureStatus.OutOfScope)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Implemented reference features must not be out of scope: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void NotImplementedReferenceFeaturesAreNotPriorityP0()
    {
        var violations = ExternalReferenceValidationMatrix.Features
            .Where(feature =>
                feature.ReferenceStatus == ReferenceFeatureStatus.NotImplemented &&
                feature.Priority == ExternalReferenceValidationPriority.P0)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Not implemented reference features must not be P0: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void AllP0FeaturesHaveAssistantEngineerArea()
    {
        var violations = ExternalReferenceValidationMatrix.Features
            .Where(feature =>
                feature.Priority == ExternalReferenceValidationPriority.P0 &&
                string.IsNullOrWhiteSpace(feature.AssistantEngineerArea))
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"P0 equivalence features must have AssistantEngineer area: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ExternalReferenceValidationMatrixContainsCoreIso52016Features()
    {
        var codes = ExternalReferenceValidationMatrix.Features
            .Select(feature => feature.Code)
            .ToHashSet(StringComparer.Ordinal);

        var requiredCodes = new[]
        {
            "ISO52016.HOURLY_HEATING_NEED",
            "ISO52016.HOURLY_COOLING_NEED",
            "ISO52016.MONTHLY_HEATING_COOLING_NEED",
            "ISO52016.INTERNAL_TEMPERATURE_HOURLY",
            "ISO52016.SENSIBLE_LOAD_HOURLY",
            "ISO52016.THERMAL_ZONES"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(
                requiredCode,
                codes);
        }
    }

    [Fact]
    public void ExternalReferenceValidationMatrixContainsCoreIso52010WeatherFeatures()
    {
        var codes = ExternalReferenceValidationMatrix.Features
            .Select(feature => feature.Code)
            .ToHashSet(StringComparer.Ordinal);

        var requiredCodes = new[]
        {
            "ISO52010.CLIMATE_CONVERSION",
            "ISO52010.SURFACE_IRRADIANCE",
            "WEATHER.EPW",
            "WEATHER.PVGIS"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(
                requiredCode,
                codes);
        }
    }

    [Fact]
    public void ExternalReferenceValidationMatrixContainsDhwAndPrimaryEnergyFeatures()
    {
        var codes = ExternalReferenceValidationMatrix.Features
            .Select(feature => feature.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(
            "DHW.EN12831_3",
            codes);

        Assert.Contains(
            "PRIMARY_ENERGY.EN15316_1",
            codes);
    }

    [Fact]
    public void ExternalReferenceValidationMatrixContainsFinalExternalReferenceValidationFunctions()
    {
        var codes = ExternalReferenceValidationMatrix.Features
            .Select(feature => feature.Code)
            .ToHashSet(StringComparer.Ordinal);

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
            Assert.Contains(requiredCode, codes);
    }

    [Fact]
    public void NoFeatureClaimsExternalExactMatchWithoutEvidence()
    {
        var violations = ExternalReferenceValidationMatrix.Features
            .Where(feature => feature.AssistantEngineerStatus == AssistantEngineerFeatureStatus.ExternalReferenceCovered)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ExternalReferenceCovered requires documented benchmark evidence: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void OutOfScopeFeaturesAreOnlyP3()
    {
        var violations = ExternalReferenceValidationMatrix.Features
            .Where(feature =>
                feature.AssistantEngineerStatus == AssistantEngineerFeatureStatus.OutOfScope &&
                feature.Priority != ExternalReferenceValidationPriority.P3)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Out-of-scope features must be P3 only: {string.Join(", ", violations)}.");
    }
}
