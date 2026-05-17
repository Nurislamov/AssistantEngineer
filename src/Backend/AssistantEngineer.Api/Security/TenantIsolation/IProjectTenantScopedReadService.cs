using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public interface IProjectTenantScopedReadService
{
    Task<Result<Project?>> GetProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Project>>> ListProjectsForTenantAsync(
        TenantQueryContext context,
        CancellationToken cancellationToken = default);
}
