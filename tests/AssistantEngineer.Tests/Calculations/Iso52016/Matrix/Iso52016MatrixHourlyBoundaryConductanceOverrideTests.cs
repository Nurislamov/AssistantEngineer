using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixHourlyBoundaryConductanceOverrideTests
{
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void Solve_UsesHourlyBoundaryConductanceOverrideInMatrixAndRightHandSide()
    {
        var request = new Iso52016MatrixHourlySolverRequest(
            ZoneCode: "dynamic-boundary-zone",
            Nodes: new[]
            {
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: 3600,
                    InitialTemperatureC: 20,
                    IsAirNode: true)
            },
            InternalConductances: Array.Empty<Iso52016MatrixConductanceLink>(),
            BoundaryConductances: new[]
            {
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: 10)
            },
            Hours: new[]
            {
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: 0,
                    Month: 1,
                    Day: 1,
                    Hour: 0,
                    BoundaryTemperaturesC: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["outdoor"] = 0
                    },
                    NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
                    HeatingSetpointC: -50,
                    CoolingSetpointC: 100,
                    BoundaryConductanceOverrides: new[]
                    {
                        new Iso52016MatrixHourlyBoundaryConductanceOverride(
                            NodeId: "air",
                            BoundaryId: "outdoor",
                            ConductanceWPerK: 0)
                    }),
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: 1,
                    Month: 1,
                    Day: 1,
                    Hour: 1,
                    BoundaryTemperaturesC: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["outdoor"] = 0
                    },
                    NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
                    HeatingSetpointC: -50,
                    CoolingSetpointC: 100,
                    BoundaryConductanceOverrides: new[]
                    {
                        new Iso52016MatrixHourlyBoundaryConductanceOverride(
                            NodeId: "air",
                            BoundaryId: "outdoor",
                            ConductanceWPerK: 10)
                    })
            },
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600,
                AirNodeId: "air",
                DefaultHeatingSetpointC: -50,
                DefaultCoolingSetpointC: 100));

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(20.0, result.Value.Hours[0].AirTemperatureAfterHvacC, precision: 6);
        Assert.Equal(20.0 / 11.0, result.Value.Hours[1].AirTemperatureAfterHvacC, precision: 6);
    }

    [Fact]
    public void Solve_RejectsBoundaryConductanceOverrideForMissingLink()
    {
        var request = CreateMinimalRequest(
            new[]
            {
                new Iso52016MatrixHourlyBoundaryConductanceOverride(
                    NodeId: "air",
                    BoundaryId: "missing-boundary",
                    ConductanceWPerK: 1)
            });

        var result = _solver.Solve(request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 Matrix boundary conductance override 'air|missing-boundary' does not match a declared boundary conductance link at hour 0.",
            result.Error);
    }

    [Fact]
    public void Solve_RejectsNegativeBoundaryConductanceOverride()
    {
        var request = CreateMinimalRequest(
            new[]
            {
                new Iso52016MatrixHourlyBoundaryConductanceOverride(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: -1)
            });

        var result = _solver.Solve(request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 Matrix boundary conductance override 'air|outdoor' must be finite and non-negative at hour 0.",
            result.Error);
    }

    private static Iso52016MatrixHourlySolverRequest CreateMinimalRequest(
        IReadOnlyList<Iso52016MatrixHourlyBoundaryConductanceOverride> overrides) =>
        new(
            ZoneCode: "dynamic-boundary-zone",
            Nodes: new[]
            {
                new Iso52016MatrixNodeDefinition(
                    NodeId: "air",
                    HeatCapacityJPerK: 3600,
                    InitialTemperatureC: 20,
                    IsAirNode: true)
            },
            InternalConductances: Array.Empty<Iso52016MatrixConductanceLink>(),
            BoundaryConductances: new[]
            {
                new Iso52016MatrixBoundaryConductance(
                    NodeId: "air",
                    BoundaryId: "outdoor",
                    ConductanceWPerK: 10)
            },
            Hours: new[]
            {
                new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: 0,
                    Month: 1,
                    Day: 1,
                    Hour: 0,
                    BoundaryTemperaturesC: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["outdoor"] = 0
                    },
                    NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
                    HeatingSetpointC: -50,
                    CoolingSetpointC: 100,
                    BoundaryConductanceOverrides: overrides)
            },
            Options: new Iso52016MatrixHourlySolverOptions(
                TimeStepSeconds: 3600,
                AirNodeId: "air",
                DefaultHeatingSetpointC: -50,
                DefaultCoolingSetpointC: 100));
}
