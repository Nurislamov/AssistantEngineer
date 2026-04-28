using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomSimulationFacadeTests
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
                new Iso52016RoomHeatBalanceSolver()));

    [Fact]
    public void Simulate_ReturnsEndToEndRoomSimulation()
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

        var simulation = result.Value;

        Assert.Equal(room.Name, simulation.RoomCode);
        Assert.Equal(8760, simulation.HourCount);
        Assert.Equal(8760, simulation.WeatherSolarContext.HourCount);
        Assert.Equal(8760, simulation.SimulationResult.HourCount);

        Assert.True(simulation.AnnualSolarGainsKWh >= 0);
        Assert.True(simulation.AnnualInternalGainsKWh > 0);
        Assert.True(simulation.AnnualTotalGainsKWh > 0);

        Assert.Equal(
            simulation.SimulationResult.AnnualHeatingEnergyKWh,
            simulation.AnnualHeatingEnergyKWh);

        Assert.Equal(
            simulation.SimulationResult.AnnualCoolingEnergyKWh,
            simulation.AnnualCoolingEnergyKWh);
    }

    [Fact]
    public void Simulate_ExpandsRoomSchedulesEndToEnd()
    {
        var room = CreateRoomWithEnvelope();

        var schedule = HourlySchedule.Create(
            "Working hours",
            Enumerable.Range(0, 24)
                .Select(hour => hour is >= 8 and <= 17 ? 1.0 : 0.0)
                .ToArray()).Value;

        Assert.True(room.SetOccupancySchedule(schedule).IsSuccess);

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

        Assert.Equal(
            0.0,
            result.Value.SimulationRequest.OccupancyFactors[0]);

        Assert.Equal(
            1.0,
            result.Value.SimulationRequest.OccupancyFactors[8]);

        Assert.Equal(
            1.0,
            result.Value.SimulationRequest.OccupancyFactors[17]);

        Assert.Equal(
            0.0,
            result.Value.SimulationRequest.OccupancyFactors[18]);
    }

    [Fact]
    public void Simulate_UsesSetpointOverrides()
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
                HeatingSetpointOverrideC: 21,
                CoolingSetpointOverrideC: 25,
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess);

        Assert.Equal(21, result.Value.SimulationRequest.HeatingSetpointC);
        Assert.Equal(25, result.Value.SimulationRequest.CoolingSetpointC);

        Assert.Equal(21, result.Value.SimulationResult.HeatBalanceProfile.HeatingSetpointC);
        Assert.Equal(25, result.Value.SimulationResult.HeatBalanceProfile.CoolingSetpointC);
    }

    [Theory]
    [InlineData(-91.0, 69.2, "Latitude must be between -90 and 90 degrees.")]
    [InlineData(91.0, 69.2, "Latitude must be between -90 and 90 degrees.")]
    [InlineData(41.3, -181.0, "Longitude must be between -180 and 180 degrees.")]
    [InlineData(41.3, 181.0, "Longitude must be between -180 and 180 degrees.")]
    public void Simulate_RejectsInvalidCoordinates(
        double latitude,
        double longitude,
        string expectedError)
    {
        var result = _facade.Simulate(
            new Iso52016RoomSimulationFacadeRequest(
                Room: CreateRoomWithEnvelope(),
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: latitude,
                LongitudeDegrees: longitude,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void Simulate_RejectsInvalidSetpointOverrides()
    {
        var result = _facade.Simulate(
            new Iso52016RoomSimulationFacadeRequest(
                Room: CreateRoomWithEnvelope(),
                AnnualClimateData: CreateAnnualClimateData(
                    year: 2026,
                    outdoorTemperatureC: 10),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                HeatingSetpointOverrideC: 25,
                CoolingSetpointOverrideC: 24));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Cooling setpoint override must be greater than heating setpoint override.",
            result.Error);
    }

    [Fact]
    public void Simulate_PropagatesAnnualClimateDataValidationFailure()
    {
        var climateZone = CreateClimateZone();

        var annualClimateDataResult = AnnualClimateData.Create(
            climateZone,
            year: 2026);

        Assert.True(annualClimateDataResult.IsSuccess);

        var incompleteAnnualClimateData = annualClimateDataResult.Value;

        var result = _facade.Simulate(
            new Iso52016RoomSimulationFacadeRequest(
                Room: CreateRoomWithEnvelope(),
                AnnualClimateData: incompleteAnnualClimateData,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsFailure);
        Assert.Equal("Annual climate data must contain hourly records.", result.Error);
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