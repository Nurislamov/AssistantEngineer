using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016BuildingDomainSimulationFacadeTests
{
    private readonly Iso52016BuildingDomainSimulationFacade _facade =
        new(
            new Iso52016BuildingRoomCollector(),
            new Iso52016BuildingSimulationFacade(
                new Iso52016WeatherSolarContextBuilder(
                    new AnnualClimateDataNormalizer(),
                    new AnnualWeatherSolarProfileBuilder(
                        new SolarPositionCalculator(),
                        new IsotropicSkySurfaceIrradianceCalculator()),
                    new PeriodicIso52016GroundBoundaryTemperatureProvider()),
                new Iso52016RoomEnergySimulationRequestBuilder(
                    new Iso52016RoomWindowSolarGainInputMapper(),
                    new Iso52016RoomEnvelopeInputCalculator(),
                    new Iso52016ScheduleProfileExpander()),
                new Iso52016RoomEnergySimulationService(
                    new Iso52016RoomSolarGainProfileBuilder(
                        new Iso52016WindowSolarGainCalculator()),
                    new Iso52016RoomInternalGainProfileBuilder(),
                    new Iso52016RoomHourlyInputProfileBuilder(),
                    new Iso52016RoomHeatBalanceSolver())));

    [Fact]
    public void Simulate_ReturnsBuildingDomainSimulation()
    {
        var building = CreateBuildingWithRooms();

        var result = _facade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: building,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);

        Assert.Equal(building.Id, result.Value.BuildingId);
        Assert.Equal(building.Name, result.Value.BuildingName);
        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(8760, result.Value.HourCount);
        Assert.Equal(2, result.Value.SimulationResult.RoomCount);

        Assert.True(result.Value.AnnualInternalGainsKWh > 0);
        Assert.True(result.Value.AnnualTotalGainsKWh > 0);
    }

    [Fact]
    public void Simulate_PropagatesCollectorFailureForEmptyBuilding()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        Assert.True(building.AddFloor("Floor 1").IsSuccess);

        var result = _facade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: building,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Building must contain at least one room.", result.Error);
    }

    [Fact]
    public void Simulate_RejectsInvalidCoordinates()
    {
        var result = _facade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: CreateBuildingWithRooms(),
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 91,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Latitude must be between -90 and 90 degrees.", result.Error);
    }

    [Fact]
    public void Simulate_PropagatesAnnualClimateDataValidationFailure()
    {
        var climateZone = CreateClimateZone();
        var annualData = AnnualClimateData.Create(
            climateZone,
            year: 2026).Value;

        var result = _facade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: CreateBuildingWithRooms(),
                AnnualClimateData: annualData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Annual climate data must contain hourly records.", result.Error);
    }

    private static Building CreateBuildingWithRooms()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;

        var firstFloor = building.AddFloor("Floor 1").Value;
        var secondFloor = building.AddFloor("Floor 2").Value;

        var firstRoom = firstFloor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

        var secondRoom = secondFloor.AddRoom(
            "Room 2",
            Area.FromSquareMeters(15).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 1,
            equipmentLoad: Power.FromWatts(400).Value,
            lightingLoad: Power.FromWatts(200).Value,
            type: RoomType.Office).Value;

        AddEnvelope(firstRoom, areaM2: 20);
        AddEnvelope(secondRoom, areaM2: 15);

        return building;
    }

    private static void AddEnvelope(
        Room room,
        double areaM2)
    {
        Assert.True(room.AddWall(
            Area.FromSquareMeters(areaM2 * 0.8).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(Math.Max(1.0, areaM2 * 0.1)).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);
    }

    private static AnnualClimateData CreateAnnualClimateData(
        int year,
        double outdoorTemperatureC)
    {
        var climateZone = CreateClimateZone();

        var annualDataResult = AnnualClimateData.Create(
            climateZone,
            year);

        Assert.True(annualDataResult.IsSuccess);

        var annualData = annualDataResult.Value;

        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var isDay = hourOfDay is >= 7 and <= 17;

            var addResult = annualData.AddHourlyData(
                hourOfYear: hour,
                dryBulbTemp: outdoorTemperatureC,
                directSolar: isDay ? 600 : 0,
                diffuseSolar: isDay ? 100 : 0,
                relativeHumidityPercent: 50,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180,
                horizontalInfraredRadiationWPerM2: 300,
                skyTemperatureC: 0,
                totalSkyCoverTenths: 5,
                opaqueSkyCoverTenths: 4);

            Assert.True(addResult.IsSuccess);
        }

        return annualData;
    }

    private static ClimateZone CreateClimateZone()
    {
        var summer = Temperature.FromCelsius(35);
        var winter = Temperature.FromCelsius(-5);

        Assert.True(summer.IsSuccess);
        Assert.True(winter.IsSuccess);

        var climateZone = ClimateZone.Create(
            "Test climate zone",
            summer.Value,
            winter.Value);

        Assert.True(climateZone.IsSuccess);

        return climateZone.Value;
    }
}