using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;
using System.Reflection;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016BuildingEnergySimulationApplicationServiceTests
{
    [Fact]
    public async Task SimulateAsync_ReturnsRepositoryBasedBuildingSimulation()
    {
        var climateZone = CreateClimateZone();
        var building = CreateBuildingWithRooms(
            climateZone);

        var annualClimateData = CreateAnnualClimateData(
            climateZone,
            year: 2026,
            outdoorTemperatureC: 10);

        var service = CreateService(
            building,
            annualClimateData,
            defaultWeatherYear: 2020);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: building.Id,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026,
                HeatBalanceOptions: new(
                    InitialIndoorTemperatureC: 22)));

        Assert.True(result.IsSuccess, result.Error);

        Assert.Equal(building.Id, result.Value.BuildingId);
        Assert.Equal(building.Name, result.Value.BuildingName);
        Assert.Equal(climateZone.Id, result.Value.ClimateZoneId);
        Assert.Equal(2026, result.Value.WeatherYear);
        Assert.Equal(8760, result.Value.HourCount);
        Assert.Equal(2, result.Value.RoomCount);
        Assert.True(result.Value.AnnualInternalGainsKWh > 0);
        Assert.True(result.Value.AnnualTotalGainsKWh > 0);
    }

    [Fact]
    public async Task SimulateAsync_UsesDefaultWeatherYearFromOptions()
    {
        var climateZone = CreateClimateZone();
        var building = CreateBuildingWithRooms(
            climateZone);

        var annualClimateData = CreateAnnualClimateData(
            climateZone,
            year: 2030,
            outdoorTemperatureC: 10);

        var service = CreateService(
            building,
            annualClimateData,
            defaultWeatherYear: 2030);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: building.Id,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5)));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(2030, result.Value.WeatherYear);
    }

    [Fact]
    public async Task SimulateAsync_ReturnsNotFoundWhenBuildingDoesNotExist()
    {
        var climateZone = CreateClimateZone();
        var building = CreateBuildingWithRooms(
            climateZone);

        var annualClimateData = CreateAnnualClimateData(
            climateZone,
            year: 2026,
            outdoorTemperatureC: 10);

        var service = CreateService(
            building,
            annualClimateData,
            defaultWeatherYear: 2026);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: 999,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task SimulateAsync_ReturnsValidationWhenClimateZoneIsMissing()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        SetEntityId(building, 101);

        var service = CreateService(
            building,
            annualClimateData: null,
            defaultWeatherYear: 2026);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: building.Id,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026));

        Assert.True(result.IsFailure);
        Assert.Equal("Building climate zone is not assigned.", result.Error);
    }

    [Fact]
    public async Task SimulateAsync_ReturnsValidationWhenAnnualClimateDataIsMissing()
    {
        var climateZone = CreateClimateZone();
        var building = CreateBuildingWithRooms(
            climateZone);

        var service = CreateService(
            building,
            annualClimateData: null,
            defaultWeatherYear: 2026);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: building.Id,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026));

        Assert.True(result.IsFailure);
        Assert.Contains("Annual climate data for climate zone", result.Error);
    }

    [Fact]
    public async Task SimulateAsync_RejectsInvalidRequestBeforeRepositoryAccess()
    {
        var service = CreateService(
            building: null,
            annualClimateData: null,
            defaultWeatherYear: 2026);

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: 0,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026));

        Assert.True(result.IsFailure);
        Assert.Equal("Building id must be greater than zero.", result.Error);
    }

    private static Iso52016BuildingEnergySimulationApplicationService CreateService(
        Building? building,
        AnnualClimateData? annualClimateData,
        int defaultWeatherYear)
    {
        return new Iso52016BuildingEnergySimulationApplicationService(
            new BuildingRepositoryStub(building),
            new AnnualClimateDataProviderStub(annualClimateData),
            new Iso52016BuildingDomainSimulationFacade(
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
                        new Iso52016V2ReducedRoomModelBuilder(),
                new Iso52016V2HourlySolver(),
                new Iso52016V2RoomEnergySimulationResultMapper()))),
            Options.Create(new Iso52016EnergyNeedOptions
            {
                DefaultWeatherYear = defaultWeatherYear
            }));
    }

    private static Building CreateBuildingWithRooms(
        ClimateZone climateZone)
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project, climateZone).Value;
        SetEntityId(building, 101);

        var floor = building.AddFloor("Floor 1").Value;

        var firstRoom = floor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

        var secondRoom = floor.AddRoom(
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

    private static ClimateZone CreateClimateZone()
    {
        var summer = Temperature.FromCelsius(35);
        var winter = Temperature.FromCelsius(-5);

        Assert.True(summer.IsSuccess);
        Assert.True(winter.IsSuccess);

        var climateZone = ClimateZone.Create(
            "Test climate zone",
            summer.Value,
            winter.Value).Value;

        SetEntityId(climateZone, 201);

        return climateZone;
    }

    private static AnnualClimateData CreateAnnualClimateData(
        ClimateZone climateZone,
        int year,
        double outdoorTemperatureC)
    {
        var annualData = AnnualClimateData.Create(
            climateZone,
            year).Value;

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

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building? _building;

        public BuildingRepositoryStub(
            Building? building)
        {
            _building = building;
        }

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(
            int thermalZoneId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null &&
                                       _building.ThermalZones.Any(zone => zone.Id == thermalZoneId)
                ? _building
                : null);

        public Task<Building?> GetForCalculationAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetForReportAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(
                _building is not null && _building.ProjectId == projectId
                    ? [_building]
                    : []);

        public void Add(
            Building building)
        {
        }

        public void Remove(
            Building building)
        {
        }

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);
    }

    private static void SetEntityId(
        object entity,
        int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed class AnnualClimateDataProviderStub : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData? _annualClimateData;

        public AnnualClimateDataProviderStub(
            AnnualClimateData? annualClimateData)
        {
            _annualClimateData = annualClimateData;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default)
        {
            if (_annualClimateData is null)
                return Task.FromResult<AnnualClimateData?>(null);

            return Task.FromResult<AnnualClimateData?>(
                _annualClimateData.ClimateZone.Id == climateZoneId &&
                _annualClimateData.Year == year
                    ? _annualClimateData
                    : null);
        }
    }
}
