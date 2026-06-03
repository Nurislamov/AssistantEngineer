using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016AggregationCharacterizationTests
{
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Solve_Aggregation_RemainsConsistentWithHourlySeries()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 22.0,
            gainsW: 500.0,
            initialAirTemperatureC: 22.0,
            initialMassTemperatureC: 22.0,
            hourCount: 48);

        var result = _solver.Solve(request);
        Assert.True(result.IsSuccess, result.Error);

        var profile = result.Value;
        var hours = profile.Hours;

        var hourlyHeating = hours.Sum(hour => hour.HeatingEnergyKWh);
        var hourlyCooling = hours.Sum(hour => hour.CoolingEnergyKWh);
        var hourlyGains = hours.Sum(hour => hour.TotalNodeHeatGainsKWh);

        Assert.InRange(Math.Abs(profile.AnnualHeatingEnergyKWh - hourlyHeating), 0.0, 1e-9);
        Assert.InRange(Math.Abs(profile.AnnualCoolingEnergyKWh - hourlyCooling), 0.0, 1e-9);
        Assert.InRange(Math.Abs(profile.AnnualTotalNodeHeatGainsKWh - hourlyGains), 0.0, 1e-9);

        var monthOne = profile.MonthlySummaries.Single(summary => summary.Month == 1);
        Assert.InRange(Math.Abs(monthOne.HeatingEnergyKWh - hourlyHeating), 0.0, 1e-9);
        Assert.InRange(Math.Abs(monthOne.CoolingEnergyKWh - hourlyCooling), 0.0, 1e-9);
        Assert.InRange(Math.Abs(monthOne.TotalNodeHeatGainsKWh - hourlyGains), 0.0, 1e-9);
    }

    private static Iso52016MatrixHourlySolverRequest CreateTwoNodeRequest(
        double outdoorTemperatureC,
        double gainsW,
        double initialAirTemperatureC,
        double initialMassTemperatureC,
        int hourCount)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016MatrixHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                BoundaryTemperaturesC: new Dictionary<string, double>
                {
                    ["outdoor"] = outdoorTemperatureC
                },
                NodeHeatGainsW: new Dictionary<string, double>
                {
                    ["air"] = gainsW,
                    ["mass"] = 0.0
                },
                HeatingSetpointC: 20.0,
                CoolingSetpointC: 26.0))
            .ToArray();

        return new Iso52016MatrixHourlySolverRequest(
            ZoneCode: "aggregation-zone",
            Nodes:
            [
                new Iso52016MatrixNodeDefinition("air", 1_200_000.0, initialAirTemperatureC, IsAirNode: true),
                new Iso52016MatrixNodeDefinition("mass", 8_000_000.0, initialMassTemperatureC)
            ],
            InternalConductances:
            [
                new Iso52016MatrixConductanceLink("air", "mass", 40.0)
            ],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance("air", "outdoor", 90.0),
                new Iso52016MatrixBoundaryConductance("mass", "outdoor", 15.0)
            ],
            Hours: hours,
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600.0,
                AirNodeId: "air",
                DefaultHeatingSetpointC: 20.0,
                DefaultCoolingSetpointC: 26.0));
    }
}
