using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class InMemoryTelegramServiceRequestDialogStore : ITelegramServiceRequestDialogStore
{
    private readonly object _sync = new();
    private readonly List<TelegramServiceRequestMessageSnapshot> _messages = [];
    private readonly List<TelegramServiceRequestMessageAttachmentSnapshot> _attachments = [];
    private readonly ConcurrentDictionary<long, TelegramServiceRequestPendingSnapshot> _pending = new();
    private long _messageId;
    private long _pendingId;
    private long _attachmentId;

    public Task<TelegramServiceRequestMessageSnapshot> AddMessageAsync(
        TelegramServiceRequestMessageCreate message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var snapshot = new TelegramServiceRequestMessageSnapshot(
                ++_messageId,
                message.ServiceRequestId,
                message.Direction,
                message.SenderTelegramUserId,
                message.SenderRole,
                message.Text,
                message.TelegramChatId,
                message.TelegramMessageId,
                message.CreatedAt);
            _messages.Add(snapshot);
            return Task.FromResult(snapshot);
        }
    }

    public Task<IReadOnlyList<TelegramServiceRequestMessageSnapshot>> GetLatestMessagesAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TelegramServiceRequestMessageSnapshot>>(
                _messages
                    .Where(item => item.ServiceRequestId == serviceRequestId)
                    .OrderByDescending(item => item.CreatedAt)
                    .ThenByDescending(item => item.Id)
                    .Take(Math.Clamp(limit, 1, 50))
                    .Reverse()
                    .ToArray());
        }
    }

    public Task<bool> HasOperatorReplyAsync(
        long serviceRequestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(_messages.Any(item =>
                item.ServiceRequestId == serviceRequestId &&
                item.Direction == TelegramServiceRequestMessageDirection.OperatorToUser));
        }
    }

    public Task<TelegramServiceRequestMessageAttachmentSnapshot> AddAttachmentAsync(
        TelegramServiceRequestMessageAttachmentCreate attachment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var snapshot = new TelegramServiceRequestMessageAttachmentSnapshot(
                ++_attachmentId,
                attachment.MessageId,
                attachment.AttachmentType,
                attachment.FileId,
                attachment.FileUniqueId,
                attachment.FileName,
                attachment.MimeType,
                attachment.FileSize,
                attachment.Width,
                attachment.Height,
                attachment.Duration,
                attachment.CreatedAt);
            _attachments.Add(snapshot);
            return Task.FromResult(snapshot);
        }
    }

    public Task<IReadOnlyList<TelegramServiceRequestMessageAttachmentSnapshot>> GetAttachmentsAsync(
        long messageId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TelegramServiceRequestMessageAttachmentSnapshot>>(
                _attachments.Where(item => item.MessageId == messageId).OrderBy(item => item.Id).ToArray());
        }
    }

    public Task<TelegramServiceRequestPendingSnapshot?> GetPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pending.TryGetValue(telegramUserId, out var pending);
        if (pending is not null && pending.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _pending.TryRemove(telegramUserId, out _);
            pending = null;
        }
        return Task.FromResult(pending);
    }

    public Task<TelegramServiceRequestPendingSnapshot> SetPendingAsync(
        long telegramUserId,
        TelegramServiceRequestPendingKind kind,
        long? serviceRequestId,
        string? pendingText,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = new TelegramServiceRequestPendingSnapshot(
            Interlocked.Increment(ref _pendingId),
            telegramUserId,
            kind,
            serviceRequestId,
            pendingText,
            createdAt,
            expiresAt);
        _pending[telegramUserId] = snapshot;
        return Task.FromResult(snapshot);
    }

    public Task ClearPendingAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pending.TryRemove(telegramUserId, out _);
        return Task.CompletedTask;
    }
}
