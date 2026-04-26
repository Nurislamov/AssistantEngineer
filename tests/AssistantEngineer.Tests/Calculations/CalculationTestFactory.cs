using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

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
            new SimplifiedCoolingLoadCalculator(Options.Create(options), referenceData),
            new Iso52016CoolingLoadCalculator(Options.Create(new Iso52016CoolingLoadOptions()), new TestIso52016ReferenceDataProvider(), profileAggregator)
        ]);
    }

    public static IAggregateLoadCalculator CreateAggregateCalculator(IRoomCoolingLoadCalculator roomCoolingLoadCalculator) =>
        new AggregateCalculator(roomCoolingLoadCalculator, Options.Create(CreateOptions()), CreateProfileAggregator());

    public static En12831HeatingLoadCalculator CreateHeatingLoadCalculator() =>
        new(Options.Create(new En12831HeatingLoadOptions()));

    public static Iso52016ClimateDataValidator CreateIso52016ClimateDataValidator(bool hasClimateData = true) =>
        new(
            new TestIso52016ReferenceDataProvider(hasClimateData),
            Options.Create(new Iso52016CoolingLoadOptions()),
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
