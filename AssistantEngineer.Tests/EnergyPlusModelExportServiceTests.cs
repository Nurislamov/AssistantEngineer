using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Application.Services.Benchmarks;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;

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
            new EnergyPlusModelExportRequest { OutputPath = "model.idf" });

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(exporter.Called);
        Assert.Equal(building.Id, exporter.BuildingId);
        Assert.Equal("model.idf", exporter.OutputPath);
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
            new EnergyPlusModelExportRequest { OutputPath = "model.idf" });

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

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
        public string OutputPath { get; private set; } = string.Empty;

        public Task<Result<EnergyPlusModelExportResult>> ExportAsync(
            Building building,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            BuildingId = building.Id;
            OutputPath = outputPath;
            return Task.FromResult(Result<EnergyPlusModelExportResult>.Success(new EnergyPlusModelExportResult
            {
                BuildingId = building.Id,
                BuildingName = building.Name,
                ModelPath = outputPath
            }));
        }
    }
}
