using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Buildings;

public class BuildingHeatingLoadService
{
    private readonly IBuildingHeatingReadModelRepository _buildings;
    private readonly BuildingHeatingReadModelCalculator _heatingLoadCalculator;
    private readonly ILogger<BuildingHeatingLoadService> _logger;

    public BuildingHeatingLoadService(
        IBuildingHeatingReadModelRepository buildings,
        BuildingHeatingReadModelCalculator heatingLoadCalculator,
        ILogger<BuildingHeatingLoadService>? logger = null)
    {
        _buildings = buildings;
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

        var building = await _buildings.GetByIdAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Heating load calculation failed because building {BuildingId} was not found.", buildingId);
            return Result<BuildingHeatingLoadResult>.NotFound($"Building with id {buildingId} not found.");
        }

        if (building.WinterDesignTemperatureC is null)
        {
            _logger.LogWarning(
                "Heating load validation failed for building {BuildingId}: {Error}.",
                buildingId,
                "Building climate zone is required for EN 12831 heating load calculation.");
            return Result<BuildingHeatingLoadResult>.Validation(
                "Building climate zone is required for EN 12831 heating load calculation.");
        }

        var result = await _heatingLoadCalculator.CalculateAsync(building, method, cancellationToken: cancellationToken);
        _logger.LogInformation(
            "Calculated heating load for building {BuildingId}: design load {TotalDesignHeatingLoadKw} kW.",
            buildingId,
            result.TotalDesignHeatingLoadKw);
        return Result<BuildingHeatingLoadResult>.Success(result);
    }
}
