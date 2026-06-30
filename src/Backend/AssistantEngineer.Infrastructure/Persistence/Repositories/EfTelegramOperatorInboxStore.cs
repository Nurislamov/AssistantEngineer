using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramOperatorInboxStore : ITelegramOperatorInboxStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramOperatorInboxStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramOperatorInboxUserMessage> AddUserMessageAsync(
        TelegramUserAccessResult access,
        EquipmentDiagnosticTelegramUpdate update,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        var thread = await context.TelegramOperatorInboxThreads
            .Where(item =>
                item.TelegramChatId == update.ChatId &&
                item.Status == TelegramOperatorInboxThreadStatus.Open)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var isNewThread = thread is null;
        if (thread is null)
        {
            thread = new TelegramOperatorInboxThreadEntity
            {
                TelegramUserId = access.User?.Id,
                TelegramChatId = update.ChatId,
                UserDisplayName = DisplayName(update, access.User),
                Username = Normalize(update.Username ?? access.User?.Username),
                UserRole = access.Role.ToString(),
                Status = TelegramOperatorInboxThreadStatus.Open,
                CreatedAt = now,
                UpdatedAt = now
            };
            await context.TelegramOperatorInboxThreads.AddAsync(thread, cancellationToken);
        }

        thread.TelegramUserId = access.User?.Id ?? thread.TelegramUserId;
        thread.UserDisplayName = DisplayName(update, access.User) ?? thread.UserDisplayName;
        thread.Username = Normalize(update.Username ?? access.User?.Username) ?? thread.Username;
        thread.UserRole = access.Role.ToString();
        thread.LastUserMessageAt = now;
        thread.UpdatedAt = now;
        if (isNewThread)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var message = new TelegramOperatorInboxMessageEntity
        {
            ThreadId = thread.Id,
            Direction = TelegramOperatorInboxMessageDirection.UserToOperator,
            UserChatId = update.ChatId,
            UserMessageId = update.MessageId,
            MessageKind = kind,
            Text = Truncate(text, 4000),
            CreatedAt = now
        };
        await context.TelegramOperatorInboxMessages.AddAsync(message, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new TelegramOperatorInboxUserMessage(ToSnapshot(thread), ToSnapshot(message));
    }

    public async Task SetOperatorMessageAsync(
        long messageId,
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await context.TelegramOperatorInboxMessages
            .FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.OperatorChatId = operatorChatId;
        message.OperatorMessageId = operatorMessageId;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TelegramOperatorInboxMessageSnapshot> AddOperatorMirrorAsync(
        long threadId,
        long? userChatId,
        long? userMessageId,
        long operatorChatId,
        long operatorMessageId,
        TelegramOperatorInboxMessageKind kind,
        string? text,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = new TelegramOperatorInboxMessageEntity
        {
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
        await context.TelegramOperatorInboxMessages.AddAsync(message, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(message);
    }

    public async Task<TelegramOperatorInboxMessageSnapshot?> GetByOperatorMessageAsync(
        long operatorChatId,
        long operatorMessageId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await context.TelegramOperatorInboxMessages
            .AsNoTracking()
            .Where(item =>
                item.OperatorChatId == operatorChatId &&
                item.OperatorMessageId == operatorMessageId)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return message is null ? null : ToSnapshot(message);
    }

    public async Task<TelegramOperatorInboxThreadSnapshot?> GetThreadAsync(
        long threadId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var thread = await context.TelegramOperatorInboxThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == threadId, cancellationToken);
        return thread is null ? null : ToSnapshot(thread);
    }

    public async Task<TelegramOperatorInboxMessageSnapshot> AddOperatorReplyAsync(
        long threadId,
        long userChatId,
        long operatorChatId,
        long operatorMessageId,
        long operatorReplyToMessageId,
        TelegramOperatorInboxMessageKind kind,
        string text,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var thread = await context.TelegramOperatorInboxThreads
            .FirstAsync(item => item.Id == threadId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        thread.Status = TelegramOperatorInboxThreadStatus.Answered;
        thread.LastOwnerReplyAt = now;
        thread.UpdatedAt = now;

        var message = new TelegramOperatorInboxMessageEntity
        {
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
        await context.TelegramOperatorInboxMessages.AddAsync(message, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(message);
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
