using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;

public sealed class InMemoryTelegramOperatorInboxStore : ITelegramOperatorInboxStore
{
    private readonly object _sync = new();
    private readonly List<TelegramOperatorInboxThreadEntity> _threads = [];
    private readonly List<TelegramOperatorInboxMessageEntity> _messages = [];
    private long _lastThreadId;
    private long _lastMessageId;

    public Task<TelegramOperatorInboxUserMessage> AddUserMessageAsync(
        TelegramUserAccessResult access,
        EquipmentDiagnosticTelegramUpdate update,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
            var thread = _threads
                .Where(item =>
                    item.TelegramChatId == update.ChatId &&
                    item.Status == TelegramOperatorInboxThreadStatus.Open)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
            if (thread is null)
            {
                thread = new TelegramOperatorInboxThreadEntity
                {
                    Id = ++_lastThreadId,
                    TelegramUserId = access.User?.Id,
                    TelegramChatId = update.ChatId,
                    UserDisplayName = DisplayName(update, access.User),
                    Username = Normalize(update.Username ?? access.User?.Username),
                    UserRole = access.Role.ToString(),
                    Status = TelegramOperatorInboxThreadStatus.Open,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _threads.Add(thread);
            }

            thread.TelegramUserId = access.User?.Id ?? thread.TelegramUserId;
            thread.UserDisplayName = DisplayName(update, access.User) ?? thread.UserDisplayName;
            thread.Username = Normalize(update.Username ?? access.User?.Username) ?? thread.Username;
            thread.UserRole = access.Role.ToString();
            thread.UpdatedAt = now;
            thread.LastUserMessageAt = now;

            var message = new TelegramOperatorInboxMessageEntity
            {
                Id = ++_lastMessageId,
                ThreadId = thread.Id,
                Direction = TelegramOperatorInboxMessageDirection.UserToOperator,
                UserChatId = update.ChatId,
                UserMessageId = update.MessageId,
                MessageKind = kind,
                Text = Truncate(text, 4000),
                CreatedAt = now
            };
            _messages.Add(message);
            return Task.FromResult(new TelegramOperatorInboxUserMessage(ToSnapshot(thread), ToSnapshot(message)));
        }
    }

    public Task SetOperatorMessageAsync(
        long messageId,
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var message = _messages.FirstOrDefault(item => item.Id == messageId);
            if (message is not null)
            {
                message.OperatorChatId = operatorChatId;
                message.OperatorMessageId = operatorMessageId;
            }
        }

        return Task.CompletedTask;
    }

    public Task<TelegramOperatorInboxMessageSnapshot> AddOperatorMirrorAsync(
        long threadId,
        long? userChatId,
        long? userMessageId,
        long operatorChatId,
        long operatorMessageId,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var message = new TelegramOperatorInboxMessageEntity
            {
                Id = ++_lastMessageId,
                ThreadId = threadId,
                Direction = TelegramOperatorInboxMessageDirection.System,
                UserChatId = userChatId,
                UserMessageId = userMessageId,
                OperatorChatId = operatorChatId,
                OperatorMessageId = operatorMessageId,
                MessageKind = kind,
                Text = Truncate(text, 4000),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _messages.Add(message);
            return Task.FromResult(ToSnapshot(message));
        }
    }

    public Task<TelegramOperatorInboxMessageSnapshot?> GetByOperatorMessageAsync(
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var message = _messages
                .Where(item =>
                    item.OperatorChatId == operatorChatId &&
                    item.OperatorMessageId == operatorMessageId)
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();
            return Task.FromResult(message is null ? null : ToSnapshot(message));
        }
    }

    public Task<TelegramOperatorInboxThreadSnapshot?> GetThreadAsync(
        long threadId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var thread = _threads.FirstOrDefault(item => item.Id == threadId);
            return Task.FromResult(thread is null ? null : ToSnapshot(thread));
        }
    }

    public Task<TelegramOperatorInboxMessageSnapshot> AddOperatorReplyAsync(
        long threadId,
        long userChatId,
        long operatorChatId,
        long operatorMessageId,
        long operatorReplyToMessageId,
        TelegramOperatorInboxMessageKind kind,
        string text,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            var thread = _threads.First(item => item.Id == threadId);
            thread.Status = TelegramOperatorInboxThreadStatus.Answered;
            thread.LastOwnerReplyAt = now;
            thread.UpdatedAt = now;

            var message = new TelegramOperatorInboxMessageEntity
            {
                Id = ++_lastMessageId,
                ThreadId = threadId,
                Direction = TelegramOperatorInboxMessageDirection.OperatorToUser,
                UserChatId = userChatId,
                OperatorChatId = operatorChatId,
                OperatorMessageId = operatorMessageId,
                OperatorReplyToMessageId = operatorReplyToMessageId,
                MessageKind = kind,
                Text = Truncate(text, 4000),
                CreatedAt = now
            };
            _messages.Add(message);
            return Task.FromResult(ToSnapshot(message));
        }
    }

    private static TelegramOperatorInboxThreadSnapshot ToSnapshot(TelegramOperatorInboxThreadEntity entity) =>
        new(
            entity.Id,
            entity.TelegramUserId,
            entity.TelegramChatId,
            entity.UserDisplayName,
            entity.Username,
            entity.UserRole,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastUserMessageAt,
            entity.LastOwnerReplyAt);

    private static TelegramOperatorInboxMessageSnapshot ToSnapshot(TelegramOperatorInboxMessageEntity entity) =>
        new(
            entity.Id,
            entity.ThreadId,
            entity.Direction,
            entity.UserChatId,
            entity.UserMessageId,
            entity.OperatorChatId,
            entity.OperatorMessageId,
            entity.OperatorReplyToMessageId,
            entity.MessageKind,
            entity.Text,
            entity.CreatedAt);

    private static string? DisplayName(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot? user)
    {
        var parts = new[] { update.FirstName ?? user?.FirstName, update.LastName ?? user?.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();
        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        return Normalize(update.Username ?? user?.Username);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimStart('@');

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength
                ? value
                : value[..maxLength];
}
