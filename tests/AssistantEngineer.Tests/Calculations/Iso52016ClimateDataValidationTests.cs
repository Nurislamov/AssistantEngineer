using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class Iso52016ClimateDataValidationTests
{
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
            Options.Create(new Iso52016CoolingLoadOptions()),
            new MemoryCache(new MemoryCacheOptions()));

        var first = await validator.ValidateAsync(building, CoolingLoadCalculationMethod.Iso52016);
        var second = await validator.ValidateAsync(building, CoolingLoadCalculationMethod.Iso52016);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, provider.HasClimateDataCallCount);
    }

    private sealed class CountingReferenceDataProvider : ISo52016ReferenceDataProvider
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
