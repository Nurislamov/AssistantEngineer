using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;

namespace AssistantEngineer.Tests.Calculations.RoomLoads;

public class RoomLoadCalculationEngineTests
{
    private readonly RoomLoadCalculationEngine _engine = new();

    [Fact]
    public void Calculate_HeatingTransmissionOnlyReturnsExpectedLoad()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(1000, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(50, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal(1000, result.Value.HeatingBreakdown.TransmissionW, precision: 6);
    }

    [Fact]
    public void Calculate_HeatingTransmissionVentilationInfiltrationReturnsExpectedLoad()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000,
                HeatingVentilationW: 500,
                HeatingInfiltrationW: 250)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(1750, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(87.5, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal("transmission", result.Value.DominantHeatingComponent);
    }

    [Fact]
    public void Calculate_CoolingSolarInternalVentilationReturnsExpectedLoad()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 25,
            VolumeM3: 75,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                CoolingTransmissionW: 300,
                CoolingSolarW: 600,
                CoolingInternalGainsW: 500,
                CoolingVentilationW: 400,
                CoolingInfiltrationW: 200)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(2000, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(80, result.Value.CoolingLoadWPerM2, precision: 6);
        Assert.Equal(600, result.Value.CoolingBreakdown.SolarW, precision: 6);
        Assert.Equal(500, result.Value.CoolingBreakdown.InternalGainsW, precision: 6);
        Assert.Equal(300, result.Value.CoolingBreakdown.TransmissionW, precision: 6);
        Assert.Equal(400, result.Value.CoolingBreakdown.VentilationW, precision: 6);
    }

    [Fact]
    public void Calculate_InvalidAreaAddsDiagnostic()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 0,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000)));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasErrors);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error &&
            diagnostic.Code == "RoomLoad.InvalidArea");
    }

    [Fact]
    public void Calculate_NegativeFixedComponentsDoNotCreateNegativeLoads()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: -100,
                CoolingSolarW: -50)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(0, result.Value.CoolingLoadW, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "RoomLoad.NegativeFixedComponentClamped");
    }

    [Fact]
    public void Calculate_UsesInternalGainEngineAsCoolingComponent()
    {
        var result = _engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            InternalGains: new InternalGainInput(
                RoomId: 1,
                OccupancyPeople: 4,
                SensibleGainPerPersonW: 75,
                OccupancyScheduleFactor: 1.0)));

        Assert.True(result.IsSuccess);
        Assert.Equal(300, result.Value.CoolingBreakdown.InternalGainsW, precision: 6);
        Assert.Equal(300, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(0, result.Value.HeatingBreakdown.UsefulInternalGainOffsetW, precision: 6);
    }
}
