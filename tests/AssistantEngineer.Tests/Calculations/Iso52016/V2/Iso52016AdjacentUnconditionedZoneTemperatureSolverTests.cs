using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016AdjacentUnconditionedZoneTemperatureSolverTests
{
    private readonly Iso52016AdjacentUnconditionedZoneTemperatureSolver _solver = new();

    [Fact]
    public void Solve_ReturnsTemperatureBetweenOutdoorGroundAndConditionedZoneWhenNoGains()
    {
        var result = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 0.0,
            AdjacentZonePreviousTemperatureC: 10.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 100.0,
            HeatTransferToGroundWPerK: 50.0,
            GroundTemperatureC: 8.0,
            HeatTransferToConditionedZoneWPerK: 60.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 3_600_000.0));

        Assert.True(result.IsSuccess, result.Error);
        Assert.InRange(result.Value.TemperatureC, 0.0, 20.0);
        Assert.True(result.Value.HeatFlowToConditionedZoneW < 0.0);
    }

    [Fact]
    public void Solve_InternalAndSolarGainsWarmAdjacentZone()
    {
        var withoutGains = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 5.0,
            AdjacentZonePreviousTemperatureC: 8.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 80.0,
            HeatTransferToGroundWPerK: 20.0,
            GroundTemperatureC: 10.0,
            HeatTransferToConditionedZoneWPerK: 40.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 2_000_000.0));

        var withGains = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 5.0,
            AdjacentZonePreviousTemperatureC: 8.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 80.0,
            HeatTransferToGroundWPerK: 20.0,
            GroundTemperatureC: 10.0,
            HeatTransferToConditionedZoneWPerK: 40.0,
            InternalGainsW: 200.0,
            SolarGainsW: 300.0,
            ThermalCapacityJPerK: 2_000_000.0));

        Assert.True(withoutGains.IsSuccess, withoutGains.Error);
        Assert.True(withGains.IsSuccess, withGains.Error);
        Assert.True(withGains.Value.TemperatureC > withoutGains.Value.TemperatureC);
        Assert.Equal(500.0, withGains.Value.TotalGainsW, precision: 6);
    }

    [Fact]
    public void Solve_RejectsNegativeConductance()
    {
        var result = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 0.0,
            AdjacentZonePreviousTemperatureC: 10.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: -1.0,
            HeatTransferToGroundWPerK: 0.0,
            GroundTemperatureC: 8.0,
            HeatTransferToConditionedZoneWPerK: 60.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 3_600_000.0));

        Assert.True(result.IsFailure);
        Assert.Equal("Adjacent unconditioned zone heat transfer coefficients cannot be negative.", result.Error);
    }
}