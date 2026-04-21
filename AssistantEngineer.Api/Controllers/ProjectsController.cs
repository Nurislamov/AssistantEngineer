using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectCommandService _command;
    private readonly ProjectQueryService _query;

    public ProjectsController(ProjectCommandService command, ProjectQueryService query)
    {
        _command = command;
        _query = query;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), project => project.Id);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _query.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
