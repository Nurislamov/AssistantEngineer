using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Floors;

/// <summary>
/// Compatibility floor-load service kept for controlled transition.
/// </summary>
/// <remarks>
/// Deprecation marker (documentation-level only): first-party API controllers and load facade use
/// <c>EnergyCalculationPipelineService</c> as the active production path.
/// Keep this service until DI consumers are fully enumerated and migrated.
/// </remarks>
public class FloorCalculationService
{
    private readonly IFloorRepository _floors;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IAggregateLoadCalculator _calculator;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<FloorCalculationService> _logger;

    public FloorCalculationService(
        IFloorRepository floors,
        ICalculationPreferencesRepository preferences,
        IAggregateLoadCalculator calculator,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<FloorCalculationService>? logger = null)
    {
        _floors = floors;
        _preferences = preferences;
        _calculator = calculator;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<FloorCalculationService>.Instance;
    }

    public async Task<Result<FloorCalculationResult>> CalculateAsync(
        int floorId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating cooling load for floor {FloorId} using {CalculationMethod}.",
            floorId,
            method);

        var floor = await _floors.GetForCalculationAsync(floorId, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Cooling load calculation failed because floor {FloorId} was not found.", floorId);
            return Result<FloorCalculationResult>.NotFound($"Floor with id {floorId} not found.");
        }

        var validation = await _iso52016ClimateDataValidator.ValidateAsync(floor, method, cancellationToken);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Cooling load validation failed for floor {FloorId}: {Error}.",
                floorId,
                validation.Error);
            return Result<FloorCalculationResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(floor.Building.ProjectId, cancellationToken);
        var result = await _calculator.CalculateFloorAsync(floor, method, preferences, cancellationToken);

        _logger.LogInformation(
            "Calculated cooling load for floor {FloorId}: design capacity {DesignCapacityKw} kW.",
            floorId,
            result.DesignCapacityKw);

        return Result<FloorCalculationResult>.Success(result);
    }
}

