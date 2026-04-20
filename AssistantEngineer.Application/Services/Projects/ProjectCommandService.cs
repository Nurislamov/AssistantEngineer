using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Projects;

public class ProjectCommandService
{
    private readonly IProjectRepository _projects;
    private readonly IAppDbContext _context;
    private readonly ILogger<ProjectCommandService> _logger;

    public ProjectCommandService(
        IProjectRepository projects,
        IAppDbContext context,
        ILogger<ProjectCommandService>? logger = null)
    {
        _projects = projects;
        _context = context;
        _logger = logger ?? NullLogger<ProjectCommandService>.Instance;
    }

    public async Task<Result<ProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating project with name {ProjectName}.", request.Name);

        var projectResult = Project.Create(request.Name);
        if (projectResult.IsFailure)
        {
            _logger.LogWarning("Project creation failed for name {ProjectName}: {Error}.", request.Name, projectResult.Error);
            return Result<ProjectResponse>.Failure(projectResult);
        }

        var project = projectResult.Value;
        _projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created project {ProjectId} with name {ProjectName}.", project.Id, project.Name);
        return Result<ProjectResponse>.Success(ApplicationMapper.ToResponse(project));
    }
}
