using AssistantEngineer.Api.Contracts.Buildings;
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
[Route("api/v{version:apiVersion}/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public BuildingsController(
        IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateBuildingAsync(
            projectId,
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            building => building.Id);
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateBuildingFromArchetypeAsync(
            projectId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetBuildingByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<PagedResponse<BuildingResponse>>> GetByProject(
        int projectId,
        [FromQuery] BuildingListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetBuildingsByProjectAsync(
            projectId,
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyBuildingListQuery(query));
    }

    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<BuildingResponse>, bool, IOrderedEnumerable<BuildingResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<BuildingResponse>, bool, IOrderedEnumerable<BuildingResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, building => building.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, building => building.Name)
                    .ThenByStable(descending, building => building.Id),

            ["climatezonename"] = (items, descending) =>
                items.SortBy(descending, building => building.ClimateZoneName)
                    .ThenByStable(descending, building => building.Id)
        };
}
