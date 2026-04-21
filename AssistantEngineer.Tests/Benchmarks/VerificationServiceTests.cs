using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class VerificationServiceTests
{
    [Fact]
    public async Task VerifyBuildingAsyncRunsWorkflowAndDeletesTemporaryDirectory()
    {
        var building = CreateBuilding();
        var repository = new BuildingRepositoryStub(building);
        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var coolingService = new BuildingCoolingLoadService(
            repository,
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            CalculationTestFactory.CreateIso52016ClimateDataValidator());
        var exporter = new ExporterStub();
        var runner = new RunnerStub();
        var parser = new ParserStub();
        var comparator = new ComparatorStub();
        var service = new VerificationService(
            repository,
            coolingService,
            exporter,
            runner,
            parser,
            comparator);

        var result = await service.VerifyBuildingAsync(
            building.Id,
            CoolingLoadCalculationMethod.Simplified,
            new VerificationRequest
            {
                WeatherFilePath = "weather.epw",
                AdditionalArguments = ["--readvars"]
            });

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(exporter.Called);
        Assert.True(runner.Called);
        Assert.True(parser.Called);
        Assert.True(comparator.Called);
        Assert.Equal("weather.epw", runner.Request?.WeatherFilePath);
        Assert.Contains("--readvars", runner.Request?.AdditionalArguments ?? []);
        Assert.False(Directory.Exists(runner.Request?.OutputDirectory));
    }

    [Fact]
    public async Task VerifyBuildingAsyncReturnsNotFoundWithoutRunningEnergyPlus()
    {
        var building = CreateBuilding();
        var repository = new BuildingRepositoryStub(building);
        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var coolingService = new BuildingCoolingLoadService(
            repository,
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            CalculationTestFactory.CreateIso52016ClimateDataValidator());
        var runner = new RunnerStub();
        var service = new VerificationService(
            repository,
            coolingService,
            new ExporterStub(),
            runner,
            new ParserStub(),
            new ComparatorStub());

        var result = await service.VerifyBuildingAsync(
            999,
            CoolingLoadCalculationMethod.Simplified,
            new VerificationRequest { WeatherFilePath = "weather.epw" });

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.False(runner.Called);
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

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Building building) => throw new NotSupportedException();
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
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            await File.WriteAllTextAsync(outputPath, "idf", cancellationToken);
            return Result<EnergyPlusModelExportResult>.Success(new EnergyPlusModelExportResult
            {
                BuildingId = building.Id,
                BuildingName = building.Name,
                ModelPath = outputPath
            });
        }
    }

    private sealed class RunnerStub : IEnergyPlusBenchmarkRunner
    {
        public bool Called { get; private set; }
        public EnergyPlusBenchmarkRequest? Request { get; private set; }

        public Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
            EnergyPlusBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            Request = request;
            Assert.True(File.Exists(request.ModelPath));
            return Task.FromResult(Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
            {
                Succeeded = true,
                ExitCode = 0,
                OutputDirectory = request.OutputDirectory
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
                CoolingMetrics = new VerificationMetrics { WithinTolerance = true },
                Passed = true
            };
        }
    }
}
