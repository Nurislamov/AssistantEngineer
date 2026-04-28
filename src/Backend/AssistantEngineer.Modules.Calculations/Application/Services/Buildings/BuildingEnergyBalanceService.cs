using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Buildings;

public class BuildingEnergyBalanceService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IBuildingEnergyCalculator _buildingEnergyCalculator;
    private readonly ILogger<BuildingEnergyBalanceService> _logger;

    public BuildingEnergyBalanceService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        IBuildingEnergyCalculator buildingEnergyCalculator,
        ILogger<BuildingEnergyBalanceService>? logger = null)
    {
        _buildings = buildings;
        _preferences = preferences;
        _buildingEnergyCalculator = buildingEnergyCalculator;
        _logger = logger ?? NullLogger<BuildingEnergyBalanceService>.Instance;
    }

    public async Task<Result<BuildingEnergyBalanceResult>> CalculateAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod = CoolingLoadCalculationMethod.Iso52016,
        HeatingLoadCalculationMethod heatingMethod = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating energy balance for building {BuildingId} using cooling {CoolingMethod} and heating {HeatingMethod}.",
            buildingId,
            coolingMethod,
            heatingMethod);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Energy balance calculation failed because building {BuildingId} was not found.", buildingId);
            return Result<BuildingEnergyBalanceResult>.NotFound($"Building with id {buildingId} not found.");
        }

        var validation = BuildingCalculationDataValidator.ValidateHeatingLoadData(building);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Energy balance validation failed for building {BuildingId}: {Error}.",
                buildingId,
                validation.Error);
            return Result<BuildingEnergyBalanceResult>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _buildingEnergyCalculator.CalculateAsync(
            building,
            coolingMethod,
            heatingMethod,
            preferences,
            cancellationToken);
        _logger.LogInformation(
            "Calculated energy balance for building {BuildingId}: annual total {AnnualTotalDemandKWh} kWh.",
            buildingId,
            result.AnnualTotalDemandKWh);
        return Result<BuildingEnergyBalanceResult>.Success(result);
    }
}


