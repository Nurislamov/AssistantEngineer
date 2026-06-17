using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;

public sealed class InMemoryTelegramConversationSessionStore : ITelegramConversationSessionStore
{
    private readonly ConcurrentDictionary<long, TelegramConversationSessionEntity> _sessions = new();
    private long _lastId;

    public Task<TelegramConversationSessionSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _sessions.TryGetValue(telegramUserId, out var session)
                ? ToSnapshot(session)
                : null);

    public Task<TelegramConversationSessionSnapshot> UpsertAsync(
        TelegramConversationSessionUpsert session,
        CancellationToken cancellationToken = default)
    {
        var now = session.UpdatedAt;
        var entity = _sessions.AddOrUpdate(
            session.TelegramUserId,
            _ => new TelegramConversationSessionEntity
            {
                Id = Interlocked.Increment(ref _lastId),
                TelegramUserId = session.TelegramUserId,
                CreatedAt = now
            },
            (_, existing) => existing);

        entity.State = session.State;
        entity.CurrentCode = session.CurrentCode;
        entity.SelectedManufacturer = session.SelectedManufacturer;
        entity.SelectedEquipmentType = session.SelectedEquipmentType;
        entity.SelectedDisplayContext = session.SelectedDisplayContext;
        entity.CandidateOptionsJson = session.CandidateOptionsJson;
        entity.LastPromptMessageId = session.LastPromptMessageId;
        entity.UpdatedAt = now;
        entity.ExpiresAt = session.ExpiresAt;

        return Task.FromResult(ToSnapshot(entity));
    }

    public Task ClearAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(telegramUserId, out _);
        return Task.CompletedTask;
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
