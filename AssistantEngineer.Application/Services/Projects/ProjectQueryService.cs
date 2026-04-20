using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Projects;

public class ProjectQueryService
{
    private readonly IProjectRepository _projects;
    private readonly ILogger<ProjectQueryService> _logger;

    public ProjectQueryService(
        IProjectRepository projects,
        ILogger<ProjectQueryService>? logger = null)
    {
        _projects = projects;
        _logger = logger ?? NullLogger<ProjectQueryService>.Instance;
    }

    public async Task<Result<List<ProjectResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        _logger.LogDebug("Loaded {ProjectCount} projects.", projects.Count);
        return Result<List<ProjectResponse>>.Success(projects.Select(ApplicationMapper.ToResponse).ToList());
    }

    public async Task<Result<ProjectResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} was not found.", id);
            return Result<ProjectResponse>.NotFound($"Project with id {id} not found.");
        }

        _logger.LogDebug("Loaded project {ProjectId}.", id);
        return Result<ProjectResponse>.Success(ApplicationMapper.ToResponse(project));
    }
}
