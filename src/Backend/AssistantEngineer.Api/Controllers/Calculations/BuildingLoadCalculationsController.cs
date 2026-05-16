using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations")]
public sealed class BuildingLoadCalculationsController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public BuildingLoadCalculationsController(
        ILoadCalculationsFacade loadCalculations,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _loadCalculations = loadCalculations;
        _authorizationGate = authorizationGate;
    }

    [HttpGet("cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateCoolingLoad(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: buildingId,
            floorId: null,
            roomId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _loadCalculations.CalculateBuildingCoolingLoadAsync(
            buildingId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingLoadResult>> CalculateHeatingLoad(
        int buildingId,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: buildingId,
            floorId: null,
            roomId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _loadCalculations.CalculateBuildingHeatingLoadAsync(
            buildingId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("energy-balance")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyBalanceResult>> CalculateEnergyBalance(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: buildingId,
            floorId: null,
            roomId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _loadCalculations.CalculateBuildingEnergyBalanceAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);

        return result.ToActionResult(this);
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

