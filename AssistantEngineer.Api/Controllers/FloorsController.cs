using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Api;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Common;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Application.Services.Floors;
using AssistantEngineer.Application;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/floors")]
[Route("api/floors")]
public class FloorsController : ControllerBase
{
    private readonly FloorCommandService _command;
    private readonly FloorQueryService _query;

    public FloorsController(FloorCommandService command, FloorQueryService query)
    {
        _command = command;
        _query = query;
    }

    [HttpPost("{buildingId:int}")]
    public async Task<ActionResult<FloorResponse>> Create(
        int buildingId,
        [FromBody] CreateFloorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(buildingId, request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), floor => floor.Id);
    }

    [HttpGet("building/{buildingId:int}")]
    public async Task<ActionResult<List<FloorResponse>>> GetByBuilding(
        int buildingId,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByBuildingIdAsync(buildingId, cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FloorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/calculate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<FloorCalculationResult>> Calculate(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _query.CalculateAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }
}
