using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;

namespace AssistantEngineer.Tests.Calculations;

public class RoomLoadHeatingUsefulGainOffsetTests
{
    [Fact]
    public void ExplicitUsefulHeatingGainOffsetsReduceDesignHeatingLoadAndRemainVisibleInBreakdown()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 301,
            RoomCode: "R-301",
            RoomName: "Useful gain offset room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 900,
                HeatingVentilationW: 300,
                HeatingUsefulSolarGainOffsetW: 150,
                HeatingUsefulInternalGainOffsetW: 50)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(150, result.Value.HeatingBreakdown.UsefulSolarGainOffsetW, precision: 6);
        Assert.Equal(50, result.Value.HeatingBreakdown.UsefulInternalGainOffsetW, precision: 6);
        Assert.Equal(1000, result.Value.HeatingBreakdown.TotalW, precision: 6);
        Assert.Equal(1000, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(50, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal("transmission", result.Value.DominantHeatingComponent);

        Assert.Contains(result.Value.AssumptionsUsed, assumption =>
            assumption.Contains("not automatically deducted", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "RoomLoad.HeatingUsefulGainOffsetExceedsGrossLoss");
    }

    [Fact]
    public void UsefulHeatingGainOffsetsCanClampHeatingLoadToZeroWithWarningWhenTheyExceedGrossLosses()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 302,
            RoomCode: "R-302",
            RoomName: "Excessive useful gain offset room",
            AreaM2: 10,
            VolumeM3: 30,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 100,
                HeatingUsefulSolarGainOffsetW: 80,
                HeatingUsefulInternalGainOffsetW: 70)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(-50, result.Value.HeatingBreakdown.TotalW, precision: 6);
        Assert.Equal(0, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(0, result.Value.HeatingLoadWPerM2, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "RoomLoad.HeatingUsefulGainOffsetExceedsGrossLoss");
    }

    [Fact]
    public void NegativeUsefulHeatingGainOffsetsAreClampedAndReported()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 303,
            RoomCode: "R-303",
            RoomName: "Negative useful gain offset room",
            AreaM2: 10,
            VolumeM3: 30,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 500,
                HeatingUsefulSolarGainOffsetW: -20,
                HeatingUsefulInternalGainOffsetW: -30)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(0, result.Value.HeatingBreakdown.UsefulSolarGainOffsetW, precision: 6);
        Assert.Equal(0, result.Value.HeatingBreakdown.UsefulInternalGainOffsetW, precision: 6);
        Assert.Equal(500, result.Value.HeatingBreakdown.TotalW, precision: 6);
        Assert.Equal(500, result.Value.HeatingLoadW, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "RoomLoad.NegativeFixedComponentClamped");
    }
}
