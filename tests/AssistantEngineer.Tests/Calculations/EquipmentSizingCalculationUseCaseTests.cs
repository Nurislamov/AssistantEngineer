using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Models.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations;

public sealed class EquipmentSizingCalculationUseCaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CalculateRoomEquipmentSizingAsync_ReturnsValidation_WhenProviderIsMissing()
    {
        var building = CreateDeterministicBuilding();
        var repository = new BuildingGraphRepositoryStub(building);
        var useCase = CreateUseCase(repository, catalogProvider: null);

        var result = await useCase.CalculateRoomEquipmentSizingAsync(
            roomId: 1,
            systemType: "DX",
            unitType: "Wall");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Equal("Equipment catalog sizing provider is not configured.", result.Error);
    }

    [Fact]
    public async Task CalculateRoomEquipmentSizingAsync_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        var building = CreateDeterministicBuilding();
        var repository = new BuildingGraphRepositoryStub(building);
        var useCase = CreateUseCase(
            repository,
            new FakeCatalogSizingProvider([
                new CoolingEquipmentCatalogSizingCandidate(1, "Acme", "DX", "Wall", "Candidate-2500-2200", 2.5, 2.2)
            ]));

        var result = await useCase.CalculateRoomEquipmentSizingAsync(
            roomId: -999,
            systemType: "DX",
            unitType: "Wall");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.Equal("Room with id -999 not found.", result.Error);
    }

    [Fact]
    public async Task CalculateRoomEquipmentSizingAsync_ReturnsExpectedResult_OnHappyPath()
    {
        var building = CreateDeterministicBuilding();
        var repository = new BuildingGraphRepositoryStub(building);
        var useCase = CreateUseCase(
            repository,
            new FakeCatalogSizingProvider([
                new CoolingEquipmentCatalogSizingCandidate(1, "Acme", "DX", "Wall", "TooSmall-2000", 2.0, 1.9),
                new CoolingEquipmentCatalogSizingCandidate(2, "Acme", "DX", "Wall", "Fit-2500", 2.5, 2.2)
            ]));

        var result = await useCase.CalculateRoomEquipmentSizingAsync(
            roomId: 1,
            systemType: "DX",
            unitType: "Wall",
            CoolingLoadCalculationMethod.Simplified);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(2000, result.Value.RequiredCoolingCapacityW, precision: 2);
        Assert.Equal(1750, result.Value.RequiredHeatingCapacityW, precision: 2);
        Assert.Equal(2200, result.Value.RequiredCoolingCapacityWithReserveW, precision: 2);
        Assert.Equal(1925, result.Value.RequiredHeatingCapacityWithReserveW, precision: 2);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);
        Assert.Contains(result.Value.RecommendedEquipment, item => item.EquipmentId == 2);
        Assert.Contains(result.Value.RejectedEquipment, item => item.EquipmentId == 1);
        Assert.Contains(result.Value.Diagnostics, item => item.Code == "EquipmentSizing.HeatingSafetyFactorApplied");
        Assert.Contains(result.Value.Diagnostics, item => item.Code == "EquipmentSizing.CoolingSafetyFactorApplied");
        Assert.DoesNotContain(result.Value.Diagnostics, item => item.Severity == CalculationDiagnosticSeverity.Error);
    }

    private static EquipmentSizingCalculationUseCase CreateUseCase(
        BuildingGraphRepositoryStub repository,
        ICoolingEquipmentCatalogSizingProvider? catalogProvider)
    {
        var timeProvider = new FixedTimeProvider(FixedNow);
        return new EquipmentSizingCalculationUseCase(
            repository,
            repository,
            new RoomLoadCalculationEngine(timeProvider: timeProvider),
            new EquipmentSizingEngine(timeProvider),
            new CoolingLoadReferenceData(),
            Options.Create(new CoolingLoadCalculationOptions()),
            Options.Create(new En12831HeatingLoadOptions()),
            catalogProvider,
            annualClimateDataProvider: null,
            groundTemperatureService: null,
            solarRadiationService: null,
            Options.Create(new Iso52016EnergyNeedOptions()));
    }

    private static Building CreateDeterministicBuilding()
    {
        var project = DomainInvariantTests.CreateProject("Equipment sizing project");
        SetId(project, 100);

        var climateZone = ClimateZone.Create(
            "Deterministic climate",
            Temperature.FromCelsius(36).Value,
            Temperature.FromCelsius(0).Value).Value;
        SetId(climateZone, 200);

        var building = Building.Create("Pipeline building", project, climateZone).Value;
        SetId(building, 10);
        Assert.True(project.AddBuilding(building).IsSuccess);

        var floor = building.AddFloor("Level 1").Value;
        SetId(floor, 11);

        var room = floor.AddRoom(
            "Deterministic room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 4,
            type: RoomType.Office).Value;
        SetId(room, 1);

        var adjacent = floor.AddRoom(
            "Adjacent unheated room",
            Area.FromSquareMeters(5).Value,
            3,
            Temperature.FromCelsius(-5).Value,
            outdoorTemperatureOverride: null,
            peopleCount: 0,
            type: RoomType.Corridor).Value;
        SetId(adjacent, 2);

        Assert.True(room.SetVentilationParameters(
            VentilationParameters.Create(
                airChangesPerHour: 500.0 / (1.2 * 1005.0 * (60.0 / 3600.0) * 20.0),
                heatRecoveryEfficiency: 0,
                infiltrationAirChangesPerHour: 250.0 / (1.2 * 1005.0 * (60.0 / 3600.0) * 20.0),
                windExposureFactor: 0,
                stackCoefficient: 0,
                windCoefficient: 0).Value).IsSuccess);

        Assert.True(adjacent.SetVentilationParameters(VentilationParameters.Create(0, 0).Value).IsSuccess);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(20).Value,
            ThermalTransmittance.FromValue(0.935).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(5).Value,
            ThermalTransmittance.FromValue(0.01).Value,
            SolarHeatGainCoefficient.FromValue(0.5).Value,
            CardinalDirection.South).IsSuccess);
        Assert.True(room.AddWall(
            Area.FromSquareMeters(1).Value,
            ThermalTransmittance.FromValue(25).Value,
            CardinalDirection.North,
            WallBoundaryType.AdjacentUnconditioned,
            adjacent).IsSuccess);

        return building;
    }

    private static void SetId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed class BuildingGraphRepositoryStub :
        IRoomRepository,
        ICalculationPreferencesRepository
    {
        private readonly Building _building;
        private readonly CalculationPreferences _preferences;

        public BuildingGraphRepositoryStub(Building building)
        {
            _building = building;
            _preferences = CalculationPreferences.Create(1.1, 1.1).Value;
        }

        Task<Room?> IRoomRepository.GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        Task<Room?> IRoomRepository.GetForCalculationAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id));

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Floors.SelectMany(floor => floor.Rooms).ToList());

        Task<IReadOnlyList<Room>> IRoomRepository.ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Id == buildingId
                ? _building.Floors.SelectMany(floor => floor.Rooms).ToList()
                : []);

        Task<IReadOnlyList<Room>> IRoomRepository.ListWithEngineeringInputsByBuildingIdAsync(int buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Room>>(_building.Id == buildingId
                ? _building.Floors.SelectMany(floor => floor.Rooms).ToList()
                : []);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRoom(id) is not null);

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(FindRoom(roomId)?.Windows.ToList() ?? []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(FindRoom(roomId)?.Walls.ToList() ?? []);

        public void Add(Room room) => throw new NotSupportedException();
        public void Remove(Room room) => throw new NotSupportedException();
        public void RemoveWindow(Window window) => throw new NotSupportedException();
        public void RemoveWall(Wall wall) => throw new NotSupportedException();

        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(_preferences);

        private Room? FindRoom(int id) =>
            _building.Floors.SelectMany(floor => floor.Rooms).FirstOrDefault(room => room.Id == id);
    }

    private sealed class FakeCatalogSizingProvider : ICoolingEquipmentCatalogSizingProvider
    {
        private readonly IReadOnlyList<CoolingEquipmentCatalogSizingCandidate> _candidates;

        public FakeCatalogSizingProvider(IReadOnlyList<CoolingEquipmentCatalogSizingCandidate> candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>> ListActiveCoolingCandidatesAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_candidates
                .Where(candidate =>
                    candidate.SystemType == systemType &&
                    candidate.UnitType == unitType)
                .ToList() as IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>);
    }
}
