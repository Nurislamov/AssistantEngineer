using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public sealed class ReportsFacade : IReportsFacade
{
    private readonly BuildingReportDataService _reportDataService;
    private readonly IBuildingReportExporter _reportExporter;
    private readonly BuildingEnergyBalanceService _energyBalanceService;

    public ReportsFacade(
        BuildingReportDataService reportDataService,
        IBuildingReportExporter reportExporter,
        BuildingEnergyBalanceService energyBalanceService)
    {
        _reportDataService = reportDataService;
        _reportExporter = reportExporter;
        _energyBalanceService = energyBalanceService;
    }

    public async Task<Result<BuildingReport>> BuildBuildingReportAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken)
    {
        var argumentCheck = ValidateEquipmentSelectionArguments(systemType, unitType);
        if (argumentCheck.IsFailure)
            return Result<BuildingReport>.Failure(argumentCheck);

        return await _reportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType,
            method.ToDomain(),
            cancellationToken);
    }

    public async Task<Result<byte[]>> GenerateBuildingReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken)
    {
        var report = await BuildBuildingReportAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);
        if (report.IsFailure)
            return Result<byte[]>.Failure(report);

        return Result<byte[]>.Success(
            _reportExporter.GenerateBuildingReport(report.Value, cancellationToken));
    }

    public Task<Result<HeatingReport>> BuildHeatingReportAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _reportDataService.BuildHeatingReportAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);

    public async Task<Result<byte[]>> GenerateEnergyBalanceReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _energyBalanceService.CalculateAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);
        if (result.IsFailure)
            return Result<byte[]>.Failure(result);

        return Result<byte[]>.Success(
            _reportExporter.GenerateEnergyBalanceReport(result.Value, cancellationToken));
    }

    private static Result ValidateEquipmentSelectionArguments(string? systemType, string? unitType)
    {
        var hasSystemType = !string.IsNullOrWhiteSpace(systemType);
        var hasUnitType = !string.IsNullOrWhiteSpace(unitType);

        return hasSystemType == hasUnitType
            ? Result.Success()
            : Result.Validation("Both systemType and unitType must be provided together for equipment selection.");
    }
}
