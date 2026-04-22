using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public BuildingsController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateAsync(projectId, request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), building => building.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _buildings.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<List<BuildingResponse>>> GetByProject(
        int projectId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetByProjectIdAsync(projectId, cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CalculateCoolingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CalculateHeatingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/energy-balance")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyBalanceResult>> CalculateEnergyBalance(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CalculateEnergyBalanceAsync(
            id,
            coolingMethod,
            heatingMethod,
            cancellationToken);
        return result.ToActionResult();
    }
}
