using AssistantEngineer.Modules.Identity.Domain.Entities;

namespace AssistantEngineer.Modules.Identity.Application.Abstractions;

public interface IOrganizationMembershipRepository
{
    Task<OrganizationMembership?> GetActiveMembershipAsync(
        int userId,
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembership>> ListActiveByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken = default);
}
