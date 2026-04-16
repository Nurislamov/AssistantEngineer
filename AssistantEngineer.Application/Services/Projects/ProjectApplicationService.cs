using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Projects;

public class ProjectApplicationService
{
    private readonly IAppDbContext _context;

    public ProjectApplicationService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request)
    {
        var project = new Project { Name = request.Name };

        _context.AddProject(project);
        await _context.SaveChangesAsync();

        return ToResponse(project);
    }

    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        return _context.Projects
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name
            })
            .ToList();
    }

    public async Task<ProjectResponse?> GetByIdAsync(int id)
    {
        return _context.Projects
            .Where(project => project.Id == id)
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name
            })
            .FirstOrDefault();
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
