using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
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
    private readonly IBuildingsFacade _buildings;
    private readonly ICalculationsFacade _calculations;

    public FloorsController(
        IBuildingsFacade buildings,
        ICalculationsFacade calculations)
    {
        _buildings = buildings;
        _calculations = calculations;
    }

    [HttpPost("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<FloorResponse>> Create(
        int buildingId,
        [FromBody] CreateFloorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateFloorAsync(buildingId, request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), floor => floor.Id);
    }

    [HttpGet("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<PagedResponse<FloorResponse>>> GetByBuilding(
        int buildingId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetFloorsByBuildingAsync(buildingId, cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        var items = SortFloors(
            result.Value.ApplySearch(query.Search, floor => floor.Name),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FloorResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetFloorByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<FloorCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _calculations.CalculateFloorCoolingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult(this);
    }

    private static IEnumerable<FloorResponse> SortFloors(
        IEnumerable<FloorResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? source.OrderByDescending(floor => floor.Name).ThenByDescending(floor => floor.Id)
                : source.OrderBy(floor => floor.Name).ThenBy(floor => floor.Id),
            _ => query.SortDescending
                ? source.OrderByDescending(floor => floor.Id)
                : source.OrderBy(floor => floor.Id)
        };
}
