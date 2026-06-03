using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016MatrixAssemblyCharacterizationTests
{
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Solve_AssemblySurface_ProducesFiniteNodeStateAndDeterministicRepeatRun()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 8.0,
            gainsW: 150.0,
            initialAirTemperatureC: 21.0,
            initialMassTemperatureC: 21.0,
            hourCount: 24);

        var first = _solver.Solve(request);
        var second = _solver.Solve(request);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);

        Assert.Equal(first.Value.HourCount, second.Value.HourCount);

        for (var i = 0; i < first.Value.Hours.Count; i++)
        {
            var hourA = first.Value.Hours[i];
            var hourB = second.Value.Hours[i];

            Assert.Equal(hourA.NodeStates.Count, hourB.NodeStates.Count);
            Assert.Equal(2, hourA.NodeStates.Count);
            Assert.Contains(hourA.NodeStates, node => node.NodeId.Equals("air", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(hourA.NodeStates, node => node.NodeId.Equals("mass", StringComparison.OrdinalIgnoreCase));

            AssertFinite(hourA.AirTemperatureBeforeHvacC);
            AssertFinite(hourA.AirTemperatureAfterHvacC);
            AssertFinite(hourA.HeatingLoadW);
            AssertFinite(hourA.CoolingLoadW);

            Assert.InRange(Math.Abs(hourA.AirTemperatureAfterHvacC - hourB.AirTemperatureAfterHvacC), 0.0, 1e-9);
            Assert.InRange(Math.Abs(hourA.HeatingLoadW - hourB.HeatingLoadW), 0.0, 1e-9);
            Assert.InRange(Math.Abs(hourA.CoolingLoadW - hourB.CoolingLoadW), 0.0, 1e-9);

            Assert.False(hourA.HeatingLoadW > 0.0 && hourA.CoolingLoadW > 0.0);
        }
    }

    private static void AssertFinite(double value)
    {
        Assert.False(double.IsNaN(value));
        Assert.False(double.IsInfinity(value));
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
            ZoneCode: "characterization-zone",
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
