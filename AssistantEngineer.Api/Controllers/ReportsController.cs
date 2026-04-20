using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Api;
using AssistantEngineer.Application;
using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Common;
using AssistantEngineer.Application.Contracts.Reports;
using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Application.Services.Reports;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly BuildingReportDataService _reportDataService;
    private readonly IBuildingReportExporter _reportExporter;
    private readonly BuildingEnergyBalanceService _energyBalanceService;

    public ReportsController(
        BuildingReportDataService reportDataService,
        IBuildingReportExporter reportExporter,
        BuildingEnergyBalanceService energyBalanceService)
    {
        _reportDataService = reportDataService;
        _reportExporter = reportExporter;
        _energyBalanceService = energyBalanceService;
    }

    [HttpGet("buildings/{buildingId:int}")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingReport>> GetBuildingReport(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType,
        CancellationToken cancellationToken)
    {
        var argumentCheck = ValidateEquipmentSelectionArguments(systemType, unitType);
        if (argumentCheck is not null)
            return argumentCheck;

        var result = await _reportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType,
            method.ToDomain(),
            cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("buildings/{buildingId:int}/excel")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<IActionResult> DownloadBuildingReportExcel(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType,
        CancellationToken cancellationToken)
    {
        var argumentCheck = ValidateEquipmentSelectionArguments(systemType, unitType);
        if (argumentCheck is not null)
            return argumentCheck;

        var result = await _reportDataService.BuildReportAsync(
            buildingId,
            systemType,
            unitType,
            method.ToDomain(),
            cancellationToken);
        if (result.IsFailure)
            return result.ToFailureResult();

        var content = _reportExporter.GenerateBuildingReport(result.Value, cancellationToken);
        var fileName = $"building-{buildingId}-report.xlsx";

        return File(content, ExcelContentType, fileName);
    }

    [HttpGet("buildings/{buildingId:int}/heating")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<HeatingReport>> GetHeatingReport(
        int buildingId,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _reportDataService.BuildHeatingReportAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("buildings/{buildingId:int}/energy-balance/excel")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<IActionResult> DownloadEnergyBalanceReportExcel(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _energyBalanceService.CalculateAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);
        if (result.IsFailure)
            return result.ToFailureResult();

        var content = _reportExporter.GenerateEnergyBalanceReport(result.Value, cancellationToken);
        var fileName = $"building-{buildingId}-energy-balance.xlsx";

        return File(content, ExcelContentType, fileName);
    }

    private static BadRequestObjectResult? ValidateEquipmentSelectionArguments(string? systemType, string? unitType)
    {
        var hasSystemType = !string.IsNullOrWhiteSpace(systemType);
        var hasUnitType = !string.IsNullOrWhiteSpace(unitType);

        return hasSystemType == hasUnitType
            ? null
            : new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request failed",
                Detail = "Both systemType and unitType must be provided together for equipment selection."
            });
    }
}
