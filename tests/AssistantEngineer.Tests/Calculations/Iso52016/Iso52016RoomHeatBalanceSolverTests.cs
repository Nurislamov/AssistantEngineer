using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomHeatBalanceSolverTests
{
    private readonly Iso52016RoomHeatBalanceSolver _solver = new();

    [Fact]
    public void Solve_ReturnsNoLoadWhenFreeFloatingTemperatureStaysBetweenSetpoints()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: 20,
            gainsW: 0,
            hourCount: 24,
            initialTemperatureC: 22);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);

        Assert.All(
            result.Value.Hours,
            hour =>
            {
                Assert.Equal(0.0, hour.HeatingLoadW, precision: 6);
                Assert.Equal(0.0, hour.CoolingLoadW, precision: 6);
                Assert.InRange(hour.IndoorTemperatureAfterHvacC, 20, 26);
            });

        Assert.Equal(0.0, result.Value.AnnualHeatingEnergyKWh, precision: 6);
        Assert.Equal(0.0, result.Value.AnnualCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void Solve_ProducesHeatingLoadWhenFreeFloatingTemperatureFallsBelowHeatingSetpoint()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: -5,
            gainsW: 0,
            hourCount: 24,
            initialTemperatureC: 20);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 20)));

        Assert.True(result.IsSuccess);

        Assert.Contains(
            result.Value.Hours,
            hour => hour.HeatingLoadW > 0);

        Assert.All(
            result.Value.Hours,
            hour => Assert.Equal(
                0.0,
                hour.CoolingLoadW,
                precision: 6));

        Assert.True(result.Value.AnnualHeatingEnergyKWh > 0);
        Assert.Equal(0.0, result.Value.AnnualCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void Solve_ProducesCoolingLoadWhenFreeFloatingTemperatureExceedsCoolingSetpoint()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: 35,
            gainsW: 2000,
            hourCount: 24,
            initialTemperatureC: 26);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 26)));

        Assert.True(result.IsSuccess);

        Assert.Contains(
            result.Value.Hours,
            hour => hour.CoolingLoadW > 0);

        Assert.All(
            result.Value.Hours,
            hour => Assert.Equal(
                0.0,
                hour.HeatingLoadW,
                precision: 6));

        Assert.True(result.Value.AnnualCoolingEnergyKWh > 0);
        Assert.Equal(0.0, result.Value.AnnualHeatingEnergyKWh, precision: 6);
    }

    [Fact]
    public void Solve_BuildsMonthlySummaries()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: -5,
            gainsW: 0,
            hourCount: 48,
            initialTemperatureC: 20);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 20)));

        Assert.True(result.IsSuccess);

        Assert.Single(result.Value.MonthlySummaries);

        var january = result.Value.MonthlySummaries[0];

        Assert.Equal(1, january.Month);
        Assert.True(january.HeatingEnergyKWh > 0);
        Assert.Equal(0.0, january.CoolingEnergyKWh, precision: 6);
        Assert.True(january.PeakHeatingLoadW > 0);
    }

    [Fact]
    public void Solve_TracksSolarAndInternalGainsInSummary()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: 20,
            gainsW: 1000,
            hourCount: 24,
            initialTemperatureC: 22,
            solarGainsW: 600,
            internalGainsW: 400);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);

        Assert.Equal(600.0 * 24.0 / 1000.0, result.Value.AnnualSolarGainsKWh, precision: 6);
        Assert.Equal(400.0 * 24.0 / 1000.0, result.Value.AnnualInternalGainsKWh, precision: 6);
        Assert.Equal(1000.0 * 24.0 / 1000.0, result.Value.AnnualTotalGainsKWh, precision: 6);
    }

    [Fact]
    public void Solve_RejectsEmptyInputProfile()
    {
        var input = new Iso52016RoomHourlyInputProfile(
            RoomCode: "room-1",
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: 20,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: []);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(input));

        Assert.True(result.IsFailure);
        Assert.Equal("Room hourly input profile must contain hourly records.", result.Error);
    }

    [Fact]
    public void Solve_RejectsInvalidTimeStep()
    {
        var input = CreateInputProfile(
            outdoorTemperatureC: 20,
            gainsW: 0,
            hourCount: 24,
            initialTemperatureC: 22);

        var result = _solver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: input,
                Options: new(
                    InitialIndoorTemperatureC: 22,
                    TimeStepSeconds: 0)));

        Assert.True(result.IsFailure);
        Assert.Equal("Heat balance time step must be greater than zero.", result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile(
        double outdoorTemperatureC,
        double gainsW,
        int hourCount,
        double initialTemperatureC,
        double? solarGainsW = null,
        double? internalGainsW = null)
    {
        var solar =
            solarGainsW ??
            gainsW;

        var internalGain =
            internalGainsW ??
            0.0;

        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                OutdoorTemperatureC: outdoorTemperatureC,
                GroundBoundaryTemperatureC: outdoorTemperatureC,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26,
                TransmissionHeatTransferCoefficientWPerK: 100,
                VentilationHeatTransferCoefficientWPerK: 20,
                TotalHeatTransferCoefficientWPerK: 120,
                ThermalCapacityJPerK: 3_000_000,
                SolarGainsW: solar,
                InternalGainsW: internalGain,
                TotalGainsW: solar + internalGain))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "room-1",
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: 20,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }
}