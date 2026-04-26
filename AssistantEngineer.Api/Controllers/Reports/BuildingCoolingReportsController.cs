using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Reports;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/buildings/{buildingId:int}/cooling")]
public sealed class BuildingCoolingReportsController : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IBuildingCoolingReportsFacade _reports;

    public BuildingCoolingReportsController(
        IBuildingCoolingReportsFacade reports)
    {
        _reports = reports;
    }

    [HttpGet]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCoolingReport>> GetCoolingReport(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType,
        CancellationToken cancellationToken)
    {
        var result = await _reports.BuildCoolingReportAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);

        return result.ToOkResult(this);
    }

    [HttpGet("excel")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<IActionResult> DownloadCoolingReportExcel(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        [FromQuery] string? systemType,
        [FromQuery] string? unitType,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GenerateCoolingReportExcelAsync(
            buildingId,
            method,
            systemType,
            unitType,
            cancellationToken);

        if (result.IsFailure)
            return result.ToFailureResult(this);

        return File(
            result.Value,
            ExcelContentType,
            $"building-{buildingId}-cooling-report.xlsx");
    }
}
