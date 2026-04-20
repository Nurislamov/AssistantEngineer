using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Application.Services.Calculations.CoolingLoads.Iso52016;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;

namespace AssistantEngineer.Tests;

public class Iso52016ClimateDataValidationTests
{
    [Fact]
    public async Task BuildingCalculationReturnsValidationWhenIso52016ClimateDataIsMissing()
    {
        var project = DomainInvariantTests.CreateProject();
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var building = Building.Create("Building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        _ = floor.AddRoom(
            "Office",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(35).Value).Value;

        var roomCalculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var service = new BuildingCoolingLoadService(
            new BuildingRepositoryStub(building),
            new EmptyPreferencesRepository(),
            CalculationTestFactory.CreateAggregateCalculator(roomCalculator),
            CalculationTestFactory.CreateIso52016ClimateDataValidator(hasClimateData: false));

        var result = await service.CalculateAsync(building.Id, CoolingLoadCalculationMethod.Iso52016);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("Climate data", result.Error);
    }

    [Fact]
    public async Task ValidatorCachesClimateDataValidationForSameClimateZoneAndMonth()
    {
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var project = DomainInvariantTests.CreateProject();
        var building = Building.Create("Building", project, climateZone).Value;
        var provider = new CountingReferenceDataProvider(hasClimateData: true);
        var validator = new Iso52016ClimateDataValidator(
            provider,
            new Iso52016CoolingLoadOptions(),
            new MemoryCache(new MemoryCacheOptions()));

        var first = await validator.ValidateAsync(building, CoolingLoadCalculationMethod.Iso52016);
        var second = await validator.ValidateAsync(building, CoolingLoadCalculationMethod.Iso52016);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, provider.HasClimateDataCallCount);
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building _building;

        public BuildingRepositoryStub(Building building) => _building = building;

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(id == _building.Id ? _building : null);

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Building building) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class CountingReferenceDataProvider : IIso52016ReferenceDataProvider
    {
        private readonly bool _hasClimateData;

        public CountingReferenceDataProvider(bool hasClimateData)
        {
            _hasClimateData = hasClimateData;
        }

        public int HasClimateDataCallCount { get; private set; }

        public Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasClimateDataAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default)
        {
            HasClimateDataCallCount++;
            return Task.FromResult(_hasClimateData);
        }

        public double GetDefaultSolarRadiation(CardinalDirection orientation) => 0;

        public double GetPeopleHeatGain(RoomType roomType) => 0;
    }
}


