using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016HourlyHeatBalanceSolarContextIntegrationTests
{
    [Fact]
    public void HeatBalanceCalculator_UsesIso52016WeatherSolarSurfaceComponentsWhenProvided()
    {
        var room = CreateRoomWithSouthWindow();
        var weather = CreateWeather(hourOfYear: 180 * 24 + 12, outdoorTemperatureC: 5);
        var calculator = CreateCalculator();

        var legacyResult = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room),
            weather,
            new Dictionary<int, double> { [room.Id] = 20 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var componentResult = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room),
            weather,
            new Dictionary<int, double> { [room.Id] = 20 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12,
            weatherSolarHour: CreateWeatherSolarHour(
                weather.HourOfYear,
                solarAltitudeDegrees: 45,
                beam: 300,
                diffuse: 100,
                ground: 20));

        var legacyRoomHour = Assert.Single(legacyResult.Rooms).Hour;
        var componentRoomHour = Assert.Single(componentResult.Rooms).Hour;

        Assert.Equal(0.0, legacyRoomHour.SolarGainsW, precision: 6);
        Assert.True(componentRoomHour.SolarGainsW > 0);
        Assert.True(componentResult.Hour.SolarGainsW > legacyResult.Hour.SolarGainsW);
    }

    [Fact]
    public void HeatBalanceCalculator_ClampsIso52016WeatherSolarNightComponentsBeforeSolarGain()
    {
        var room = CreateRoomWithSouthWindow();
        var weather = CreateWeather(hourOfYear: 0, outdoorTemperatureC: 5);
        var calculator = CreateCalculator();

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room),
            weather,
            new Dictionary<int, double> { [room.Id] = 20 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12,
            weatherSolarHour: CreateWeatherSolarHour(
                weather.HourOfYear,
                solarAltitudeDegrees: -5,
                beam: 300,
                diffuse: 100,
                ground: 20));

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.Equal(0.0, roomHour.SolarGainsW, precision: 6);
        Assert.Equal(0.0, result.Hour.SolarGainsW, precision: 6);
    }

    private static Iso52016HourlyHeatBalanceCalculator CreateCalculator() =>
        new(
            new ZeroSolarRadiationService(),
            ventilationCalculator: null,
            windowShadingService: null,
            envelopeReferenceData: new BuildingEnvelopeReferenceData(),
            profileCatalog: new En16798ProfileCatalog(),
            groundHeatTransferService: new FixedGroundHeatTransferService(),
            naturalVentilationAirflowService: null,
            options: new Iso52016EnergyNeedOptions
            {
                DefaultGroundBoundaryTemperatureC = 12,
                LatitudeDegrees = 41.3,
                DefaultSolarUtilizationFactor = 1.0,
                DefaultWindowFrameAreaFraction = 0.0,
                DefaultDirectSolarShadingReductionFactor = 1.0
            },
            profileOptions: new En16798ProfileOptions
            {
                UseStandardProfilesWhenMissingSchedules = false
            },
            hourlyProfiles: new HourlyInternalGainProfileService(
                new HourlyRoomProfileAccessor(
                    new RoomAnnualProfileSetProvider(
                        new AnnualScheduleGenerationService(
                            new UzbekistanHolidayCalendarProvider(),
                            new AnnualProfileTemplateProvider())))),
            windowSolarGains: new WindowSolarGainEngine());

    private static Iso52016ThermalZoneState CreateZoneState(
        Room room) =>
        new(
            FloorAreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OutdoorBoundaryHeatTransferCoefficientWPerK: 0,
            GroundBoundaryHeatTransferCoefficientWPerK: 0,
            VentilationHeatTransferCoefficientWPerK: 0,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26);

    private static Iso52016HourlyWeatherSolarRecord CreateWeatherSolarHour(
        int hourOfYear,
        double solarAltitudeDegrees,
        double beam,
        double diffuse,
        double ground)
    {
        var total = beam + diffuse + ground;

        return new Iso52016HourlyWeatherSolarRecord(
            HourOfYear: hourOfYear,
            Month: 6,
            Day: 29,
            Hour: hourOfYear % 24,
            OutdoorTemperatureC: 5,
            GroundBoundaryTemperatureC: 12,
            SolarAltitudeDegrees: solarAltitudeDegrees,
            SolarAzimuthDegrees: 180,
            DirectNormalIrradianceWm2: 0,
            DiffuseHorizontalIrradianceWm2: 0,
            GlobalHorizontalIrradianceWm2: total,
            SurfaceIrradiance:
            [
                new Iso52016SurfaceWeatherSolarRecord(
                    SurfaceCode: "south",
                    Orientation: SurfaceOrientation.SouthVertical,
                    IncidenceAngleDegrees: 30,
                    BeamIrradianceWm2: beam,
                    DiffuseSkyIrradianceWm2: diffuse,
                    GroundReflectedIrradianceWm2: ground,
                    TotalIrradianceWm2: total)
            ]);
    }

    private static Room CreateRoomWithSouthWindow()
    {
        var climateZone = ClimateZone.Create(
            "Climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;

        var project = Project.Create("Project").Value;
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Floor").Value;
        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 0,
            equipmentLoad: Power.FromWatts(0).Value,
            lightingLoad: Power.FromWatts(0).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
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

    private static AnnualHourlyData CreateWeather(
        int hourOfYear,
        double outdoorTemperatureC)
    {
        var annual = AnnualClimateData.Create(
            ClimateZone.Create(
                "Climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-10).Value).Value,
            2026).Value;

        return AnnualHourlyData.Create(
            annual,
            hourOfYear,
            outdoorTemperatureC,
            directSolarRadiation: 0,
            diffuseSolarRadiation: 0,
            windSpeedMPerS: 2).Value;
    }

    private sealed class ZeroSolarRadiationService : ISolarRadiationService
    {
        public double CalculateVerticalSurfaceRadiation(
            AnnualHourlyData hourlyData,
            CardinalDirection orientation,
            double latitude,
            int dayOfYear,
            int hour) =>
            0.0;
    }

    private sealed class FixedGroundHeatTransferService : IGroundHeatTransferService
    {
        public GroundBoundaryCondition CalculateBoundaryCondition(
            Room room,
            BuildingEnvelopeDefaults envelopeDefaults) =>
            new()
            {
                HeatTransferCoefficientWPerK = 0,
                IndoorTemperatureWeight = 0,
                OutdoorTemperatureWeight = 0,
                GroundTemperatureWeight = 1
            };
    }
}
