using AssistantEngineer.Modules.Identity.Domain.Entities;

namespace AssistantEngineer.Modules.Identity.Application.Abstractions;

public interface IIdentityUserRepository
{
    Task<User?> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByExternalSubjectIdAsync(
        string externalSubjectId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}
