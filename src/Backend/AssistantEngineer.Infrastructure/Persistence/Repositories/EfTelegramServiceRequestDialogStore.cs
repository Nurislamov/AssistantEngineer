using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramServiceRequestDialogStore : ITelegramServiceRequestDialogStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramServiceRequestDialogStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<TelegramServiceRequestMessageSnapshot> AddMessageAsync(
        TelegramServiceRequestMessageCreate message,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramServiceRequestMessageEntity
        {
            ServiceRequestId = message.ServiceRequestId,
            Direction = message.Direction,
            SenderTelegramUserId = message.SenderTelegramUserId,
            SenderRole = message.SenderRole,
            Text = message.Text,
            TelegramChatId = message.TelegramChatId,
            TelegramMessageId = message.TelegramMessageId,
            CreatedAt = message.CreatedAt
        };
        context.TelegramServiceRequestMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<IReadOnlyList<TelegramServiceRequestMessageSnapshot>> GetLatestMessagesAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = await context.TelegramServiceRequestMessages
            .AsNoTracking()
            .Where(item => item.ServiceRequestId == serviceRequestId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 50))
            .ToArrayAsync(cancellationToken);
        return messages.Reverse().Select(ToSnapshot).ToArray();
    }

    public async Task<bool> HasOperatorReplyAsync(
        long serviceRequestId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.TelegramServiceRequestMessages
            .AsNoTracking()
            .AnyAsync(item =>
                item.ServiceRequestId == serviceRequestId &&
                item.Direction == TelegramServiceRequestMessageDirection.OperatorToUser,
                cancellationToken);
    }

    public async Task<TelegramServiceRequestPendingSnapshot?> GetPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await context.TelegramServiceRequestPending
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.TelegramUserId == telegramUserId &&
                item.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);
        return entity is null ? null : ToSnapshot(entity);
    }

    public async Task<TelegramServiceRequestPendingSnapshot> SetPendingAsync(
        long telegramUserId,
        TelegramServiceRequestPendingKind kind,
        long? serviceRequestId,
        string? pendingText,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await context.TelegramServiceRequestPending
            .SingleOrDefaultAsync(item => item.TelegramUserId == telegramUserId, cancellationToken);
        if (entity is null)
        {
            entity = new TelegramServiceRequestPendingEntity { TelegramUserId = telegramUserId };
            context.TelegramServiceRequestPending.Add(entity);
        }
        entity.Kind = kind;
        entity.ServiceRequestId = serviceRequestId;
        entity.PendingText = pendingText;
        entity.CreatedAt = createdAt;
        entity.ExpiresAt = expiresAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task ClearPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.TelegramServiceRequestPending
            .Where(item => item.TelegramUserId == telegramUserId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static TelegramServiceRequestMessageSnapshot ToSnapshot(TelegramServiceRequestMessageEntity entity) =>
        new(entity.Id, entity.ServiceRequestId, entity.Direction, entity.SenderTelegramUserId, entity.SenderRole,
            entity.Text, entity.TelegramChatId, entity.TelegramMessageId, entity.CreatedAt);

    private static TelegramServiceRequestPendingSnapshot ToSnapshot(TelegramServiceRequestPendingEntity entity) =>
        new(entity.Id, entity.TelegramUserId, entity.Kind, entity.ServiceRequestId, entity.PendingText,
            entity.CreatedAt, entity.ExpiresAt);
}
