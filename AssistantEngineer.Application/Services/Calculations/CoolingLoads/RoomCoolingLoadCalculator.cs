using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Calculations;

public interface IRoomCoolingLoadCalculator
{
    Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

public interface IRoomCoolingLoadCalculationStrategy
{
    CoolingLoadCalculationMethod Method { get; }

    Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

public sealed class CoolingLoadCalculationOptions
{
    public double DefaultCoolingSafetyFactor { get; init; } = 1.10;
    public double SimplifiedVolumeLoadWPerM3 { get; init; } = 35.0;
    public double SimplifiedInternalWallLoadWPerM2 { get; init; } = 30.0;
    public double SimplifiedNorthExternalWallLoadWPerM2 { get; init; } = 30.0;
    public double SimplifiedExternalWallLoadWPerM2 { get; init; } = 60.0;
}

public sealed class RoomCoolingLoadCalculator : IRoomCoolingLoadCalculator
{
    private readonly IReadOnlyDictionary<CoolingLoadCalculationMethod, IRoomCoolingLoadCalculationStrategy> _strategies;
    private readonly ILogger<RoomCoolingLoadCalculator> _logger;

    public RoomCoolingLoadCalculator(
        IEnumerable<IRoomCoolingLoadCalculationStrategy> strategies,
        ILogger<RoomCoolingLoadCalculator>? logger = null)
    {
        _strategies = strategies.ToDictionary(strategy => strategy.Method);
        _logger = logger ?? NullLogger<RoomCoolingLoadCalculator>.Instance;
    }

    public Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default) =>
        CalculateAsync(room, CoolingLoadCalculationMethod.Simplified, preferences, cancellationToken);

    public Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_strategies.TryGetValue(method, out var strategy))
        {
            _logger.LogWarning("Unsupported cooling load calculation method {CalculationMethod} for room {RoomId}.", method, room.Id);
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported calculation method.");
        }

        _logger.LogDebug("Calculating cooling load for room {RoomId} using {CalculationMethod}.", room.Id, method);
        return strategy.CalculateAsync(room, preferences, cancellationToken);
    }
}
