using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;

public sealed class TelegramOperatorInboxService : ITelegramOperatorInboxService
{
    private const string OperatorChatIdCommand = "/operator_chat_id";
    private const string ChatIdAliasCommand = "/chatid";

    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramOperatorInboxStore _store;
    private readonly ITelegramUserStore _userStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;

    public TelegramOperatorInboxService(
        EquipmentDiagnosticTelegramOptions options,
        ITelegramOperatorInboxStore store,
        ITelegramUserStore userStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient)
    {
        _options = options;
        _store = store;
        _userStore = userStore;
        _outboundClient = outboundClient;
    }

    public async Task<bool> TryHandleOperatorCommandAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!IsOperatorChatIdCommand(update.Text))
        {
            return false;
        }

        if (!IsGroup(update.ChatType))
        {
            await SendAsync(
                update.ChatId,
                "Команду нужно отправить в operator group chat.",
                cancellationToken);
            return true;
        }

        if (!await IsOwnerAsync(update.UserId, cancellationToken))
        {
            await SendAsync(update.ChatId, "Доступ ограничен.", cancellationToken);
            return true;
        }

        await SendAsync(
            update.ChatId,
            $"Operator chat id: {update.ChatId}\n\nДобавьте в окружение: TELEGRAM_OPERATOR_CHAT_ID={update.ChatId}",
            cancellationToken);
        return true;
    }

    public async Task<bool> TryHandleOperatorReplyAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!IsGroup(update.ChatType) ||
            !_options.OperatorInbox.Enabled ||
            update.ReplyToMessageId is null ||
            _options.OperatorInbox.ChatId is null ||
            update.ChatId != _options.OperatorInbox.ChatId.Value)
        {
            return false;
        }

        if (!await IsOwnerAsync(update.UserId, cancellationToken))
        {
            await SendAsync(update.ChatId, "Доступ ограничен.", cancellationToken);
            return true;
        }

        if (string.IsNullOrWhiteSpace(update.Text))
        {
            await SendAsync(update.ChatId, "Пока поддерживается только текстовый ответ.", cancellationToken);
            return true;
        }

        var source = await _store.GetByOperatorMessageAsync(
            update.ChatId,
            update.ReplyToMessageId.Value,
            cancellationToken);
        if (source is null)
        {
            await SendAsync(
                update.ChatId,
                "Не удалось определить получателя. Ответьте reply-сообщением на карточку обращения.",
                cancellationToken);
            return true;
        }

        var thread = await _store.GetThreadAsync(source.ThreadId, cancellationToken);
        if (thread is null)
        {
            await SendAsync(
                update.ChatId,
                "Не удалось определить получателя. Ответьте reply-сообщением на карточку обращения.",
                cancellationToken);
            return true;
        }

        var userSend = await _outboundClient.SendMessageAsync(
            thread.TelegramChatId,
            $"Ответ специалиста:\n{update.Text.Trim()}",
            parseMode: null,
            disableWebPagePreview: true,
            replyMarkup: null,
            cancellationToken: cancellationToken);
        if (!userSend.Succeeded)
        {
            await SendAsync(update.ChatId, "Не удалось отправить ответ пользователю.", cancellationToken);
            return true;
        }

        await _store.AddOperatorReplyAsync(
            thread.Id,
            thread.TelegramChatId,
            update.ChatId,
            update.MessageId ?? 0,
            update.ReplyToMessageId.Value,
            update.Text.Trim(),
            cancellationToken);

        await SendAsync(update.ChatId, "Ответ отправлен пользователю.", cancellationToken);
        return true;
    }

    public async Task<bool> MirrorUserMessageAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.OperatorInbox.Enabled ||
            _options.OperatorInbox.ChatId is null ||
            !IsPrivate(update.ChatType) ||
            access.User is null)
        {
            return false;
        }

        var kind = MessageKind(update);
        var userMessage = await _store.AddUserMessageAsync(
            access,
            update,
            kind,
            update.Text,
            cancellationToken);

        var card = BuildCard(userMessage.Thread, update, access, kind, attachmentCopyFailed: false);
        var sent = await _outboundClient.SendMessageAsync(
            _options.OperatorInbox.ChatId.Value,
            card,
            parseMode: null,
            disableWebPagePreview: true,
            replyMarkup: null,
            cancellationToken: cancellationToken);
        if (sent.Succeeded && sent.MessageId is not null)
        {
            await _store.SetOperatorMessageAsync(
                userMessage.Message.Id,
                _options.OperatorInbox.ChatId.Value,
                sent.MessageId.Value,
                cancellationToken);
        }

        if (kind is TelegramOperatorInboxMessageKind.Text or TelegramOperatorInboxMessageKind.Unknown ||
            update.MessageId is null)
        {
            return sent.Succeeded;
        }

        var copied = await _outboundClient.CopyMessageAsync(
            _options.OperatorInbox.ChatId.Value,
            update.ChatId,
            update.MessageId.Value,
            cancellationToken);
        if (copied.Succeeded && copied.MessageId is not null)
        {
            await _store.AddOperatorMirrorAsync(
                userMessage.Thread.Id,
                update.ChatId,
                update.MessageId,
                _options.OperatorInbox.ChatId.Value,
                copied.MessageId.Value,
                kind,
                "Вложение скопировано в operator group.",
                cancellationToken);
            return true;
        }

        var copyFailureNotice = await _outboundClient.SendMessageAsync(
            _options.OperatorInbox.ChatId.Value,
            BuildCard(userMessage.Thread, update, access, kind, attachmentCopyFailed: true),
            parseMode: null,
            disableWebPagePreview: true,
            replyMarkup: null,
            cancellationToken: cancellationToken);
        return sent.Succeeded || copyFailureNotice.Succeeded;
    }

    private async Task<bool> IsOwnerAsync(
        long? telegramUserId,
        CancellationToken cancellationToken)
    {
        if (telegramUserId is null)
        {
            return false;
        }

        var user = await _userStore.GetByTelegramUserIdAsync(telegramUserId.Value, cancellationToken);
        return user is { IsEnabled: true, IsBlocked: false, Role: TelegramUserRole.Owner };
    }

    private Task SendAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken) =>
        _outboundClient.SendMessageAsync(
            chatId,
            text,
            parseMode: null,
            disableWebPagePreview: true,
            replyMarkup: null,
            cancellationToken: cancellationToken);

    private static string BuildCard(
        TelegramOperatorInboxThreadSnapshot thread,
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramOperatorInboxMessageKind kind,
        bool attachmentCopyFailed)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"📩 Обращение #{thread.Id}");
        builder.AppendLine();
        builder.AppendLine($"Пользователь: {DisplayName(update, thread)}{UsernameSuffix(update, thread)}");
        builder.AppendLine($"Роль: {access.Role}");
        builder.AppendLine($"ChatId: {update.ChatId}");
        builder.AppendLine($"Доступ к библиотеке: {(access.CanAccessLibrary ? "да" : "нет")}");
        builder.AppendLine($"Время: {FormatTime(update.ReceivedAt ?? DateTimeOffset.UtcNow)}");
        builder.AppendLine();
        builder.AppendLine("Сообщение:");
        builder.AppendLine(string.IsNullOrWhiteSpace(update.Text) ? $"[{MessageKindLabel(kind)}]" : Truncate(update.Text.Trim(), 1800));
        if (attachmentCopyFailed)
        {
            builder.AppendLine();
            builder.AppendLine("Вложение не удалось скопировать.");
        }

        return builder.ToString().Trim();
    }

    private static TelegramOperatorInboxMessageKind MessageKind(EquipmentDiagnosticTelegramUpdate update)
    {
        if (update.HasPhoto)
        {
            return TelegramOperatorInboxMessageKind.Photo;
        }

        if (update.HasVideoNote)
        {
            return TelegramOperatorInboxMessageKind.VideoNote;
        }

        if (update.HasVideo)
        {
            return TelegramOperatorInboxMessageKind.Video;
        }

        if (update.HasVoice)
        {
            return TelegramOperatorInboxMessageKind.Voice;
        }

        if (!string.IsNullOrWhiteSpace(update.DocumentFileId))
        {
            return TelegramOperatorInboxMessageKind.Document;
        }

        return string.IsNullOrWhiteSpace(update.Text)
            ? TelegramOperatorInboxMessageKind.Unknown
            : TelegramOperatorInboxMessageKind.Text;
    }

    private static string MessageKindLabel(TelegramOperatorInboxMessageKind kind) =>
        kind == TelegramOperatorInboxMessageKind.VideoNote
            ? "Видео-кружок"
            : kind.ToString();

    private static bool IsOperatorChatIdCommand(string? text)
    {
        var normalized = text?.Trim();
        return string.Equals(normalized, OperatorChatIdCommand, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, ChatIdAliasCommand, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGroup(string? chatType) =>
        string.Equals(chatType, "group", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(chatType, "supergroup", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrivate(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) ||
        string.Equals(chatType, "private", StringComparison.OrdinalIgnoreCase);

    private static string DisplayName(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramOperatorInboxThreadSnapshot thread)
    {
        var parts = new[] { update.FirstName, update.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();
        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        if (!string.IsNullOrWhiteSpace(thread.UserDisplayName))
        {
            return thread.UserDisplayName;
        }

        return "Пользователь без имени";
    }

    private static string UsernameSuffix(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramOperatorInboxThreadSnapshot thread)
    {
        var username = update.Username ?? thread.Username;
        return string.IsNullOrWhiteSpace(username)
            ? string.Empty
            : $" (@{username.Trim().TrimStart('@')})";
    }

    private static string FormatTime(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
