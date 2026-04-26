using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class Iso52016ClimateDataValidator
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(15),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private readonly IIso52016ReferenceDataProvider _referenceDataProvider;
    private readonly Iso52016CoolingLoadOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<Iso52016ClimateDataValidator> _logger;

    public Iso52016ClimateDataValidator(
        IIso52016ReferenceDataProvider referenceDataProvider,
        IOptions<Iso52016CoolingLoadOptions> options,
        IMemoryCache cache,
        ILogger<Iso52016ClimateDataValidator>? logger = null)
    {
        _referenceDataProvider = referenceDataProvider;
        _options = options.Value;
        _cache = cache;
        _logger = logger ?? NullLogger<Iso52016ClimateDataValidator>.Instance;
    }

    public Task<Result> ValidateAsync(
        Room room,
        CoolingLoadCalculationMethod method,
        CancellationToken cancellationToken = default) =>
        ValidateClimateZoneAsync(room.Floor.Building.ClimateZone, method, cancellationToken);

    public Task<Result> ValidateAsync(
        Floor floor,
        CoolingLoadCalculationMethod method,
        CancellationToken cancellationToken = default) =>
        ValidateClimateZoneAsync(floor.Building.ClimateZone, method, cancellationToken);

    public Task<Result> ValidateAsync(
        Building building,
        CoolingLoadCalculationMethod method,
        CancellationToken cancellationToken = default) =>
        ValidateClimateZoneAsync(building.ClimateZone, method, cancellationToken);

    private async Task<Result> ValidateClimateZoneAsync(
        ClimateZone? climateZone,
        CoolingLoadCalculationMethod method,
        CancellationToken cancellationToken)
    {
        if (method != CoolingLoadCalculationMethod.Iso52016)
            return Result.Success();

        if (climateZone is null)
        {
            _logger.LogWarning("ISO 52016 validation failed: building climate zone is missing.");
            return Result.Validation("Building climate zone is required for ISO 52016 cooling load calculation.");
        }

        var cacheKey = new ClimateValidationCacheKey(climateZone.Id, _options.DefaultDesignMonth);
        if (_cache.TryGetValue(cacheKey, out Result? cachedResult))
            return cachedResult!;

        var hasClimateData = await _referenceDataProvider.HasClimateDataAsync(
            climateZone,
            _options.DefaultDesignMonth,
            cancellationToken);
        if (hasClimateData)
        {
            _logger.LogDebug(
                "ISO 52016 validation succeeded for climate zone {ClimateZoneId}, month {Month}.",
                climateZone.Id,
                _options.DefaultDesignMonth);
            var success = Result.Success();
            _cache.Set(cacheKey, success, CacheOptions);
            return success;
        }

        _logger.LogWarning(
            "ISO 52016 validation failed for climate zone {ClimateZoneId}: climate data for month {Month} is incomplete or missing.",
            climateZone.Id,
            _options.DefaultDesignMonth);
        var failure = Result.Validation(
            $"Climate data with 24 hourly records for month {_options.DefaultDesignMonth} is required for ISO 52016 cooling load calculation.");
        _cache.Set(cacheKey, failure, CacheOptions);
        return failure;
    }

    private sealed record ClimateValidationCacheKey(int ClimateZoneId, int Month);
}
