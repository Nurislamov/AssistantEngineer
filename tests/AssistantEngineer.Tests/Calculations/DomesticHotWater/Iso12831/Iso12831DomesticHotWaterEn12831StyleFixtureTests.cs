using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterEn12831StyleFixtureTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Iso12831DomesticHotWaterDemandCalculator _calculator = new(
        new Iso12831DomesticHotWaterReferenceDataProvider(),
        new Iso12831DomesticHotWaterDrawProfileProvider());

    [Fact]
    public void En12831StyleFixtures_MatchExpectedResultsWithinTolerance()
    {
        var fixtureDirectory = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "dhw", "en12831");
        Assert.True(Directory.Exists(fixtureDirectory), $"Fixture directory was not found: {fixtureDirectory}");

        var fixturePaths = Directory.GetFiles(fixtureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.NotEmpty(fixturePaths);

        foreach (var fixturePath in fixturePaths)
        {
            var fixture = JsonSerializer.Deserialize<Iso12831DomesticHotWaterFixture>(
                File.ReadAllText(fixturePath),
                SerializerOptions);

            Assert.NotNull(fixture);
            var result = _calculator.Calculate(fixture!.Input);
            Assert.True(result.IsSuccess, $"Fixture '{fixture.Id}' failed: {result.Error}");

            AssertWithinTolerance(fixture.Id, "DailyVolumeLiters", fixture.Expected.DailyVolumeLiters, result.Value.DailyVolumeLiters, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "DailyDrawEnergyKWh", fixture.Expected.DailyDrawEnergyKWh, result.Value.DailyDrawEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "DailyTotalEnergyKWh", fixture.Expected.DailyTotalEnergyKWh, result.Value.DailyTotalEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualVolumeLiters", fixture.Expected.AnnualVolumeLiters, result.Value.AnnualVolumeLiters, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualDrawEnergyKWh", fixture.Expected.AnnualDrawEnergyKWh, result.Value.AnnualDrawEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "AnnualTotalEnergyKWh", fixture.Expected.AnnualTotalEnergyKWh, result.Value.AnnualTotalEnergyKWh, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "EquivalentOccupantsUsed", fixture.Expected.EquivalentOccupantsUsed, result.Value.EquivalentOccupantsUsed, fixture.Tolerance);
            AssertWithinTolerance(fixture.Id, "ReferenceDailyVolumeLiters", fixture.Expected.ReferenceDailyVolumeLiters, result.Value.ReferenceDailyVolumeLiters, fixture.Tolerance);
            Assert.Equal(fixture.Expected.HourlyResultsCount, result.Value.HourlyResults.Count);

            Assert.Equal(
                result.Value.AnnualTotalEnergyKWh,
                result.Value.MonthlyResults.Sum(item => item.TotalEnergyKWh),
                3);
        }
    }

    private static void AssertWithinTolerance(
        string fixtureId,
        string metricName,
        double expected,
        double actual,
        Iso12831DomesticHotWaterFixtureTolerance tolerance)
    {
        var absoluteDelta = Math.Abs(expected - actual);
        var relativeDeltaPercent = Math.Abs(expected) > 1e-12
            ? absoluteDelta / Math.Abs(expected) * 100.0
            : absoluteDelta == 0.0 ? 0.0 : double.PositiveInfinity;

        var passes = absoluteDelta <= tolerance.Absolute ||
                     relativeDeltaPercent <= tolerance.RelativePercent;

        Assert.True(
            passes,
            $"Fixture '{fixtureId}' metric '{metricName}' is out of tolerance. Expected={expected}, Actual={actual}, |Delta|={absoluteDelta}, Relative%={relativeDeltaPercent}, AbsTol={tolerance.Absolute}, RelTol%={tolerance.RelativePercent}.");
    }
}
