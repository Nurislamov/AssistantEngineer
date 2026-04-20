using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Buildings;

public class BuildingCoolingLoadService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IAggregateLoadCalculator _calculator;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly ILogger<BuildingCoolingLoadService> _logger;

    public BuildingCoolingLoadService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        IAggregateLoadCalculator calculator,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        ILogger<BuildingCoolingLoadService>? logger = null)
    {
        _buildings = buildings;
        _preferences = preferences;
        _calculator = calculator;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _logger = logger ?? NullLogger<BuildingCoolingLoadService>.Instance;
    }

    public async Task<Result<BuildingCalculationResult>> CalculateAsync(
        int buildingId,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating cooling load for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Cooling load calculation failed because building {BuildingId} was not found.", buildingId);
            return Result<BuildingCalculationResult>.NotFound($"Building with id {buildingId} not found.");
        }

        var validation = await _iso52016ClimateDataValidator.ValidateAsync(building, method, cancellationToken);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Cooling load validation failed for building {BuildingId}: {Error}.",
                buildingId,
                validation.Error);
            return Result<BuildingCalculationResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _calculator.CalculateBuildingAsync(building, method, preferences, cancellationToken);
        _logger.LogInformation(
            "Calculated cooling load for building {BuildingId}: design capacity {DesignCapacityKw} kW.",
            buildingId,
            result.DesignCapacityKw);
        return Result<BuildingCalculationResult>.Success(result);
    }
}


