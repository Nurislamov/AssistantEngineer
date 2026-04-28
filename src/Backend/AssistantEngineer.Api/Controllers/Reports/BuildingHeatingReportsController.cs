using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Reports;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/buildings/{buildingId:int}/heating")]
public sealed class BuildingHeatingReportsController : ControllerBase
{
    private readonly IBuildingHeatingReportsFacade _reports;

    public BuildingHeatingReportsController(
        IBuildingHeatingReportsFacade reports)
    {
        _reports = reports;
    }

    [HttpGet]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingReport>> GetHeatingReport(
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
}

