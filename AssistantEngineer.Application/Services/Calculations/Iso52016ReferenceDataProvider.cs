using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Abstractions.Repositories;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Climate;
using Microsoft.Extensions.Caching.Memory;

namespace AssistantEngineer.Application.Services.Calculations;

public sealed class Iso52016ReferenceDataProvider : IIso52016ReferenceDataProvider
{
    private static readonly IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>> DefaultSolarRadiationProfiles =
        GetDefaultSolarRadiationProfiles();
    private static readonly IReadOnlyDictionary<CardinalDirection, double> DefaultSolarRadiationByOrientation =
        Enum.GetValues<CardinalDirection>()
            .ToDictionary(orientation => orientation, GetDefaultSolarRadiationValue);
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(15),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private readonly IClimateDataRepository _climateDataRepository;
    private readonly IMemoryCache _cache;

    public Iso52016ReferenceDataProvider(
        IClimateDataRepository climateDataRepository,
        IMemoryCache cache)
    {
        _climateDataRepository = climateDataRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetClimateDataSnapshotAsync(climateZone, month, cancellationToken);
        return snapshot.SolarRadiationProfiles;
    }

    public async Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetClimateDataSnapshotAsync(climateZone, month, cancellationToken);
        return snapshot.OutdoorTemperatureProfile;
    }

    public async Task<bool> HasClimateDataAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetClimateDataSnapshotAsync(climateZone, month, cancellationToken);
        return snapshot.HasCompleteHourlyData;
    }

    public double GetDefaultSolarRadiation(CardinalDirection orientation) =>
        DefaultSolarRadiationByOrientation.TryGetValue(orientation, out var value)
            ? value
            : 0;

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

    private async Task<ClimateDataSnapshot> GetClimateDataSnapshotAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken)
    {
        var cacheKey = new ClimateDataCacheKey(climateZone.Id, month);
        if (_cache.TryGetValue(cacheKey, out ClimateDataSnapshot? cachedSnapshot))
            return cachedSnapshot!;

        var climateData = await _climateDataRepository.GetForClimateZoneAsync(
            climateZone.Id,
            month,
            cancellationToken);
        var snapshot = CreateSnapshot(climateData);
        _cache.Set(cacheKey, snapshot, CacheOptions);
        return snapshot;
    }

    private static ClimateDataSnapshot CreateSnapshot(ClimateData? climateData)
    {
        if (!HasCompleteHourlyData(climateData))
            return new ClimateDataSnapshot(false, DefaultSolarRadiationProfiles, OutdoorTemperatureProfile: null);

        var completeClimateData = climateData!;
        var orderedHourlyData = completeClimateData.HourlyData
            .Where(h => h.Hour.HasValue)
            .OrderBy(h => h.Hour!.Value)
            .ToArray();
        var result = new Dictionary<CardinalDirection, IReadOnlyList<double>>();
        foreach (CardinalDirection orientation in Enum.GetValues<CardinalDirection>())
        {
            var profile = orderedHourlyData
                .Select(h => GetSolarRadiationForOrientation(orientation, h))
                .ToArray();
            result[orientation] = profile;
        }

        return new ClimateDataSnapshot(
            true,
            result,
            orderedHourlyData.Select(h => h.DryBulbTemperature).ToArray());
    }

    private static double GetDefaultSolarRadiationValue(CardinalDirection orientation) =>
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

    private static double GetSolarRadiationForOrientation(CardinalDirection orientation, HourlyClimateData hourlyData) =>
        orientation switch
        {
            CardinalDirection.South => hourlyData.DirectSolarRadiation * 0.9 + hourlyData.DiffuseSolarRadiation,
            CardinalDirection.East => hourlyData.DirectSolarRadiation * 0.7 + hourlyData.DiffuseSolarRadiation,
            CardinalDirection.West => hourlyData.DirectSolarRadiation * 0.7 + hourlyData.DiffuseSolarRadiation,
            CardinalDirection.North => hourlyData.DiffuseSolarRadiation,
            _ => hourlyData.DirectSolarRadiation * 0.5 + hourlyData.DiffuseSolarRadiation
        };

    private static IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>> GetDefaultSolarRadiationProfiles()
    {
        var result = new Dictionary<CardinalDirection, IReadOnlyList<double>>();
        var daylightProfile = new[]
        {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.05, 0.2, 0.45, 0.7, 0.88, 1.0, 0.95,
            0.9, 0.82, 0.65, 0.42, 0.18, 0.04, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
        };

        foreach (CardinalDirection orientation in Enum.GetValues<CardinalDirection>())
        {
            var baseValue = GetDefaultSolarRadiationValue(orientation);
            result[orientation] = daylightProfile.Select(factor => factor * baseValue).ToArray();
        }

        return result;
    }

    private static bool HasCompleteHourlyData(ClimateData? climateData) =>
        climateData is not null &&
        climateData.HourlyData
            .Select(h => h.Hour)
            .Where(h => h.HasValue)
            .Select(h => h!.Value)
            .Distinct()
            .OrderBy(h => h)
            .SequenceEqual(Enumerable.Range(0, 24));

    private sealed record ClimateDataCacheKey(int ClimateZoneId, int Month);

    private sealed record ClimateDataSnapshot(
        bool HasCompleteHourlyData,
        IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>> SolarRadiationProfiles,
        IReadOnlyList<double>? OutdoorTemperatureProfile);
}
