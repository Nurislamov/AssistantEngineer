using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class EnergyPlusModelExportServiceTests
{
    [Fact]
    public async Task ExportBuildingModelAsyncLoadsBuildingAndCallsExporter()
    {
        var building = CreateBuilding();
        var exporter = new ExporterStub();
        var service = new EnergyPlusModelExportService(
            new BuildingRepositoryStub(building),
            exporter);

        var result = await service.ExportBuildingModelAsync(
            building.Id,
            new EnergyPlusModelExportRequest { RunName = "model" });

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(exporter.Called);
        Assert.Equal(building.Id, exporter.BuildingId);
        Assert.Equal("model", exporter.RunName);
    }

    [Fact]
    public async Task ExportBuildingModelAsyncReturnsNotFoundWhenBuildingDoesNotExist()
    {
        var building = CreateBuilding();
        var exporter = new ExporterStub();
        var service = new EnergyPlusModelExportService(
            new BuildingRepositoryStub(building),
            exporter);

        var result = await service.ExportBuildingModelAsync(
            999,
            new EnergyPlusModelExportRequest { RunName = "model" });

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.False(exporter.Called);
    }

    private static Building CreateBuilding()
    {
        var project = DomainInvariantTests.CreateProject("EnergyPlus service project");
        var building = Building.Create("EnergyPlus service building", project).Value;
        Assert.True(project.AddBuilding(building).IsSuccess);
        var floor = building.AddFloor("Level 1").Value;
        Assert.True(floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).IsSuccess);
        return building;
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building.ThermalZones.Any(zone => zone.Id == thermalZoneId) ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class ExporterStub : IEnergyPlusModelExporter
    {
        public bool Called { get; private set; }
        public int BuildingId { get; private set; }
        public string? RunName { get; private set; }

        public Task<Result<EnergyPlusModelExportResult>> ExportAsync(
            Building building,
            string? runName = null,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            BuildingId = building.Id;
            RunName = runName;
            return Task.FromResult(Result<EnergyPlusModelExportResult>.Success(new EnergyPlusModelExportResult
            {
                BuildingId = building.Id,
                BuildingName = building.Name,
                ModelArtifactId = "model-artifact.idf"
            }));
        }
    }
}
