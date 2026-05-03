using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016BuildingSimulationFacadeTests
{
    private readonly Iso52016BuildingSimulationFacade _facade =
        new(
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
                new Iso52016MatrixReducedRoomModelBuilder(),
                new Iso52016MatrixHourlySolver(),
                new Iso52016MatrixRoomEnergySimulationResultMapper()));

    [Fact]
    public void Simulate_ReturnsBuildingSimulationForAllRooms()
    {
        var rooms = new[]
        {
            CreateRoomWithEnvelope("Room 1", areaM2: 20, peopleCount: 2),
            CreateRoomWithEnvelope("Room 2", areaM2: 15, peopleCount: 1)
        };

        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms: rooms,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);

        var simulation = result.Value;

        Assert.Equal("building-1", simulation.BuildingCode);
        Assert.Equal(2, simulation.RoomCount);
        Assert.Equal(8760, simulation.HourCount);
        Assert.Equal(8760, simulation.WeatherSolarContext.HourCount);
        Assert.Equal(12, simulation.MonthlySummaries.Count);

        Assert.True(simulation.AnnualSolarGainsKWh >= 0);
        Assert.True(simulation.AnnualInternalGainsKWh > 0);
        Assert.True(simulation.AnnualTotalGainsKWh > 0);
    }

    [Fact]
    public void Simulate_AggregatesRoomAnnualResults()
    {
        var rooms = new[]
        {
            CreateRoomWithEnvelope("Room 1", areaM2: 20, peopleCount: 2),
            CreateRoomWithEnvelope("Room 2", areaM2: 15, peopleCount: 1)
        };

        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms: rooms,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: -5),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 20)));

        Assert.True(result.IsSuccess);

        var roomHeating =
            result.Value.RoomResults.Sum(room => room.AnnualHeatingEnergyKWh);

        var roomCooling =
            result.Value.RoomResults.Sum(room => room.AnnualCoolingEnergyKWh);

        Assert.Equal(
            roomHeating,
            result.Value.AnnualHeatingEnergyKWh,
            precision: 6);

        Assert.Equal(
            roomCooling,
            result.Value.AnnualCoolingEnergyKWh,
            precision: 6);
    }

    [Fact]
    public void Simulate_AggregatesHourlyLoads()
    {
        var rooms = new[]
        {
            CreateRoomWithEnvelope("Room 1", areaM2: 20, peopleCount: 2),
            CreateRoomWithEnvelope("Room 2", areaM2: 15, peopleCount: 1)
        };

        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms: rooms,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: -5),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 20)));

        Assert.True(result.IsSuccess);

        var hour = result.Value.GetHour(0);

        var roomHourHeating = result.Value.RoomResults
            .Sum(room => room.HeatBalanceProfile.GetHour(0).HeatingLoadW);

        Assert.Equal(
            roomHourHeating,
            hour.HeatingLoadW,
            precision: 6);
    }

    [Fact]
    public void Simulate_RejectsEmptyRoomList()
    {
        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms: [],
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
    public void Simulate_RejectsDuplicateRoomNames()
    {
        var rooms = new[]
        {
            CreateRoomWithEnvelope("Room 1", areaM2: 20, peopleCount: 2),
            CreateRoomWithEnvelope("room 1", areaM2: 15, peopleCount: 1)
        };

        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms: rooms,
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Contains("Room names must be unique", result.Error);
    }

    [Fact]
    public void Simulate_RejectsInvalidCoordinates()
    {
        var result = _facade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: "building-1",
                Rooms:
                [
                    CreateRoomWithEnvelope("Room 1", areaM2: 20, peopleCount: 2)
                ],
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 91,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Latitude must be between -90 and 90 degrees.", result.Error);
    }

    private static Room CreateRoomWithEnvelope(
        string name,
        double areaM2,
        int peopleCount)
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = Floor.Create("Floor", building).Value;

        var room = Room.Create(
            name: name,
            area: Area.FromSquareMeters(areaM2).Value,
            heightM: 3,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: peopleCount,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

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

        return room;
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