using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public class ReportsController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IReportsFacade _reports;

    public ReportsController(IReportsFacade reports)
    {
        _reports = reports;
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
        var result = await _reports.BuildBuildingReportAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);
        return result.ToOkResult(this);
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
        var result = await _reports.GenerateBuildingReportExcelAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);
        if (result.IsFailure)
            return result.ToFailureResult(this);

        return File(result.Value, ExcelContentType, $"building-{buildingId}-report.xlsx");
    }

    [HttpGet("buildings/{buildingId:int}/heating")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<HeatingReport>> GetHeatingReport(
        int buildingId,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _reports.BuildHeatingReportAsync(
            buildingId,
            method,
            cancellationToken);
        return result.ToOkResult(this);
    }

    [HttpGet("buildings/{buildingId:int}/energy-balance/excel")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<IActionResult> DownloadEnergyBalanceReportExcel(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GenerateEnergyBalanceReportExcelAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);
        if (result.IsFailure)
            return result.ToFailureResult(this);

        return File(result.Value, ExcelContentType, $"building-{buildingId}-energy-balance.xlsx");
    }
}
