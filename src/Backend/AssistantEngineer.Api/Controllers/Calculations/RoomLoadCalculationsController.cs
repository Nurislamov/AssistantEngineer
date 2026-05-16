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
[Route("api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations")]
public sealed class RoomLoadCalculationsController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public RoomLoadCalculationsController(
        ILoadCalculationsFacade loadCalculations,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _loadCalculations = loadCalculations;
        _authorizationGate = authorizationGate;
    }

    [HttpGet("cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomCalculationResult>> CalculateCoolingLoad(
        int roomId,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var authorizationDecision = await _authorizationGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: null,
            floorId: null,
            roomId: roomId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _loadCalculations.CalculateRoomCoolingLoadAsync(
            roomId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomHeatingLoadResult>> CalculateHeatingLoad(
        int roomId,
        [FromQuery] HeatingLoadCalculationMethodDto method = HeatingLoadCalculationMethodDto.En12831,
        CancellationToken cancellationToken = default)
    {
        var authorizationDecision = await _authorizationGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: null,
            floorId: null,
            roomId: roomId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _loadCalculations.CalculateRoomHeatingLoadAsync(
            roomId,
            method,
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

