using AssistantEngineer.Contracts;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var response = ToResponse(project);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects()
    {
        var projects = await _context.Projects
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(int id)
    {
        var project = await _context.Projects
            .Where(project => project.Id == id)
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name
            })
            .FirstOrDefaultAsync();

        if (project == null)
            return NotFound();

        return Ok(project);
    }

    private static ProjectResponse ToResponse(Project project)
    {
        return new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name
        };
    }
}
