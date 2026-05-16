using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Api.Security.Authorization;
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
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public BuildingHeatingReportsController(
        IBuildingHeatingReportsFacade reports,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _reports = reports;
        _authorizationGate = authorizationGate;
    }

    [HttpGet]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingReport>> GetHeatingReport(
        int buildingId,
        [FromQuery] HeatingLoadCalculationMethodDto method,
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

        var result = await _reports.BuildHeatingReportAsync(
            buildingId,
            method,
            cancellationToken);

        return result.ToOkResult(this);
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
