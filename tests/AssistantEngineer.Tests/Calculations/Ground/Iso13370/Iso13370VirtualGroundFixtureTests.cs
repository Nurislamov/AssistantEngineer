using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370VirtualGroundFixtureTests
{
    private readonly Iso13370VirtualGroundTemperatureCalculator _calculator = new();

    [Fact]
    public void VirtualGroundFixtures_ProduceStableInternalAnalyticalAnchors()
    {
        var fixtures = Iso13370VirtualGroundFixtureLoader.LoadAll();
        var resultsById = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(fixture.Input);

            Assert.Equal(12, result.MonthlyVirtualGroundTemperatureC.Count);
            Assert.Equal(12, result.MonthlyEquivalentGroundHeatTransferCoefficientWPerK.Count);
            Assert.Equal(12, result.MonthlyBoundaryConditions.Count);
            Assert.True(result.MonthlyVirtualGroundTemperatureC.All(double.IsFinite));
            Assert.True(result.MonthlyEquivalentGroundHeatTransferCoefficientWPerK.All(value => double.IsFinite(value) && value > 0.0));
            Assert.True(result.CharacteristicFloorDimensionM > 0.0);
            Assert.True(result.AnnualEquivalentGroundHeatTransferCoefficientWPerK > 0.0);

            Assert.Equal(8760, result.HourlyVirtualGroundTemperatureC.Count);
            resultsById[fixture.Id] = result.AnnualEquivalentGroundHeatTransferCoefficientWPerK;
        }

        Assert.True(
            resultsById["insulated-slab"] < resultsById["slab-on-ground-basic"],
            "Insulated slab fixture should produce lower annual ground coupling than baseline slab.");
        Assert.True(
            resultsById["high-conductivity-ground"] > resultsById["slab-on-ground-basic"],
            "High-conductivity ground fixture should produce higher annual ground coupling than baseline slab.");
        Assert.True(
            resultsById["thermal-bridge-enabled"] > resultsById["slab-on-ground-basic"],
            "Thermal-bridge-enabled fixture should produce higher annual ground coupling than baseline slab.");
    }
}
