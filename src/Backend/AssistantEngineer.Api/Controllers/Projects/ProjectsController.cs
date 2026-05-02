using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Projects;
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

    public ProjectsController(
        IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
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
        var result = await _buildings.DeleteProjectAsync(
            id,
            cancellationToken);

        return result.ToNoContentResult(this);
    }
}
