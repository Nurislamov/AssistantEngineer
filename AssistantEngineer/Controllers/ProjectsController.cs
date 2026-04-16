using AssistantEngineer.Application.Services.Projects;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectApplicationService _projects;

    public ProjectsController(ProjectApplicationService projects)
    {
        _projects = projects;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request)
    {
        var response = await _projects.CreateAsync(request);
        return CreatedAtAction(nameof(GetProject), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects()
    {
        return Ok(await _projects.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(int id)
    {
        var project = await _projects.GetByIdAsync(id);

        if (project == null)
            return NotFound();

        return Ok(project);
    }
}
