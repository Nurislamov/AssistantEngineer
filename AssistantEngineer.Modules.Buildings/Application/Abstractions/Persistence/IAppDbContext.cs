namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
