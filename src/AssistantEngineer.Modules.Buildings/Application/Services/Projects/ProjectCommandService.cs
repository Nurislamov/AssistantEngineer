using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Projects;

public class ProjectCommandService
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProjectCommandService> _logger;

    public ProjectCommandService(
        IProjectRepository projects,
        IUnitOfWork unitOfWork,
        ILogger<ProjectCommandService>? logger = null)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created project {ProjectId} with name {ProjectName}.", project.Id, project.Name);
        return Result<ProjectResponse>.Success(BuildingsMapper.ToResponse(project));
    }
}
