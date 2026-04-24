using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public ProjectsController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateProjectAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), project => project.Id);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> GetAll(
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetProjectsAsync(cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        var items = SortProjects(
            result.Value.ApplySearch(query.Search, project => project.Name),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _buildings.GetProjectByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    private static IEnumerable<ProjectResponse> SortProjects(
        IEnumerable<ProjectResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? source.OrderByDescending(project => project.Name).ThenByDescending(project => project.Id)
                : source.OrderBy(project => project.Name).ThenBy(project => project.Id),
            _ => query.SortDescending
                ? source.OrderByDescending(project => project.Id)
                : source.OrderBy(project => project.Id)
        };
}
