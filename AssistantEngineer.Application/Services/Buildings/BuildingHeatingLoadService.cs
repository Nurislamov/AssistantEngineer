using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Buildings;

public class BuildingHeatingLoadService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IBuildingHeatingLoadCalculator _heatingLoadCalculator;
    private readonly ILogger<BuildingHeatingLoadService> _logger;

    public BuildingHeatingLoadService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        IBuildingHeatingLoadCalculator heatingLoadCalculator,
        ILogger<BuildingHeatingLoadService>? logger = null)
    {
        _buildings = buildings;
        _preferences = preferences;
        _heatingLoadCalculator = heatingLoadCalculator;
        _logger = logger ?? NullLogger<BuildingHeatingLoadService>.Instance;
    }

    public async Task<Result<BuildingHeatingLoadResult>> CalculateAsync(
        int buildingId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating heating load for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Heating load calculation failed because building {BuildingId} was not found.", buildingId);
            return Result<BuildingHeatingLoadResult>.NotFound($"Building with id {buildingId} not found.");
        }

        var validation = BuildingCalculationDataValidator.ValidateHeatingLoadData(building);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Heating load validation failed for building {BuildingId}: {Error}.",
                buildingId,
                validation.Error);
            return Result<BuildingHeatingLoadResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _heatingLoadCalculator.CalculateAsync(building, method, preferences, cancellationToken);
        _logger.LogInformation(
            "Calculated heating load for building {BuildingId}: design load {TotalDesignHeatingLoadKw} kW.",
            buildingId,
            result.TotalDesignHeatingLoadKw);
        return Result<BuildingHeatingLoadResult>.Success(result);
    }
}
