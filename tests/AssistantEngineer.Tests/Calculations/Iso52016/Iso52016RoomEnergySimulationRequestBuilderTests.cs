using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomEnergySimulationRequestBuilderTests
{
    private readonly Iso52016RoomEnergySimulationRequestBuilder _builder =
        new(
            new Iso52016RoomWindowSolarGainInputMapper(),
            new Iso52016RoomEnvelopeInputCalculator(),
            new Iso52016ScheduleProfileExpander());

    [Fact]
    public void Build_CreatesSimulationRequestFromRoom()
    {
        var room = CreateRoomWithEnvelope();

        var result = _builder.Build(
            new Iso52016RoomEnergySimulationBuildRequest(
                Room: room,
                WeatherSolarContext: CreateWeatherSolarContext(24)));

        Assert.True(result.IsSuccess);

        var request = result.Value;

        Assert.Equal(room.Name, request.RoomCode);
        Assert.Equal(24, request.WeatherSolarContext.HourCount);
        Assert.Single(request.Windows);

        Assert.Equal(room.PeopleCount, request.PeopleCount);
        Assert.Equal(room.EquipmentLoad.Watts, request.EquipmentLoadW);
        Assert.Equal(room.LightingLoad.Watts, request.LightingLoadW);

        Assert.Equal(24, request.OccupancyFactors.Count);
        Assert.Equal(24, request.EquipmentFactors.Count);
        Assert.Equal(24, request.LightingFactors.Count);

        Assert.True(request.TransmissionHeatTransferCoefficientWPerK > 0);
        Assert.True(request.VentilationHeatTransferCoefficientWPerK > 0);
        Assert.True(request.ThermalCapacityJPerK > 0);
    }

    [Fact]
    public void Build_ExpandsRoomSchedules()
    {
        var room = CreateRoomWithEnvelope();

        var schedule = HourlySchedule.Create(
            "Half day",
            Enumerable.Range(0, 24)
                .Select(hour => hour < 12 ? 1.0 : 0.0)
                .ToArray()).Value;

        Assert.True(room.SetOccupancySchedule(schedule).IsSuccess);

        var result = _builder.Build(
            new Iso52016RoomEnergySimulationBuildRequest(
                Room: room,
                WeatherSolarContext: CreateWeatherSolarContext(48)));

        Assert.True(result.IsSuccess);

        Assert.Equal(1.0, result.Value.OccupancyFactors[0]);
        Assert.Equal(0.0, result.Value.OccupancyFactors[12]);
        Assert.Equal(1.0, result.Value.OccupancyFactors[24]);
        Assert.Equal(0.0, result.Value.OccupancyFactors[36]);
    }

    [Fact]
    public void Build_UsesSetpointOverrides()
    {
        var room = CreateRoomWithEnvelope();

        var result = _builder.Build(
            new Iso52016RoomEnergySimulationBuildRequest(
                Room: room,
                WeatherSolarContext: CreateWeatherSolarContext(24),
                HeatingSetpointOverrideC: 21,
                CoolingSetpointOverrideC: 25));

        Assert.True(result.IsSuccess);

        Assert.Equal(21, result.Value.HeatingSetpointC);
        Assert.Equal(25, result.Value.CoolingSetpointC);
    }

    [Fact]
    public void Build_RejectsInvalidSetpointOverrides()
    {
        var room = CreateRoomWithEnvelope();

        var result = _builder.Build(
            new Iso52016RoomEnergySimulationBuildRequest(
                Room: room,
                WeatherSolarContext: CreateWeatherSolarContext(24),
                HeatingSetpointOverrideC: 25,
                CoolingSetpointOverrideC: 24));

        Assert.True(result.IsFailure);
        Assert.Equal("Cooling setpoint override must be greater than heating setpoint override.", result.Error);
    }

    private static Room CreateRoomWithEnvelope()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = Floor.Create("Floor", building).Value;

        var room = Room.Create(
            name: "Room 1",
            area: Area.FromSquareMeters(20).Value,
            heightM: 3,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);

        return room;
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
                Hour: hour % 24,
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