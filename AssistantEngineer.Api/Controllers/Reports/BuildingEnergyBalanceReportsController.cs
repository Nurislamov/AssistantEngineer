using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Reports;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/buildings/{buildingId:int}/energy-balance")]
public sealed class BuildingEnergyBalanceReportsController : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IBuildingEnergyBalanceReportsFacade _reports;

    public BuildingEnergyBalanceReportsController(
        IBuildingEnergyBalanceReportsFacade reports)
    {
        _reports = reports;
    }

    [HttpGet("excel")]
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

        return File(
            result.Value,
            ExcelContentType,
            $"building-{buildingId}-energy-balance.xlsx");
    }
}
