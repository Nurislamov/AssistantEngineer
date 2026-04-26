using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingCoolingReportDataService
{
    private readonly IBuildingRepository _buildings;
    private readonly BuildingCoolingReportCalculationService _calculationService;
    private readonly BuildingCoolingReportGenerator _reportGenerator;
    private readonly ILogger<BuildingCoolingReportDataService> _logger;

    public BuildingCoolingReportDataService(
        IBuildingRepository buildings,
        BuildingCoolingReportCalculationService calculationService,
        BuildingCoolingReportGenerator reportGenerator,
        ILogger<BuildingCoolingReportDataService>? logger = null)
    {
        _buildings = buildings;
        _calculationService = calculationService;
        _reportGenerator = reportGenerator;
        _logger = logger ?? NullLogger<BuildingCoolingReportDataService>.Instance;
    }

    public async Task<Result<BuildingCoolingReport>> BuildReportAsync(
        int buildingId,
        string? systemType = null,
        string? unitType = null,
        CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building cooling report generation started for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var building = await _buildings.GetForReportAsync(
            buildingId,
            cancellationToken);

        if (building is null)
        {
            _logger.LogWarning(
                "Cooling report generation failed because building {BuildingId} was not found.",
                buildingId);

            return Result<BuildingCoolingReport>.NotFound(
                $"Building with id {buildingId} not found.");
        }

        var data = await _calculationService.BuildCoolingDataAsync(
            building,
            systemType,
            unitType,
            method,
            cancellationToken);

        if (data.IsFailure)
        {
            _logger.LogWarning(
                "Cooling report calculation failed for building {BuildingId}: {Error}.",
                buildingId,
                data.Error);

            return Result<BuildingCoolingReport>.Failure(data);
        }

        var report = _reportGenerator.Generate(data.Value);

        _logger.LogInformation(
            "Building cooling report generated for building {BuildingId}: {RoomCount} rooms, {RoomsWithSelectionCount} rooms with selection.",
            buildingId,
            report.Rooms.Count,
            report.RoomsWithSelectionCount);

        return Result<BuildingCoolingReport>.Success(report);
    }
}