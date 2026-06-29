using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramLibraryAccessStore : ITelegramLibraryAccessStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramLibraryAccessStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> HasActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.TelegramLibraryAccessGrants
            .AsNoTracking()
            .AnyAsync(
                grant =>
                    grant.TelegramUserId == telegramUserDatabaseId &&
                    grant.IsActive &&
                    grant.RevokedAt == null,
                cancellationToken);
    }

    public async Task<TelegramLibraryAccessGrant?> GetActiveGrantAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var grant = await context.TelegramLibraryAccessGrants
            .AsNoTracking()
            .Where(item =>
                item.TelegramUserId == telegramUserDatabaseId &&
                item.IsActive &&
                item.RevokedAt == null)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return grant is null ? null : ToGrant(grant);
    }

    public async Task GrantAsync(
        long telegramUserDatabaseId,
        long grantedByTelegramUserDatabaseId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var active = await context.TelegramLibraryAccessGrants
            .Where(grant =>
                grant.TelegramUserId == telegramUserDatabaseId &&
                grant.IsActive &&
                grant.RevokedAt == null)
            .ToArrayAsync(cancellationToken);
        foreach (var grant in active)
        {
            grant.IsActive = false;
            grant.UpdatedAt = now;
            grant.RevokedAt = now;
        }

        await context.TelegramLibraryAccessGrants.AddAsync(
            new TelegramLibraryAccessGrantEntity
            {
                TelegramUserId = telegramUserDatabaseId,
                GrantedByTelegramUserId = grantedByTelegramUserDatabaseId,
                IsActive = true,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        long telegramUserDatabaseId,
        long revokedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var active = await context.TelegramLibraryAccessGrants
            .Where(grant =>
                grant.TelegramUserId == telegramUserDatabaseId &&
                grant.IsActive &&
                grant.RevokedAt == null)
            .ToArrayAsync(cancellationToken);
        if (active.Length == 0)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var grant in active)
        {
            grant.IsActive = false;
            grant.UpdatedAt = now;
            grant.RevokedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TelegramLibraryAccessRequest> CreateOrGetPendingRequestAsync(
        TelegramUserSnapshot user,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await context.TelegramLibraryAccessRequests
            .AsNoTracking()
            .Where(request =>
                request.TelegramUserId == user.Id &&
                request.Status == TelegramLibraryAccessRequestStatus.Pending)
            .OrderByDescending(request => request.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return ToRequest(existing);
        }

        var entity = new TelegramLibraryAccessRequestEntity
        {
            TelegramUserId = user.Id,
            TelegramChatId = user.TelegramChatId,
            RequestedRole = user.Role,
            Status = TelegramLibraryAccessRequestStatus.Pending,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await context.TelegramLibraryAccessRequests.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToRequest(entity);
    }

    public async Task<IReadOnlyList<TelegramLibraryAccessRequest>> ListPendingRequestsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var requests = await context.TelegramLibraryAccessRequests
            .AsNoTracking()
            .Where(request => request.Status == TelegramLibraryAccessRequestStatus.Pending)
            .OrderBy(request => request.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
        return requests.Select(ToRequest).ToArray();
    }

    public async Task<TelegramLibraryAccessRequest?> GetRequestAsync(
        long requestId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await context.TelegramLibraryAccessRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        return request is null ? null : ToRequest(request);
    }

    public async Task<TelegramLibraryAccessRequest?> ResolveRequestAsync(
        long requestId,
        TelegramLibraryAccessRequestStatus status,
        long resolvedByTelegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await context.TelegramLibraryAccessRequests
            .FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        if (request.Status == TelegramLibraryAccessRequestStatus.Pending)
        {
            request.Status = status;
            request.ResolvedAt = DateTimeOffset.UtcNow;
            request.ResolvedByTelegramUserId = resolvedByTelegramUserDatabaseId;
            await context.SaveChangesAsync(cancellationToken);
        }

        return ToRequest(request);
    }

    private static TelegramLibraryAccessGrant ToGrant(TelegramLibraryAccessGrantEntity entity) =>
        new(
            entity.TelegramUserId,
            entity.GrantedByTelegramUserId,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.RevokedAt,
            entity.Reason);

    private static TelegramLibraryAccessRequest ToRequest(TelegramLibraryAccessRequestEntity entity) =>
        new(
            entity.Id,
            entity.TelegramUserId,
            entity.TelegramChatId,
            entity.RequestedRole,
            entity.Status,
            entity.Message,
            entity.CreatedAt,
            entity.ResolvedAt,
            entity.ResolvedByTelegramUserId);
}
