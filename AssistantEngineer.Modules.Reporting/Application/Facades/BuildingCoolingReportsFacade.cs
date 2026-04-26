using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public sealed class BuildingCoolingReportsFacade : IBuildingCoolingReportsFacade
{
    private readonly BuildingCoolingReportDataService _reportDataService;
    private readonly IBuildingCoolingReportExporter _reportExporter;

    internal BuildingCoolingReportsFacade(
        BuildingCoolingReportDataService reportDataService,
        IBuildingCoolingReportExporter reportExporter)
    {
        _reportDataService = reportDataService;
        _reportExporter = reportExporter;
    }

    public async Task<Result<BuildingCoolingReport>> BuildCoolingReportAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken)
    {
        var argumentCheck = ValidateEquipmentSelectionArguments(
            systemType,
            unitType);

        if (argumentCheck.IsFailure)
            return Result<BuildingCoolingReport>.Failure(argumentCheck);

        return await _reportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType,
            method,
            cancellationToken);
    }

    public async Task<Result<byte[]>> GenerateCoolingReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken)
    {
        var report = await BuildCoolingReportAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);

        if (report.IsFailure)
            return Result<byte[]>.Failure(report);

        return Result<byte[]>.Success(
            _reportExporter.GenerateCoolingReport(
                report.Value,
                cancellationToken));
    }

    private static Result ValidateEquipmentSelectionArguments(
        string? systemType,
        string? unitType)
    {
        var hasSystemType = !string.IsNullOrWhiteSpace(systemType);
        var hasUnitType = !string.IsNullOrWhiteSpace(unitType);

        return hasSystemType == hasUnitType
            ? Result.Success()
            : Result.Validation("Both systemType and unitType must be provided together for equipment selection.");
    }
}