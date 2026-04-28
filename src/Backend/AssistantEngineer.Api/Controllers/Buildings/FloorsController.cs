using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Buildings;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/floors")]
public class FloorsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public FloorsController(
        IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<FloorResponse>> Create(
        int buildingId,
        [FromBody] CreateFloorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateFloorAsync(
            buildingId,
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            floor => floor.Id);
    }

    [HttpGet("~/api/v{version:apiVersion}/buildings/{buildingId:int}/floors")]
    public async Task<ActionResult<PagedResponse<FloorResponse>>> GetByBuilding(
        int buildingId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetFloorsByBuildingAsync(
            buildingId,
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyFloorListQuery(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FloorResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetFloorByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<FloorResponse>, bool, IOrderedEnumerable<FloorResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<FloorResponse>, bool, IOrderedEnumerable<FloorResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, floor => floor.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, floor => floor.Name)
                    .ThenByStable(descending, floor => floor.Id)
        };
}

