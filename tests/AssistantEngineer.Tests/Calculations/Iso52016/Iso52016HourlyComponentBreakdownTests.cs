using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016HourlyComponentBreakdownTests
{
    [Fact]
    public void HeatBalanceCalculator_ReturnsRoomHourlyComponentBreakdown()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 10, outdoorTemperatureC: -5);
        var calculator = CreateCalculator();

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            new Iso52016ThermalZoneState(
                FloorAreaM2: room.Area.SquareMeters,
                VolumeM3: room.CalculateVolume(),
                OutdoorBoundaryHeatTransferCoefficientWPerK: 0,
                GroundBoundaryHeatTransferCoefficientWPerK: 0,
                VentilationHeatTransferCoefficientWPerK: 0,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26),
            weather,
            new Dictionary<int, double> { [room.Id] = 0 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.True(roomHour.HeatingLoadW > 0);
        Assert.True(roomHour.TransmissionW > 0);
        Assert.True(roomHour.VentilationW > 0);
        Assert.True(roomHour.GroundW > 0);
        Assert.Equal(0, roomHour.InfiltrationW, precision: 6);
        Assert.Equal(roomHour.TransmissionW, result.Hour.TransmissionW, precision: 6);
        Assert.Equal(roomHour.VentilationW, result.Hour.VentilationW, precision: 6);
        Assert.Equal(roomHour.GroundW, result.Hour.GroundW, precision: 6);
    }

    [Fact]
    public void ResultComposer_AggregatesZoneComponentsToBuildingHourlyResults()
    {
        var building = CreateBuilding();
        var composer = new Iso52016HourlyResultComposer();
        var zones = new[]
        {
            new Iso52016ZoneHourlyEnergyNeed(
                "Zone A",
                HourOfYear: 0,
                Month: 1,
                HeatingLoadW: 100,
                CoolingLoadW: 0,
                OperativeTemperatureC: 20,
                OutdoorTemperatureC: -5,
                InternalGainsW: 10,
                SolarGainsW: 20,
                TransmissionW: 30,
                VentilationW: 40,
                InfiltrationW: 0,
                GroundW: 50),
            new Iso52016ZoneHourlyEnergyNeed(
                "Zone B",
                HourOfYear: 0,
                Month: 1,
                HeatingLoadW: 200,
                CoolingLoadW: 0,
                OperativeTemperatureC: 21,
                OutdoorTemperatureC: -5,
                InternalGainsW: 15,
                SolarGainsW: 25,
                TransmissionW: 35,
                VentilationW: 45,
                InfiltrationW: 0,
                GroundW: 55)
        };

        var annual = composer.ComposeAnnualResult(
            building,
            weatherYear: 2026,
            zoneHourlyResults: zones,
            roomHourlyResults: []);

        var hour = Assert.Single(annual.HourlyResults);

        Assert.Equal(65, hour.TransmissionW, precision: 6);
        Assert.Equal(85, hour.VentilationW, precision: 6);
        Assert.Equal(0, hour.InfiltrationW, precision: 6);
        Assert.Equal(105, hour.GroundW, precision: 6);
        Assert.Equal(45, hour.SolarGainsW, precision: 6);
        Assert.Equal(25, hour.InternalGainsW, precision: 6);
        Assert.Equal(300, hour.HeatingLoadW, precision: 6);
    }

    private static Iso52016HourlyHeatBalanceCalculator CreateCalculator() =>
        new(
            new SolarRadiationService(),
            new VentilationHeatTransferCalculator(new Iso16798ReferenceData(new InternalLoadStandardProvider())),
            null,
            new BuildingEnvelopeReferenceData(),
            new En16798ProfileCatalog(),
            new Iso13370GroundHeatTransferService(Options.Create(new Iso13370GroundHeatTransferOptions())),
            null,
            new Iso52016EnergyNeedOptions
            {
                DefaultGroundBoundaryTemperatureC = 12,
                LatitudeDegrees = 41.3
            },
            new En16798ProfileOptions
            {
                UseStandardProfilesWhenMissingSchedules = false
            },
            new HourlyInternalGainProfileService(
                new HourlyRoomProfileAccessor(
                    new RoomAnnualProfileSetProvider(
                        new AnnualScheduleGenerationService(
                            new UzbekistanHolidayCalendarProvider(),
                            new AnnualProfileTemplateProvider())))));

    private static Room CreateRoomWithEnvelope()
    {
        var building = CreateBuilding();
        var floor = building.AddFloor("Floor").Value;
        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
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

    private static Building CreateBuilding()
    {
        var climateZone = ClimateZone.Create(
            "Climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = Project.Create("Project").Value;
        return Building.Create("Building", project, climateZone).Value;
    }

    private static HourlyClimateData CreateWeather(
        int hourOfYear,
        double outdoorTemperatureC)
    {
        var annual = AnnualClimateData.Create(
            ClimateZone.Create(
                "Climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-10).Value).Value,
            2026).Value;

        return HourlyClimateData.CreateAnnual(
            annual,
            hourOfYear,
            outdoorTemperatureC,
            directSolar: 0,
            diffuseSolar: 0,
            windSpeedMPerS: 2).Value;
    }
}
