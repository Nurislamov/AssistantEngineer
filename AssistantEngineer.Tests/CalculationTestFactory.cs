using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Application.Services.Calculations.CoolingLoads.Iso52016;
using AssistantEngineer.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AssistantEngineer.Tests;

internal static class CalculationTestFactory
{
    public static CoolingLoadCalculationOptions CreateOptions() => new();

    public static IHourlyProfileAggregator CreateProfileAggregator() => new HourlyProfileAggregator();

    public static IRoomCoolingLoadCalculator CreateRoomCoolingLoadCalculator()
    {
        var options = CreateOptions();
        var referenceData = new CoolingLoadReferenceData();
        var profileAggregator = CreateProfileAggregator();

        return new RoomCoolingLoadCalculator(
        [
            new SimplifiedCoolingLoadCalculator(options, referenceData),
            new Iso52016CoolingLoadCalculator(new Iso52016CoolingLoadOptions(), new TestIso52016ReferenceDataProvider(), profileAggregator)
        ]);
    }

    public static IAggregateLoadCalculator CreateAggregateCalculator(IRoomCoolingLoadCalculator roomCoolingLoadCalculator) =>
        new AggregateCalculator(roomCoolingLoadCalculator, CreateOptions(), CreateProfileAggregator());

    public static En12831HeatingLoadCalculator CreateHeatingLoadCalculator() =>
        new(new En12831HeatingLoadOptions());

    public static Iso52016ClimateDataValidator CreateIso52016ClimateDataValidator(bool hasClimateData = true) =>
        new(
            new TestIso52016ReferenceDataProvider(hasClimateData),
            new Iso52016CoolingLoadOptions(),
            new MemoryCache(new MemoryCacheOptions()));

    private sealed class TestIso52016ReferenceDataProvider : IIso52016ReferenceDataProvider
    {
        private readonly bool _hasClimateData;

        public TestIso52016ReferenceDataProvider(bool hasClimateData = true)
        {
            _hasClimateData = hasClimateData;
        }

        public Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>>(
                new Dictionary<CardinalDirection, IReadOnlyList<double>>());

        public Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<double>?>(null);

        public Task<bool> HasClimateDataAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_hasClimateData);

        public double GetDefaultSolarRadiation(CardinalDirection orientation) =>
            orientation switch
            {
                CardinalDirection.North => 0,
                CardinalDirection.NorthEast => 190,
                CardinalDirection.East => 250,
                CardinalDirection.SouthEast => 240,
                CardinalDirection.South => 240,
                CardinalDirection.SouthWest => 350,
                CardinalDirection.West => 470,
                CardinalDirection.NorthWest => 370,
                _ => 0
            };

        public double GetPeopleHeatGain(RoomType roomType) =>
            roomType switch
            {
                RoomType.Residential => 80,
                RoomType.Corridor => 80,
                RoomType.MeetingRoom => 125,
                RoomType.Office => 125,
                RoomType.Retail => 170,
                RoomType.ServerRoom => 125,
                _ => 125
            };
    }
}


