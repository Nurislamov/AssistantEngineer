using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;

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
