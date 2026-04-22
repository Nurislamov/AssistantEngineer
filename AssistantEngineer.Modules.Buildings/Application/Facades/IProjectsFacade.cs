using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public interface IProjectsFacade
{
    Task<Result<ProjectResponse>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
    Task<Result<List<ProjectResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<ProjectResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
}
