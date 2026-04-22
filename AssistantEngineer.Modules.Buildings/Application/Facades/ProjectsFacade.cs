using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public sealed class ProjectsFacade : IProjectsFacade
{
    private readonly ProjectCommandService _command;
    private readonly ProjectQueryService _query;

    public ProjectsFacade(ProjectCommandService command, ProjectQueryService query)
    {
        _command = command;
        _query = query;
    }

    public Task<Result<ProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken) =>
        _command.CreateAsync(request, cancellationToken);

    public Task<Result<List<ProjectResponse>>> GetAllAsync(CancellationToken cancellationToken) =>
        _query.GetAllAsync(cancellationToken);

    public Task<Result<ProjectResponse>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _query.GetByIdAsync(id, cancellationToken);
}
