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
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public BuildingsController(
        IBuildingsFacade buildings,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _buildings = buildings;
        _authorizationGate = authorizationGate;
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            projectId,
            Permission.BuildingsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

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
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            projectId,
            Permission.BuildingsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

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
        var authorizationDecision = await _authorizationGate.RequireBuildingPermissionAsync(
            id,
            Permission.BuildingsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.GetBuildingByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> Update(
        int id,
        [FromBody] UpdateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireBuildingPermissionAsync(
            id,
            Permission.BuildingsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.UpdateBuildingAsync(
            id,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireBuildingPermissionAsync(
            id,
            Permission.BuildingsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.DeleteBuildingAsync(
            id,
            cancellationToken);

        return result.ToNoContentResult(this);
    }

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<PagedResponse<BuildingResponse>>> GetByProject(
        int projectId,
        [FromQuery] BuildingListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            projectId,
            Permission.BuildingsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

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

