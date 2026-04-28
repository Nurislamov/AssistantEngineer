using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomSolarGainProfileBuilderTests
{
    private readonly Iso52016RoomSolarGainProfileBuilder _builder =
        new(
            new Iso52016WindowSolarGainCalculator());

    [Fact]
    public void Build_WithNoWindows_ReturnsZeroSolarGains()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: context,
                Windows: []));

        Assert.True(result.IsSuccess);

        Assert.Equal(24, result.Value.HourCount);
        Assert.Equal(0.0, result.Value.AnnualSolarGainsKWh);

        Assert.All(
            result.Value.Hours,
            hour =>
            {
                Assert.Equal(0.0, hour.TotalSolarGainW);
                Assert.Empty(hour.WindowGains);
            });
    }

    [Fact]
    public void Build_AggregatesSolarGainsFromAllWindows()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: context,
                Windows:
                [
                    new(
                        WindowCode: "W1",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 2.0,
                        SolarHeatGainCoefficient: 0.6),

                    new(
                        WindowCode: "W2",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 1.0,
                        SolarHeatGainCoefficient: 0.5)
                ]));

        Assert.True(result.IsSuccess);

        var firstHour = result.Value.GetHour(0);

        Assert.Equal(2, firstHour.WindowGains.Count);

        // Surface irradiance total = 500 W/m².
        // W1: 500 * 2.0 * 0.6 = 600 W.
        // W2: 500 * 1.0 * 0.5 = 250 W.
        Assert.Equal(850.0, firstHour.TotalSolarGainW, precision: 6);

        Assert.Equal(
            850.0 * 24.0 / 1000.0,
            result.Value.AnnualSolarGainsKWh,
            precision: 6);
    }

    [Fact]
    public void Build_AggregatesBeamDiffuseAndGroundComponents()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: context,
                Windows:
                [
                    new(
                        WindowCode: "W1",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 2.0,
                        SolarHeatGainCoefficient: 0.6)
                ]));

        Assert.True(result.IsSuccess);

        var firstHour = result.Value.GetHour(0);

        Assert.Equal(360.0, firstHour.BeamSolarGainW, precision: 6);
        Assert.Equal(180.0, firstHour.DiffuseSkySolarGainW, precision: 6);
        Assert.Equal(60.0, firstHour.GroundReflectedSolarGainW, precision: 6);
        Assert.Equal(600.0, firstHour.TotalSolarGainW, precision: 6);
    }

    [Fact]
    public void Build_RejectsDuplicateWindowCodes()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: context,
                Windows:
                [
                    new(
                        WindowCode: "W1",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 2.0,
                        SolarHeatGainCoefficient: 0.6),

                    new(
                        WindowCode: "w1",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 1.0,
                        SolarHeatGainCoefficient: 0.5)
                ]));

        Assert.True(result.IsFailure);
        Assert.Contains("Window codes must be unique", result.Error);
    }

    [Fact]
    public void Build_RejectsInvalidWindowArea()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: context,
                Windows:
                [
                    new(
                        WindowCode: "W1",
                        Orientation: CardinalDirection.South,
                        WindowAreaM2: 0.0,
                        SolarHeatGainCoefficient: 0.6)
                ]));

        Assert.True(result.IsFailure);
        Assert.Equal("Window 'W1' area must be greater than zero.", result.Error);
    }

    [Fact]
    public void Build_RejectsEmptyRoomCode()
    {
        var context = CreateContext();

        var result = _builder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: " ",
                WeatherSolarContext: context,
                Windows: []));

        Assert.True(result.IsFailure);
        Assert.Equal("Room code is required.", result.Error);
    }

    private static Iso52016WeatherSolarContext CreateContext()
    {
        var hours = Enumerable
            .Range(0, 24)
            .Select(hour => new Iso52016HourlyWeatherSolarRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 10,
                GroundBoundaryTemperatureC: 12,
                SolarAltitudeDegrees: 30,
                SolarAzimuthDegrees: 180,
                DirectNormalIrradianceWm2: 600,
                DiffuseHorizontalIrradianceWm2: 100,
                GlobalHorizontalIrradianceWm2: 400,
                SurfaceIrradiance:
                [
                    new Iso52016SurfaceWeatherSolarRecord(
                        SurfaceCode: WeatherSolarSurfaceCodes.South,
                        Orientation: WeatherSolarSurface.South.Orientation,
                        IncidenceAngleDegrees: 45,
                        BeamIrradianceWm2: 300,
                        DiffuseSkyIrradianceWm2: 150,
                        GroundReflectedIrradianceWm2: 50,
                        TotalIrradianceWm2: 500)
                ]))
            .ToArray();

        return new Iso52016WeatherSolarContext(
            Year: 2026,
            TimeZoneOffset: TimeSpan.Zero,
            LatitudeDegrees: 0,
            LongitudeDegrees: 0,
            Hours: hours);
    }
}