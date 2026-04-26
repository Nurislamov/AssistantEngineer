using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingHeatingReportDataService
{
    private readonly BuildingHeatingReportCalculationService _calculationService;
    private readonly BuildingHeatingReportGenerator _reportGenerator;
    private readonly ILogger<BuildingHeatingReportDataService> _logger;

    public BuildingHeatingReportDataService(
        BuildingHeatingReportCalculationService calculationService,
        BuildingHeatingReportGenerator reportGenerator,
        ILogger<BuildingHeatingReportDataService>? logger = null)
    {
        _calculationService = calculationService;
        _reportGenerator = reportGenerator;
        _logger = logger ?? NullLogger<BuildingHeatingReportDataService>.Instance;
    }

    public async Task<Result<BuildingHeatingReport>> BuildHeatingReportAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method = HeatingLoadCalculationMethodDto.En12831,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building heating report generation started for building {BuildingId} using {CalculationMethod}.",
            buildingId,
            method);

        var heating = await _calculationService.CalculateBuildingHeatingLoadAsync(
            buildingId,
            method,
            cancellationToken);

        if (heating.IsFailure)
        {
            _logger.LogWarning(
                "Heating report generation failed for building {BuildingId}: {Error}.",
                buildingId,
                heating.Error);

            return Result<BuildingHeatingReport>.Failure(heating);
        }

        var report = _reportGenerator.Generate(heating.Value);

        _logger.LogInformation(
            "Building heating report generated for building {BuildingId}: {RoomCount} rooms, total design load {TotalDesignHeatingLoadKw} kW.",
            buildingId,
            report.RoomsCount,
            report.TotalDesignHeatingLoadKw);

        return Result<BuildingHeatingReport>.Success(report);
    }
}