using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016RoomSimulationFacadeV2EngineTests
{
    private readonly Iso52016RoomSimulationFacade _facade =
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
                new Iso52016RoomHeatBalanceSolver()),
            new Iso52016V2RoomEnergySimulationService(
                new Iso52016RoomSolarGainProfileBuilder(
                    new Iso52016WindowSolarGainCalculator()),
                new Iso52016RoomInternalGainProfileBuilder(),
                new Iso52016RoomHourlyInputProfileBuilder(),
                new Iso52016V2ReducedRoomModelBuilder(),
                new Iso52016V2HourlySolver()),
            new Iso52016V2RoomEnergySimulationResultMapper());

    [Fact]
    public void Simulate_WhenV2MatrixEngineRequested_ReturnsFacadeResultThroughV2Path()
    {
        var room = CreateRoomWithEnvelope(
            peopleCount: 0,
            equipmentLoadW: 0,
            lightingLoadW: 0,
            wallAreaM2: 200,
            wallUValueWPerM2K: 1.5);

        var annualClimateData = CreateAnnualClimateData(
            year: 2026,
            outdoorTemperatureC: -30);

        var result = _facade.Simulate(
            new Iso52016RoomSimulationFacadeRequest(
                Room: room,
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22),
                SimulationEngine: Iso52016SimulationEngine.V2Matrix));

        Assert.True(result.IsSuccess);

        Assert.Equal(Iso52016SimulationEngine.V2Matrix, result.Value.SimulationEngine);
        Assert.Equal(room.Name, result.Value.RoomCode);
        Assert.Equal(8760, result.Value.HourCount);
        Assert.Equal(8760, result.Value.SimulationResult.HourCount);
        Assert.True(result.Value.AnnualHeatingEnergyKWh > 0);
        Assert.True(result.Value.PeakHeatingLoadW > 0);
    }

    [Fact]
    public void Simulate_WhenLegacyEngineRequested_RemainsDefaultPath()
    {
        var room = CreateRoomWithEnvelope();
        var annualClimateData = CreateAnnualClimateData(
            year: 2026,
            outdoorTemperatureC: 10);

        var result = _facade.Simulate(
            new Iso52016RoomSimulationFacadeRequest(
                Room: room,
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);
        Assert.Equal(Iso52016SimulationEngine.Legacy, result.Value.SimulationEngine);
    }

    private static Room CreateRoomWithEnvelope(
        int peopleCount = 2,
        double equipmentLoadW = 500,
        double lightingLoadW = 300,
        double wallAreaM2 = 10,
        double wallUValueWPerM2K = 0.4,
        double windowAreaM2 = 2)
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
            peopleCount: peopleCount,
            equipmentLoad: Power.FromWatts(equipmentLoadW).Value,
            lightingLoad: Power.FromWatts(lightingLoadW).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(wallAreaM2).Value,
            ThermalTransmittance.FromValue(wallUValueWPerM2K).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            Area.FromSquareMeters(windowAreaM2).Value,
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