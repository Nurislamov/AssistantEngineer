using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class VerificationServiceTests
{
    [Fact]
    public async Task VerifyBuildingAsyncRunsWorkflowAndDeletesTemporaryDirectory()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
        var artifacts = CreateArtifactStore(tempDirectory);
        var building = CreateBuilding();
        var repository = new BuildingRepositoryStub(building);
        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var coolingService = new BuildingCoolingLoadService(
            repository,
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            CalculationTestFactory.CreateIso52016ClimateDataValidator());
        var exporter = new ExporterStub();
        var runner = new RunnerStub(artifacts);
        var parser = new ParserStub();
        var comparator = new ComparatorStub();
        var service = new VerificationService(
            repository,
            coolingService,
            exporter,
            runner,
            parser,
            comparator,
            artifacts);

        var result = await service.VerifyBuildingAsync(
            building.Id,
            CoolingLoadCalculationMethod.Simplified,
            new VerificationRequest
            {
                WeatherArtifactId = "weather.epw",
                AdditionalArguments = ["--readvars"]
            });

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(exporter.Called);
        Assert.True(runner.Called);
        Assert.True(parser.Called);
        Assert.True(comparator.Called);
        Assert.Equal("weather.epw", runner.Request?.WeatherArtifactId);
        Assert.Equal("exported-model.idf", runner.Request?.ModelArtifactId);
        Assert.Contains("--readvars", runner.Request?.AdditionalArguments ?? []);
        Assert.False(Directory.Exists(runner.WorkingDirectory));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task VerifyBuildingAsyncReturnsNotFoundWithoutRunningEnergyPlus()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
        var building = CreateBuilding();
        var artifacts = CreateArtifactStore(tempDirectory);
        var repository = new BuildingRepositoryStub(building);
        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var coolingService = new BuildingCoolingLoadService(
            repository,
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            CalculationTestFactory.CreateIso52016ClimateDataValidator());
        var runner = new RunnerStub(artifacts);
        var service = new VerificationService(
            repository,
            coolingService,
            new ExporterStub(),
            runner,
            new ParserStub(),
            new ComparatorStub(),
            artifacts);

        var result = await service.VerifyBuildingAsync(
            999,
            CoolingLoadCalculationMethod.Simplified,
            new VerificationRequest { WeatherArtifactId = "weather.epw" });

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.False(runner.Called);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static Building CreateBuilding()
    {
        var project = DomainInvariantTests.CreateProject("Verification project");
        var building = Building.Create("Verification building", project).Value;
        Assert.True(project.AddBuilding(building).IsSuccess);
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            isExternal: true,
            ThermalTransmittance.FromValue(1.2).Value,
            CardinalDirection.South).IsSuccess);
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

        public void Remove(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class ExporterStub : IEnergyPlusModelExporter
    {
        public bool Called { get; private set; }

        public async Task<Result<EnergyPlusModelExportResult>> ExportAsync(
            Building building,
            string? runName = null,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            await Task.CompletedTask;
            return Result<EnergyPlusModelExportResult>.Success(new EnergyPlusModelExportResult
            {
                BuildingId = building.Id,
                BuildingName = building.Name,
                ModelArtifactId = "exported-model.idf"
            });
        }
    }

    private sealed class RunnerStub : IEnergyPlusBenchmarkRunner
    {
        private readonly IEnergyPlusArtifactStore _artifacts;

        public RunnerStub(IEnergyPlusArtifactStore artifacts)
        {
            _artifacts = artifacts;
        }

        public bool Called { get; private set; }
        public EnergyPlusBenchmarkRequest? Request { get; private set; }
        public string WorkingDirectory { get; private set; } = string.Empty;

        public Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
            EnergyPlusBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            Request = request;
            var workspace = _artifacts.CreateRunWorkspace(request.RunName);
            Assert.True(workspace.IsSuccess, workspace.Error);
            WorkingDirectory = workspace.Value.WorkingDirectory;
            return Task.FromResult(Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
            {
                Succeeded = true,
                ExitCode = 0,
                RunArtifactId = workspace.Value.RunArtifactId
            }));
        }
    }

    private sealed class ParserStub : IEnergyPlusResultParser
    {
        public bool Called { get; private set; }

        public EnergyPlusCalculationSummary Parse(string outputDirectory)
        {
            Called = true;
            Assert.True(Directory.Exists(outputDirectory));
            return new EnergyPlusCalculationSummary
            {
                HourlyCoolingLoadW = [1000, 1200, 900]
            };
        }
    }

    private sealed class ComparatorStub : IVerificationComparator
    {
        public bool Called { get; private set; }

        public VerificationReport Compare(
            BuildingCalculationResult ourResult,
            EnergyPlusCalculationSummary epResult)
        {
            Called = true;
            return new VerificationReport
            {
                BuildingId = ourResult.BuildingId,
                BuildingName = ourResult.BuildingName,
                CalculationMethod = ourResult.CalculationMethod,
                OurCalculation = ourResult,
                EnergyPlusCalculation = epResult,
                CoolingMetrics = new VerificationMetrics { HasComparableData = true, WithinTolerance = true },
                HeatingMetrics = new VerificationMetrics
                {
                    HasComparableData = false,
                    WithinTolerance = false,
                    Detail = "Stub comparator does not implement heating verification."
                },
                Passed = true
            };
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static LocalEnergyPlusArtifactStore CreateArtifactStore(string rootDirectory) =>
        new(Options.Create(new EnergyPlusBenchmarkOptions { ArtifactRootDirectory = rootDirectory }));
}
