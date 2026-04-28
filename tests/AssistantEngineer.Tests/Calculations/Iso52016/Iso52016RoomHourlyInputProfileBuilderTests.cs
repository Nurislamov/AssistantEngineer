using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomHourlyInputProfileBuilderTests
{
    private readonly Iso52016RoomHourlyInputProfileBuilder _builder = new();

    [Fact]
    public void Build_CombinesWeatherSolarAndInternalGainsIntoHourlyInputProfile()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsSuccess);

        var profile = result.Value;

        Assert.Equal("room-1", profile.RoomCode);
        Assert.Equal(24, profile.HourCount);
        Assert.Equal(120, profile.TransmissionHeatTransferCoefficientWPerK);
        Assert.Equal(30, profile.VentilationHeatTransferCoefficientWPerK);
        Assert.Equal(150, profile.TotalHeatTransferCoefficientWPerK);
        Assert.Equal(3_000_000, profile.ThermalCapacityJPerK);
        Assert.Equal(20, profile.HeatingSetpointC);
        Assert.Equal(26, profile.CoolingSetpointC);

        var firstHour = profile.GetHour(0);

        Assert.Equal(0, firstHour.HourOfYear);
        Assert.Equal(10, firstHour.OutdoorTemperatureC);
        Assert.Equal(12, firstHour.GroundBoundaryTemperatureC);
        Assert.Equal(600, firstHour.SolarGainsW);
        Assert.Equal(400, firstHour.InternalGainsW);
        Assert.Equal(1000, firstHour.TotalGainsW);
        Assert.Equal(150, firstHour.TotalHeatTransferCoefficientWPerK);
    }

    [Fact]
    public void Build_CalculatesAnnualGainSummaries()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsSuccess);

        Assert.Equal(600.0 * 24.0 / 1000.0, result.Value.AnnualSolarGainsKWh, precision: 6);
        Assert.Equal(400.0 * 24.0 / 1000.0, result.Value.AnnualInternalGainsKWh, precision: 6);
        Assert.Equal(1000.0 * 24.0 / 1000.0, result.Value.AnnualTotalGainsKWh, precision: 6);
    }

    [Fact]
    public void Build_RejectsMismatchedSolarProfileRoomCode()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("other-room", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsFailure);
        Assert.Equal("Room solar gain profile room code must match request room code.", result.Error);
    }

    [Fact]
    public void Build_RejectsMismatchedInternalProfileRoomCode()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("other-room", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsFailure);
        Assert.Equal("Room internal gain profile room code must match request room code.", result.Error);
    }

    [Fact]
    public void Build_RejectsMismatchedHourCount()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 23, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Room solar gain profile hour count must match ISO 52016 weather-solar context hour count.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsInvalidHeatTransferCoefficients()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 0,
                VentilationHeatTransferCoefficientWPerK: 0,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsFailure);
        Assert.Equal("At least one heat transfer coefficient must be greater than zero.", result.Error);
    }

    [Fact]
    public void Build_RejectsCoolingSetpointLowerThanHeatingSetpoint()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 24,
                CoolingSetpointC: 22));

        Assert.True(result.IsFailure);
        Assert.Equal("Cooling setpoint must be greater than heating setpoint.", result.Error);
    }

    [Fact]
    public void GetHour_RejectsOutOfRangeHour()
    {
        var result = _builder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: "room-1",
                WeatherSolarContext: CreateWeatherSolarContext(24),
                SolarGainProfile: CreateSolarGainProfile("room-1", 24, 600),
                InternalGainProfile: CreateInternalGainProfile("room-1", 24, 400),
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26));

        Assert.True(result.IsSuccess);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(-1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.Value.GetHour(24));
    }

    private static Iso52016WeatherSolarContext CreateWeatherSolarContext(
        int hourCount)
    {
        var hours = Enumerable
            .Range(0, hourCount)
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

    private static Iso52016RoomSolarGainProfile CreateSolarGainProfile(
        string roomCode,
        int hourCount,
        double solarGainW)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016HourlyRoomSolarGainRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                BeamSolarGainW: solarGainW * 0.6,
                DiffuseSkySolarGainW: solarGainW * 0.3,
                GroundReflectedSolarGainW: solarGainW * 0.1,
                TotalSolarGainW: solarGainW,
                WindowGains: []))
            .ToArray();

        return new Iso52016RoomSolarGainProfile(
            RoomCode: roomCode,
            Windows: [],
            Hours: hours);
    }

    private static Iso52016RoomInternalGainProfile CreateInternalGainProfile(
        string roomCode,
        int hourCount,
        double internalGainW)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016HourlyRoomInternalGainRecord(
                HourOfYear: hour,
                OccupancyFactor: 1,
                EquipmentFactor: 1,
                LightingFactor: 1,
                PeopleGainW: internalGainW * 0.25,
                EquipmentGainW: internalGainW * 0.5,
                LightingGainW: internalGainW * 0.25,
                TotalInternalGainW: internalGainW))
            .ToArray();

        return new Iso52016RoomInternalGainProfile(
            RoomCode: roomCode,
            PeopleCount: 1,
            SensibleHeatGainPerPersonW: 100,
            EquipmentLoadW: 200,
            LightingLoadW: 100,
            Hours: hours);
    }
}