using AssistantEngineer.Modules.Identity.Domain.Entities;

namespace AssistantEngineer.Modules.Identity.Application.Abstractions;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<Organization?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Organization organization,
        CancellationToken cancellationToken = default);
}
