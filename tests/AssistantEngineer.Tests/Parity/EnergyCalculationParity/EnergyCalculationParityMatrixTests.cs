namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

public class EnergyCalculationParityMatrixTests
{
    [Fact]
    public void ParityMatrixContainsOnlyUniqueFeatureCodes()
    {
        var duplicateCodes = EnergyCalculationParityMatrix.Features
            .GroupBy(feature => feature.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateCodes.Length == 0,
            $"Parity feature codes must be unique: {string.Join(", ", duplicateCodes)}.");
    }

    [Fact]
    public void ImplementedReferenceFeaturesAreNotMarkedOutOfScope()
    {
        var violations = EnergyCalculationParityMatrix.Features
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
        var violations = EnergyCalculationParityMatrix.Features
            .Where(feature =>
                feature.ReferenceStatus == ReferenceFeatureStatus.NotImplemented &&
                feature.Priority == EnergyCalculationParityPriority.P0)
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
        var violations = EnergyCalculationParityMatrix.Features
            .Where(feature =>
                feature.Priority == EnergyCalculationParityPriority.P0 &&
                string.IsNullOrWhiteSpace(feature.AssistantEngineerArea))
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"P0 parity features must have AssistantEngineer area: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void ParityMatrixContainsCoreIso52016Features()
    {
        var codes = EnergyCalculationParityMatrix.Features
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
    public void ParityMatrixContainsCoreIso52010WeatherFeatures()
    {
        var codes = EnergyCalculationParityMatrix.Features
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
    public void ParityMatrixContainsDhwAndPrimaryEnergyFeatures()
    {
        var codes = EnergyCalculationParityMatrix.Features
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
    public void ParityMatrixContainsFinalEnergyCalculationParityFunctions()
    {
        var codes = EnergyCalculationParityMatrix.Features
            .Select(feature => feature.Code)
            .ToHashSet(StringComparer.Ordinal);

        var requiredCodes = new[]
        {
            "ENERGY_CALCULATION_PARITY.TRANSMISSION_HEAT_TRANSFER",
            "ENERGY_CALCULATION_PARITY.WINDOW_SOLAR_GAINS",
            "ENERGY_CALCULATION_PARITY.VENTILATION_INFILTRATION_LOADS",
            "ENERGY_CALCULATION_PARITY.INTERNAL_GAINS",
            "ENERGY_CALCULATION_PARITY.ROOM_HEATING_LOAD",
            "ENERGY_CALCULATION_PARITY.ROOM_COOLING_LOAD",
            "ENERGY_CALCULATION_PARITY.THERMAL_ZONE_AGGREGATION",
            "ENERGY_CALCULATION_PARITY.FLOOR_AGGREGATION",
            "ENERGY_CALCULATION_PARITY.BUILDING_AGGREGATION",
            "ENERGY_CALCULATION_PARITY.ANNUAL_ENERGY_BALANCE",
            "ENERGY_CALCULATION_PARITY.SIGNED_COMPONENT_BALANCE",
            "ENERGY_CALCULATION_PARITY.DHW_DEMAND",
            "ENERGY_CALCULATION_PARITY.SYSTEM_ENERGY",
            "ENERGY_CALCULATION_PARITY.EQUIPMENT_SIZING_INTEGRATION"
        };

        foreach (var requiredCode in requiredCodes)
            Assert.Contains(requiredCode, codes);
    }

    [Fact]
    public void NoFeatureClaimsExternalParityWithoutEvidence()
    {
        var violations = EnergyCalculationParityMatrix.Features
            .Where(feature => feature.AssistantEngineerStatus == AssistantEngineerFeatureStatus.ExternalParityCovered)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ExternalParityCovered requires documented benchmark evidence: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void OutOfScopeFeaturesAreOnlyP3()
    {
        var violations = EnergyCalculationParityMatrix.Features
            .Where(feature =>
                feature.AssistantEngineerStatus == AssistantEngineerFeatureStatus.OutOfScope &&
                feature.Priority != EnergyCalculationParityPriority.P3)
            .Select(feature => feature.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Out-of-scope features must be P3 only: {string.Join(", ", violations)}.");
    }
}
