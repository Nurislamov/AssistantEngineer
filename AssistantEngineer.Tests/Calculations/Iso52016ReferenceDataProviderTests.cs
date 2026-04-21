using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Services;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Caching.Memory;

namespace AssistantEngineer.Tests;

public class Iso52016ReferenceDataProviderTests
{
    [Fact]
    public async Task GetSolarRadiationAsyncUsesHourlyClimateData()
    {
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var climateData = ClimateData.Create(climateZone, month: 7, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
        for (var hour = 0; hour < 24; hour++)
        {
            Assert.True(climateData.AddHourlyData(hour, dryBulbTemp: 30, directSolar: 100, diffuseSolar: 20).IsSuccess);
        }

        var provider = CreateProvider(new ClimateDataRepositoryStub(climateData));

        var result = await provider.GetSolarRadiationAsync(climateZone, month: 7);

        Assert.Equal(24, result[CardinalDirection.South].Count);
        Assert.Equal(110, result[CardinalDirection.South][0]);
        Assert.Equal(20, result[CardinalDirection.North][0]);
    }

    [Fact]
    public async Task HasClimateDataAsyncRequiresAllTwentyFourHours()
    {
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var incompleteClimateData = ClimateData.Create(climateZone, month: 7, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
        for (var hour = 0; hour < 23; hour++)
        {
            Assert.True(incompleteClimateData.AddHourlyData(hour, dryBulbTemp: 30, directSolar: 100, diffuseSolar: 20).IsSuccess);
        }

        var provider = CreateProvider(new ClimateDataRepositoryStub(incompleteClimateData));

        var result = await provider.HasClimateDataAsync(climateZone, month: 7);

        Assert.False(result);
    }

    [Fact]
    public async Task GetOutdoorTemperatureProfileAsyncUsesHourlyDryBulbTemperatures()
    {
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var climateData = ClimateData.Create(climateZone, month: 7, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
        for (var hour = 0; hour < 24; hour++)
        {
            Assert.True(climateData.AddHourlyData(
                hour,
                dryBulbTemp: 20 + hour,
                directSolar: 100,
                diffuseSolar: 20).IsSuccess);
        }

        var provider = CreateProvider(new ClimateDataRepositoryStub(climateData));

        var result = await provider.GetOutdoorTemperatureProfileAsync(climateZone, month: 7);

        Assert.NotNull(result);
        Assert.Equal(24, result.Count);
        Assert.Equal(20, result[0]);
        Assert.Equal(43, result[23]);
    }

    [Fact]
    public async Task ReusesCachedClimateDataForSameClimateZoneAndMonth()
    {
        var climateZone = ClimateZone.Create(
            "Summer climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var climateData = ClimateData.Create(climateZone, month: 7, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
        for (var hour = 0; hour < 24; hour++)
        {
            Assert.True(climateData.AddHourlyData(hour, dryBulbTemp: 30, directSolar: 100, diffuseSolar: 20).IsSuccess);
        }

        var repository = new ClimateDataRepositoryStub(climateData);
        var provider = CreateProvider(repository);

        Assert.True(await provider.HasClimateDataAsync(climateZone, month: 7));
        var result = await provider.GetSolarRadiationAsync(climateZone, month: 7);

        Assert.Equal(110, result[CardinalDirection.South][0]);
        Assert.Equal(1, repository.CallCount);
    }

    private static Iso52016ReferenceDataProvider CreateProvider(IClimateDataRepository repository) =>
        new(repository, new MemoryCache(new MemoryCacheOptions()));

    private sealed class ClimateDataRepositoryStub : IClimateDataRepository
    {
        private readonly ClimateData _climateData;

        public ClimateDataRepositoryStub(ClimateData climateData) => _climateData = climateData;

        public int CallCount { get; private set; }

        public Task<ClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int month,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<ClimateData?>(
                climateZoneId == _climateData.ClimateZoneId && month == _climateData.Month
                    ? _climateData
                    : null);
        }

        public Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
            int climateZoneId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>(
                climateZoneId == _climateData.ClimateZoneId
                    ? [_climateData.Month]
                    : []);
    }
}


