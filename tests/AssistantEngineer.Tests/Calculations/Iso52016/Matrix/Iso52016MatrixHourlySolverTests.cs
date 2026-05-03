using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixHourlySolverTests
{
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Solve_HoldsAirNodeAtSteadyStateWithoutHvac()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 22.0,
            gainsW: 0.0,
            initialAirTemperatureC: 22.0,
            initialMassTemperatureC: 22.0,
            hourCount: 24);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.All(result.Value.Hours, hour =>
        {
            Assert.Equal(0.0, hour.HeatingLoadW, precision: 6);
            Assert.Equal(0.0, hour.CoolingLoadW, precision: 6);
            Assert.Equal(22.0, hour.AirTemperatureAfterHvacC, precision: 6);
        });
        Assert.Equal(0.0, result.Value.AnnualHeatingEnergyKWh, precision: 6);
        Assert.Equal(0.0, result.Value.AnnualCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void Solve_UsesImplicitNodeMatrixAndProducesHeatingLoad()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: -5.0,
            gainsW: 0.0,
            initialAirTemperatureC: 20.0,
            initialMassTemperatureC: 20.0,
            hourCount: 12);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Hours, hour => hour.HeatingLoadW > 0.0);
        Assert.All(result.Value.Hours, hour => Assert.Equal(0.0, hour.CoolingLoadW, precision: 6));
        Assert.True(result.Value.AnnualHeatingEnergyKWh > 0.0);
    }

    [Fact]
    public void Solve_UsesImplicitNodeMatrixAndProducesCoolingLoad()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 35.0,
            gainsW: 2500.0,
            initialAirTemperatureC: 26.0,
            initialMassTemperatureC: 26.0,
            hourCount: 12);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Hours, hour => hour.CoolingLoadW > 0.0);
        Assert.All(result.Value.Hours, hour => Assert.Equal(0.0, hour.HeatingLoadW, precision: 6));
        Assert.True(result.Value.AnnualCoolingEnergyKWh > 0.0);
    }

    [Fact]
    public void Solve_KeepsMassNodeSeparateFromAirNode()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: -10.0,
            gainsW: 0.0,
            initialAirTemperatureC: 20.0,
            initialMassTemperatureC: 24.0,
            hourCount: 1);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);

        var hour = result.Value.Hours[0];
        var airTemperature = hour.GetNodeTemperatureAfterHvacC("air");
        var massTemperature = hour.GetNodeTemperatureAfterHvacC("mass");

        Assert.NotEqual(airTemperature, massTemperature, precision: 6);
        Assert.True(massTemperature > airTemperature);
    }

    [Fact]
    public void Solve_BuildsMonthlySummariesAndTracksGains()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 22.0,
            gainsW: 500.0,
            initialAirTemperatureC: 22.0,
            initialMassTemperatureC: 22.0,
            hourCount: 48);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Single(result.Value.MonthlySummaries);
        Assert.Equal(24.0, result.Value.AnnualTotalNodeHeatGainsKWh, precision: 6);
    }

    [Fact]
    public void Solve_RejectsMissingBoundaryTemperature()
    {
        var request = new Iso52016MatrixHourlySolverRequest(
            ZoneCode: "zone-1",
            Nodes:
            [
                new Iso52016MatrixNodeDefinition("air", 1_000_000.0, 20.0, IsAirNode: true)
            ],
            InternalConductances: [],
            BoundaryConductances:
            [
                new Iso52016MatrixBoundaryConductance("air", "outdoor", 100.0)
            ],
            Hours:
            [
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: 0,
                    Month: 1,
                    Day: 1,
                    Hour: 0,
                    BoundaryTemperaturesC: new Dictionary<string, double>(),
                    NodeHeatGainsW: new Dictionary<string, double>())
            ]);

        var result = _solver.Solve(request);

        Assert.True(result.IsFailure);
        Assert.Equal("ISO 52016 Matrix boundary temperature 'outdoor' is missing at hour 0.", result.Error);
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
            ZoneCode: "zone-1",
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