using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Floors;

public class FloorQueryService
{
    private readonly IFloorRepository _floors;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IAggregateLoadCalculator _calculator;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<FloorQueryService> _logger;

    public FloorQueryService(
        IFloorRepository floors,
        ICalculationPreferencesRepository preferences,
        IAggregateLoadCalculator calculator,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<FloorQueryService>? logger = null)
    {
        _floors = floors;
        _preferences = preferences;
        _calculator = calculator;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<FloorQueryService>.Instance;
    }

    public async Task<Result<List<FloorResponse>>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        var floors = await _floors.ListByBuildingIdAsync(buildingId, cancellationToken);
        _logger.LogDebug("Loaded {FloorCount} floors for building {BuildingId}.", floors.Count, buildingId);
        return Result<List<FloorResponse>>.Success(floors.Select(ApplicationMapper.ToResponse).ToList());
    }

    public async Task<Result<FloorResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var floor = await _floors.GetByIdAsync(id, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Floor {FloorId} was not found.", id);
            return Result<FloorResponse>.NotFound($"Floor with id {id} not found.");
        }

        _logger.LogDebug("Loaded floor {FloorId}.", id);
        return Result<FloorResponse>.Success(ApplicationMapper.ToResponse(floor));
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


