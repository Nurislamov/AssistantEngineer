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
[Route("api/v{version:apiVersion}/floors")]
public class FloorsController : ControllerBase
{
    private readonly IFloorsFacade _floors;

    public FloorsController(IFloorsFacade floors)
    {
        _floors = floors;
    }

    [HttpPost("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<FloorResponse>> Create(
        int buildingId,
        [FromBody] CreateFloorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _floors.CreateAsync(buildingId, request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), floor => floor.Id);
    }

    [HttpGet("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<List<FloorResponse>>> GetByBuilding(
        int buildingId,
        CancellationToken cancellationToken)
    {
        var result = await _floors.GetByBuildingIdAsync(buildingId, cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FloorResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _floors.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<FloorCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _floors.CalculateCoolingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult();
    }
}
