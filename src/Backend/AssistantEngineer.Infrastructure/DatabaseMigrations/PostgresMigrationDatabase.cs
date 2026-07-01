using AssistantEngineer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.DatabaseMigrations;

public interface IPostgresMigrationDatabase
{
    Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public sealed class AppDbContextPostgresMigrationDatabase : IPostgresMigrationDatabase
{
    private readonly AppDbContext _context;

    public AppDbContextPostgresMigrationDatabase(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default) =>
        (await _context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default) =>
        (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}
