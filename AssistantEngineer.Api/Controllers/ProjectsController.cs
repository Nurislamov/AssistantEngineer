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
    private readonly IProjectsFacade _projects;

    public ProjectsController(IProjectsFacade projects)
    {
        _projects = projects;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _projects.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), project => project.Id);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _projects.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _projects.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
