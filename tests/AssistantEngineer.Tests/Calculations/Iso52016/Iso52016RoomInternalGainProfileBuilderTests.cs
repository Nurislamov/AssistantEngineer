using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomInternalGainProfileBuilderTests
{
    private readonly Iso52016RoomInternalGainProfileBuilder _builder = new();

    [Fact]
    public void Build_ReturnsHourlyInternalGainProfile()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 2,
                SensibleHeatGainPerPersonW: 125,
                EquipmentLoadW: 500,
                LightingLoadW: 300,
                OccupancyFactors: ConstantProfile(24, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsSuccess);

        Assert.Equal("room-1", result.Value.RoomCode);
        Assert.Equal(24, result.Value.HourCount);

        var firstHour = result.Value.GetHour(0);

        Assert.Equal(250.0, firstHour.PeopleGainW, precision: 6);
        Assert.Equal(500.0, firstHour.EquipmentGainW, precision: 6);
        Assert.Equal(300.0, firstHour.LightingGainW, precision: 6);
        Assert.Equal(1050.0, firstHour.TotalInternalGainW, precision: 6);

        Assert.Equal(
            1050.0 * 24.0 / 1000.0,
            result.Value.AnnualInternalGainsKWh,
            precision: 6);
    }

    [Fact]
    public void Build_AppliesHourlyFactors()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 3,
                PeopleCount: 2,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 500,
                LightingLoadW: 300,
                OccupancyFactors: [0.0, 0.5, 1.0],
                EquipmentFactors: [0.0, 0.5, 1.0],
                LightingFactors: [0.0, 0.5, 1.0]));

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value.GetHour(0).TotalInternalGainW, precision: 6);
        Assert.Equal(500.0, result.Value.GetHour(1).TotalInternalGainW, precision: 6);
        Assert.Equal(1000.0, result.Value.GetHour(2).TotalInternalGainW, precision: 6);
    }

    [Fact]
    public void Build_AllowsZeroLoads()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 0,
                SensibleHeatGainPerPersonW: 0,
                EquipmentLoadW: 0,
                LightingLoadW: 0,
                OccupancyFactors: ConstantProfile(24, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsSuccess);

        Assert.All(
            result.Value.Hours,
            hour => Assert.Equal(
                0.0,
                hour.TotalInternalGainW,
                precision: 6));
    }

    [Fact]
    public void Build_RejectsEmptyRoomCode()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: " ",
                HourCount: 24,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 100,
                LightingLoadW: 100,
                OccupancyFactors: ConstantProfile(24, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsFailure);
        Assert.Equal("Room code is required.", result.Error);
    }

    [Fact]
    public void Build_RejectsInvalidHourCount()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 0,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 100,
                LightingLoadW: 100,
                OccupancyFactors: [],
                EquipmentFactors: [],
                LightingFactors: []));

        Assert.True(result.IsFailure);
        Assert.Equal("Hour count must be greater than zero.", result.Error);
    }

    [Fact]
    public void Build_RejectsNegativeLoads()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: -1,
                LightingLoadW: 100,
                OccupancyFactors: ConstantProfile(24, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsFailure);
        Assert.Equal("Equipment load must not be negative.", result.Error);
    }

    [Fact]
    public void Build_RejectsProfileWithWrongLength()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 100,
                LightingLoadW: 100,
                OccupancyFactors: ConstantProfile(23, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsFailure);
        Assert.Equal("Occupancy factors must contain exactly 24 values.", result.Error);
    }

    [Fact]
    public void Build_RejectsProfileFactorOutsideZeroOneRange()
    {
        var occupancy = ConstantProfile(24, 1.0).ToArray();
        occupancy[5] = 1.2;

        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 100,
                LightingLoadW: 100,
                OccupancyFactors: occupancy,
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsFailure);
        Assert.Equal("Occupancy factors value at hour 5 must be between 0 and 1.", result.Error);
    }

    [Fact]
    public void GetHour_RejectsOutOfRangeHour()
    {
        var result = _builder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: "room-1",
                HourCount: 24,
                PeopleCount: 1,
                SensibleHeatGainPerPersonW: 100,
                EquipmentLoadW: 100,
                LightingLoadW: 100,
                OccupancyFactors: ConstantProfile(24, 1.0),
                EquipmentFactors: ConstantProfile(24, 1.0),
                LightingFactors: ConstantProfile(24, 1.0)));

        Assert.True(result.IsSuccess);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(-1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(24));
    }

    private static IReadOnlyList<double> ConstantProfile(
        int hourCount,
        double value) =>
        Enumerable
            .Repeat(value, hourCount)
            .ToArray();
}