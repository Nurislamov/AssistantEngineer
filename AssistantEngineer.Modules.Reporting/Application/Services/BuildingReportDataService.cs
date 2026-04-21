using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

public class BuildingReportDataService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly IEquipmentCatalogRepository _catalog;
    private readonly Iso52016ClimateDataValidator _iso52016ClimateDataValidator;
    private readonly BuildingReportCalculationService _calculationService;
    private readonly BuildingReportGenerator _reportGenerator;
    private readonly ILogger<BuildingReportDataService> _logger;

    public BuildingReportDataService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        IEquipmentCatalogRepository catalog,
        Iso52016ClimateDataValidator iso52016ClimateDataValidator,
        BuildingReportCalculationService calculationService,
        BuildingReportGenerator reportGenerator,
        ILogger<BuildingReportDataService>? logger = null)
    {
        _buildings = buildings;
        _preferences = preferences;
        _catalog = catalog;
        _iso52016ClimateDataValidator = iso52016ClimateDataValidator;
        _calculationService = calculationService;
        _reportGenerator = reportGenerator;
        _logger = logger ?? NullLogger<BuildingReportDataService>.Instance;
    }

    public async Task<Result<BuildingReport>> BuildReportAsync(
        int buildingId,
        string? systemType = null,
        string? unitType = null,
        CoolingLoadCalculationMethod method = CoolingLoadCalculationMethod.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building cooling report generation started for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var building = await _buildings.GetForReportAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Cooling report generation failed because building {BuildingId} was not found.", buildingId);
            return Result<BuildingReport>.NotFound($"Building with id {buildingId} not found.");
        }

        var validation = await _iso52016ClimateDataValidator.ValidateAsync(building, method, cancellationToken);
        if (validation.IsFailure)
        {
            _logger.LogWarning(
                "Cooling report validation failed for building {BuildingId}: {Error}.",
                buildingId,
                validation.Error);
            return Result<BuildingReport>.Failure(validation);
        }

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var equipmentSelectionRequested =
            !string.IsNullOrWhiteSpace(systemType) &&
            !string.IsNullOrWhiteSpace(unitType);
        var catalog = equipmentSelectionRequested
            ? await _catalog.ListActiveByTypeAsync(systemType!, unitType!, cancellationToken)
            : [];
        if (equipmentSelectionRequested)
        {
            _logger.LogInformation(
                "Loaded {CatalogItemCount} catalog items for cooling report building {BuildingId}.",
                catalog.Count,
                buildingId);
        }

        var data = await _calculationService.BuildCoolingDataAsync(
            building,
            preferences,
            catalog,
            systemType,
            unitType,
            method,
            cancellationToken);
        var report = _reportGenerator.GenerateCoolingReport(data);

        _logger.LogInformation(
            "Building cooling report generated for building {BuildingId}: {RoomCount} rooms, {RoomsWithSelectionCount} rooms with selection.",
            buildingId,
            report.Rooms.Count,
            report.RoomsWithSelectionCount);
        return Result<BuildingReport>.Success(report);
    }

    public async Task<Result<HeatingReport>> BuildHeatingReportAsync(
        int buildingId,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building heating report generation started for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var building = await _buildings.GetForReportAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Heating report generation failed because building {BuildingId} was not found.", buildingId);
            return Result<HeatingReport>.NotFound($"Building with id {buildingId} not found.");
        }

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var data = await _calculationService.BuildHeatingDataAsync(
            building,
            preferences,
            method,
            cancellationToken);
        var report = _reportGenerator.GenerateHeatingReport(data);

        _logger.LogInformation(
            "Building heating report generated for building {BuildingId}: {RoomCount} rooms, total design load {TotalDesignHeatingLoadKw} kW.",
            buildingId,
            report.RoomsCount,
            report.TotalDesignHeatingLoadKw);

        return Result<HeatingReport>.Success(report);
    }
}
