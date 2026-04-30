using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;

namespace AssistantEngineer.Tests.Calculations.InternalGains;

public class InternalGainEngineTests
{
    private readonly InternalGainEngine _engine = new();

    [Fact]
    public void Calculate_ReturnsOccupancySensibleGain()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                OccupancyPeople: 4,
                SensibleGainPerPersonW: 75,
                OccupancyScheduleFactor: 1.0));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(300.0, result.Value.OccupancySensibleGainW, precision: 6);
        Assert.Equal(300.0, result.Value.TotalSensibleGainW, precision: 6);
        Assert.Equal(300.0, result.Value.TotalInternalGainW, precision: 6);
    }

    [Fact]
    public void Calculate_ReturnsLightingGainByArea()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                AreaM2: 20,
                LightingPowerDensityWPerM2: 10,
                LightingScheduleFactor: 0.8));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(160.0, result.Value.LightingGainW, precision: 6);
        Assert.Equal(160.0, result.Value.TotalSensibleGainW, precision: 6);
    }

    [Fact]
    public void Calculate_ReturnsEquipmentGainByArea()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                AreaM2: 30,
                EquipmentPowerDensityWPerM2: 15,
                EquipmentScheduleFactor: 0.5));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(225.0, result.Value.EquipmentGainW, precision: 6);
        Assert.Equal(225.0, result.Value.TotalSensibleGainW, precision: 6);
    }

    [Fact]
    public void Calculate_ReturnsProcessGainWithSchedule()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                ProcessSensibleGainW: 500,
                ProcessScheduleFactor: 0.6));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(300.0, result.Value.ProcessSensibleGainW, precision: 6);
        Assert.Equal(300.0, result.Value.TotalSensibleGainW, precision: 6);
    }

    [Fact]
    public void Calculate_AggregatesRoomInternalGains()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                AreaM2: 30,

                OccupancyPeople: 4,
                SensibleGainPerPersonW: 75,

                LightingLoadW: 200,
                LightingPowerDensityWPerM2: 10,

                EquipmentPowerDensityWPerM2: 15,

                ProcessSensibleGainW: 500,

                OccupancyScheduleFactor: 1.0,
                LightingScheduleFactor: 0.8,
                EquipmentScheduleFactor: 0.5,
                ProcessScheduleFactor: 0.6));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(300.0, result.Value.OccupancySensibleGainW, precision: 6);

        // Lighting: (explicit 200 W + 30 m² × 10 W/m²) × 0.8 = 400 W
        Assert.Equal(400.0, result.Value.LightingGainW, precision: 6);

        // Equipment: 30 m² × 15 W/m² × 0.5 = 225 W
        Assert.Equal(225.0, result.Value.EquipmentGainW, precision: 6);

        // Process: 500 W × 0.6 = 300 W
        Assert.Equal(300.0, result.Value.ProcessSensibleGainW, precision: 6);

        Assert.Equal(1225.0, result.Value.TotalSensibleGainW, precision: 6);
    }

    [Fact]
    public void Calculate_ZeroScheduleGivesZeroGain()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                OccupancyPeople: 4,
                SensibleGainPerPersonW: 75,
                OccupancyScheduleFactor: 0.0));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(0.0, result.Value.OccupancySensibleGainW, precision: 6);
        Assert.Equal(0.0, result.Value.TotalSensibleGainW, precision: 6);
    }

    [Fact]
    public void Calculate_InvalidScheduleFactorReturnsDiagnosticError()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                OccupancyPeople: 4,
                SensibleGainPerPersonW: 75,
                OccupancyScheduleFactor: 1.25));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasErrors);

        Assert.Contains(
            result.Value.Diagnostics,
            diagnostic =>
                diagnostic.Severity == InternalGainDiagnosticSeverity.Error &&
                diagnostic.Code == "InternalGains.InvalidOccupancyScheduleFactor");
    }

    [Fact]
    public void Calculate_NegativePowerDensityReturnsDiagnosticError()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                AreaM2: 20,
                LightingPowerDensityWPerM2: -10));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasErrors);

        Assert.Contains(
            result.Value.Diagnostics,
            diagnostic =>
                diagnostic.Severity == InternalGainDiagnosticSeverity.Error &&
                diagnostic.Code == "InternalGains.InvalidLightingPowerDensity");
    }

    [Fact]
    public void Calculate_AreaBasedLightingRequiresArea()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                LightingPowerDensityWPerM2: 10));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasErrors);

        Assert.Contains(
            result.Value.Diagnostics,
            diagnostic =>
                diagnostic.Severity == InternalGainDiagnosticSeverity.Error &&
                diagnostic.Code == "InternalGains.MissingAreaForLightingPowerDensity");
    }

    [Fact]
    public void Calculate_LatentGainIsSeparatedFromSensibleGain()
    {
        var result = _engine.Calculate(
            new InternalGainInput(
                RoomId: 1,
                OccupancyPeople: 2,
                SensibleGainPerPersonW: 75,
                LatentGainPerPersonW: 55,
                OccupancyScheduleFactor: 1.0));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(150.0, result.Value.OccupancySensibleGainW, precision: 6);
        Assert.Equal(110.0, result.Value.OccupancyLatentGainW, precision: 6);
        Assert.Equal(150.0, result.Value.TotalSensibleGainW, precision: 6);
        Assert.Equal(110.0, result.Value.TotalLatentGainW, precision: 6);
        Assert.Equal(260.0, result.Value.TotalInternalGainW, precision: 6);

        Assert.Contains(
            result.Value.Diagnostics,
            diagnostic =>
                diagnostic.Severity == InternalGainDiagnosticSeverity.Warning &&
                diagnostic.Code == "InternalGains.LatentGainCalculatedButNotUsedByIso52016SensiblePath");
    }
}