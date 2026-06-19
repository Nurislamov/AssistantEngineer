using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramUserAuditEventStore : ITelegramUserAuditEventStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramUserAuditEventStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramUserAuditEventSnapshot> AppendAsync(
        TelegramUserAuditEventCreate request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramUserAuditEventEntity
        {
            EventType = request.EventType,
            ActorTelegramUserId = request.ActorTelegramUserId,
            TargetTelegramUserId = request.TargetTelegramUserId,
            OldRole = request.OldRole,
            NewRole = request.NewRole,
            OldIsEnabled = request.OldIsEnabled,
            NewIsEnabled = request.NewIsEnabled,
            OldIsBlocked = request.OldIsBlocked,
            NewIsBlocked = request.NewIsBlocked,
            IsSuccessful = request.IsSuccessful,
            Message = request.Message,
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt
        };
        await context.TelegramUserAuditEvents.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<IReadOnlyList<TelegramUserAuditEventSnapshot>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await context.TelegramUserAuditEvents
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
        return events.Select(ToSnapshot).ToArray();
    }

    private static TelegramUserAuditEventSnapshot ToSnapshot(TelegramUserAuditEventEntity entity) =>
        new(
            entity.Id,
            entity.EventType,
            entity.ActorTelegramUserId,
            entity.TargetTelegramUserId,
            entity.OldRole,
            entity.NewRole,
            entity.OldIsEnabled,
            entity.NewIsEnabled,
            entity.OldIsBlocked,
            entity.NewIsBlocked,
            entity.IsSuccessful,
            entity.Message,
            entity.MetadataJson,
            entity.CreatedAt);
}
