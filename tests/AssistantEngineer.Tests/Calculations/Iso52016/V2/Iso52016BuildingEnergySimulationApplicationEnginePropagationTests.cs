using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016BuildingEnergySimulationApplicationEnginePropagationTests
{
    [Fact]
    public async Task SimulateAsync_PassesRequestedSimulationEngineToDomainFacade()
    {
        var climateZone = CreateClimateZone();
        var building = CreateBuildingWithRoom(climateZone);
        var annualClimateData = CreateAnnualClimateData(climateZone);
        var domainFacade = new BuildingDomainSimulationFacadeSpy();

        var service = new Iso52016BuildingEnergySimulationApplicationService(
            new BuildingRepositoryStub(building),
            new AnnualClimateDataProviderStub(annualClimateData),
            domainFacade,
            Options.Create(new Iso52016EnergyNeedOptions
            {
                DefaultWeatherYear = 2026
            }));

        var result = await service.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: building.Id,
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026,
                SimulationEngine: Iso52016SimulationEngine.V2Matrix));

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(domainFacade.LastRequest);
        Assert.Equal(Iso52016SimulationEngine.V2Matrix, domainFacade.LastRequest!.SimulationEngine);
        Assert.Equal(Iso52016SimulationEngine.V2Matrix, result.Value.SimulationEngine);
    }

    private static Building CreateBuildingWithRoom(
        ClimateZone climateZone)
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project, climateZone).Value;
        SetEntityId(building, 101);

        var floor = building.AddFloor("Floor 1").Value;

        var room = floor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 1,
            equipmentLoad: Power.FromWatts(100).Value,
            lightingLoad: Power.FromWatts(100).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        return building;
    }

    private static ClimateZone CreateClimateZone()
    {
        var climateZone = ClimateZone.Create(
            "Test climate zone",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;

        SetEntityId(climateZone, 201);

        return climateZone;
    }

    private static AnnualClimateData CreateAnnualClimateData(
        ClimateZone climateZone)
    {
        var annualData = AnnualClimateData.Create(
            climateZone,
            year: 2026).Value;

        var addResult = annualData.AddHourlyData(
            hourOfYear: 0,
            dryBulbTemp: 10,
            directSolar: 0,
            diffuseSolar: 0,
            relativeHumidityPercent: 50,
            atmosphericPressurePa: 101_325,
            windSpeedMPerS: 2,
            windDirectionDegrees: 180,
            horizontalInfraredRadiationWPerM2: 300,
            skyTemperatureC: 0,
            totalSkyCoverTenths: 5,
            opaqueSkyCoverTenths: 4);

        Assert.True(addResult.IsSuccess);

        return annualData;
    }

    private static void SetEntityId(
        object entity,
        int id)
    {
        var field = entity.GetType().GetField(
            "<Id>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed class BuildingDomainSimulationFacadeSpy : IIso52016BuildingDomainSimulationFacade
    {
        public Iso52016BuildingDomainSimulationFacadeRequest? LastRequest { get; private set; }

        public Result<Iso52016BuildingDomainSimulationFacadeResult> Simulate(
            Iso52016BuildingDomainSimulationFacadeRequest request)
        {
            LastRequest = request;

            var simulationResult = new Iso52016BuildingSimulationFacadeResult(
                BuildingCode: request.Building.Name,
                WeatherSolarContext: new Iso52016WeatherSolarContext(
                    Year: 2026,
                    TimeZoneOffset: TimeSpan.Zero,
                    LatitudeDegrees: request.LatitudeDegrees,
                    LongitudeDegrees: request.LongitudeDegrees,
                    Hours: []),
                RoomResults: [],
                Hours: [],
                MonthlySummaries: [],
                SimulationEngine: request.SimulationEngine);

            return Result<Iso52016BuildingDomainSimulationFacadeResult>.Success(
                new Iso52016BuildingDomainSimulationFacadeResult(
                    BuildingId: request.Building.Id,
                    BuildingName: request.Building.Name,
                    Rooms: [],
                    SimulationResult: simulationResult));
        }
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