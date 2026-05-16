using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Api.Security.Authorization;
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
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public BuildingEnergyBalanceReportsController(
        IBuildingEnergyBalanceReportsFacade reports,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _reports = reports;
        _authorizationGate = authorizationGate;
    }

    [HttpGet("excel")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<IActionResult> DownloadEnergyBalanceReportExcel(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireReportReadPermissionAsync(
            projectId: null,
            buildingId: buildingId,
            workflowId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _reports.GenerateEnergyBalanceReportExcelAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.ToFailureResult(this);
        }

        return File(
            result.Value,
            ExcelContentType,
            $"building-{buildingId}-energy-balance.xlsx");
    }

    private ActionResult ToActionResult(ProtectedEndpointAuthorizationDecision decision)
    {
        return decision.Outcome switch
        {
            ProtectedEndpointAuthorizationOutcome.Unauthorized => Unauthorized(),
            ProtectedEndpointAuthorizationOutcome.Forbidden => Forbid(),
            ProtectedEndpointAuthorizationOutcome.NotFound => NotFound(),
            _ => Ok()
        };
    }
}
