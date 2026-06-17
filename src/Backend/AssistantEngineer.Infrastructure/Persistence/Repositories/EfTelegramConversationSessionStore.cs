using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramConversationSessionStore : ITelegramConversationSessionStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramConversationSessionStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramConversationSessionSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await context.TelegramConversationSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TelegramUserId == telegramUserId, cancellationToken);

        return session is null ? null : ToSnapshot(session);
    }

    public async Task<TelegramConversationSessionSnapshot> UpsertAsync(
        TelegramConversationSessionUpsert session,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await context.TelegramConversationSessions
            .FirstOrDefaultAsync(item => item.TelegramUserId == session.TelegramUserId, cancellationToken);

        if (entity is null)
        {
            entity = new TelegramConversationSessionEntity
            {
                TelegramUserId = session.TelegramUserId,
                CreatedAt = session.UpdatedAt
            };
            await context.TelegramConversationSessions.AddAsync(entity, cancellationToken);
        }

        entity.State = session.State;
        entity.CurrentCode = session.CurrentCode;
        entity.SelectedManufacturer = session.SelectedManufacturer;
        entity.SelectedEquipmentType = session.SelectedEquipmentType;
        entity.SelectedDisplayContext = session.SelectedDisplayContext;
        entity.CandidateOptionsJson = session.CandidateOptionsJson;
        entity.LastPromptMessageId = session.LastPromptMessageId;
        entity.UpdatedAt = session.UpdatedAt;
        entity.ExpiresAt = session.ExpiresAt;

        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task ClearAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await context.TelegramConversationSessions
            .FirstOrDefaultAsync(item => item.TelegramUserId == telegramUserId, cancellationToken);
        if (session is null)
        {
            return;
        }

        context.TelegramConversationSessions.Remove(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static TelegramConversationSessionSnapshot ToSnapshot(TelegramConversationSessionEntity session) =>
        new(
            session.Id,
            session.TelegramUserId,
            session.State,
            session.CurrentCode,
            session.SelectedManufacturer,
            session.SelectedEquipmentType,
            session.SelectedDisplayContext,
            session.CandidateOptionsJson,
            session.LastPromptMessageId,
            session.CreatedAt,
            session.UpdatedAt,
            session.ExpiresAt);
}
