using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Projects;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<ProjectResponse>, bool, IOrderedEnumerable<ProjectResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<ProjectResponse>, bool, IOrderedEnumerable<ProjectResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, project => project.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, project => project.Name)
                    .ThenByStable(descending, project => project.Id)
        };

    private readonly IBuildingsFacade _buildings;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public ProjectsController(
        IBuildingsFacade buildings,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _buildings = buildings;
        _authorizationGate = authorizationGate;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequirePermissionAsync(
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.CreateProjectAsync(
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            project => project.Id);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> GetAll(
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequirePermissionAsync(
            Permission.ProjectsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.GetProjectsAsync(
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyProjectListQuery(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.GetProjectByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.UpdateProjectAsync(
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
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.DeleteProjectAsync(
            id,
            cancellationToken);

        return result.ToNoContentResult(this);
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
