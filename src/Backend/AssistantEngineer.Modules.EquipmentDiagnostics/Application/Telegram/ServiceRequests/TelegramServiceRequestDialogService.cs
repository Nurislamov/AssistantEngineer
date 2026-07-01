using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed record TelegramServiceRequestDialogResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null,
    bool SuppressOutbound = false);

public sealed class TelegramServiceRequestDialogService
{
    private const string UnsupportedContentText =
        "Этот тип сообщения пока не поддерживается. Отправьте текст, фото, файл или видео.";
    private const string PendingAttachmentPrefix = "attachment:";
    public const string ReplyPrefix = "sr:reply:";
    public const string ThreadPrefix = "sr:thread:";
    public const string PickPrefix = "sr:pick:";
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromHours(12);
    private static readonly string[] Commands = ["/start", "/help", "/history", "/last", "/cancel"];

    private readonly ITelegramServiceRequestDialogStore _dialogStore;
    private readonly ITelegramServiceRequestStore _requestStore;
    private readonly ITelegramUserStore _userStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly TelegramServiceRequestEventService _eventService;

    public TelegramServiceRequestDialogService(
        ITelegramServiceRequestDialogStore dialogStore,
        ITelegramServiceRequestStore requestStore,
        ITelegramUserStore userStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOptions options,
        TelegramDisplayTimeFormatter timeFormatter,
        TelegramServiceRequestEventService eventService)
    {
        _dialogStore = dialogStore;
        _requestStore = requestStore;
        _userStore = userStore;
        _outboundClient = outboundClient;
        _options = options;
        _timeFormatter = timeFormatter;
        _eventService = eventService;
    }

    public static bool IsCallback(string? data) =>
        data?.StartsWith(ReplyPrefix, StringComparison.Ordinal) == true ||
        data?.StartsWith(ThreadPrefix, StringComparison.Ordinal) == true ||
        data?.StartsWith(PickPrefix, StringComparison.Ordinal) == true;

    public async Task<TelegramServiceRequestDialogResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseCallback(update.CallbackData, out var action, out var requestId))
        {
            return new("Действие недоступно или устарело.", CallbackAnswerText: "Действие недоступно", SuppressOutbound: true);
        }

        var actor = update.UserId is null
            ? null
            : await _userStore.GetByTelegramUserIdAsync(update.UserId.Value, cancellationToken);
        if (actor is null || !actor.IsEnabled || actor.IsBlocked)
        {
            return new("Действие недоступно.", CallbackAnswerText: "Нет доступа", SuppressOutbound: true);
        }

        if (action == "pick")
        {
            return await SelectUserRequestAsync(actor, requestId, cancellationToken);
        }

        if (_options.ServiceRequests.NotificationChatId is null ||
            update.ChatId != _options.ServiceRequests.NotificationChatId.Value)
        {
            return new("Действие доступно в сервисной группе.", CallbackAnswerText: "Нет доступа", SuppressOutbound: true);
        }

        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return new($"Заявка #{requestId} не найдена.", CallbackAnswerText: "Заявка не найдена");
        }

        if (!TelegramUserRolePolicy.CanUseServiceQueue(actor.Role))
        {
            await AuditDeniedAsync(requestId, actor.Id, cancellationToken);
            return new("Отвечать по заявке могут Owner, Admin или Engineer.", CallbackAnswerText: "Нет доступа", SuppressOutbound: true);
        }

        if (action == "thread")
        {
            return new(
                await FormatDialogAsync(request, cancellationToken),
                DialogKeyboard(requestId),
                "Диалог загружен");
        }

        var requester = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
        if (requester is null || !requester.IsEnabled || requester.IsBlocked)
        {
            return new("Пользователь заблокирован. Отправка невозможна.", CallbackAnswerText: "Отправка невозможна", SuppressOutbound: true);
        }

        var prompt = await _outboundClient.SendMessageAsync(
            actor.TelegramChatId,
            $"Ответ по заявке #{request.Id}.\n\nНапишите сообщение, которое нужно отправить пользователю.\n\nОтмена: /cancel",
            parseMode: null,
            disableWebPagePreview: true,
            cancellationToken: cancellationToken);
        if (!prompt.Succeeded)
        {
            return new(
                "Не удалось открыть режим ответа. Откройте бота в личке и нажмите /start.",
                CallbackAnswerText: "Личный чат недоступен",
                SuppressOutbound: true);
        }

        var now = DateTimeOffset.UtcNow;
        await _dialogStore.SetPendingAsync(
            actor.Id,
            TelegramServiceRequestPendingKind.OperatorReply,
            request.Id,
            null,
            now,
            now.Add(PendingLifetime),
            cancellationToken);
        return new(string.Empty, CallbackAnswerText: "Продолжите в личном чате", SuppressOutbound: true);
    }

    public async Task<TelegramServiceRequestDialogResult?> TryHandlePrivateMessageAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!IsPrivate(update.ChatType) || access.User is null)
        {
            return null;
        }
        if (!access.User.IsEnabled || access.User.IsBlocked)
        {
            return new(EquipmentDiagnosticTelegramAdapter.BlockedUserMessage);
        }

        var text = update.Text?.Trim();
        var attachment = AttachmentFrom(update);
        var pending = await _dialogStore.GetPendingAsync(access.User.Id, cancellationToken);
        if (string.Equals(text, "/cancel", StringComparison.OrdinalIgnoreCase) && pending is not null)
        {
            await _dialogStore.ClearPendingAsync(access.User.Id, cancellationToken);
            return new("Ответ по заявке отменён.");
        }

        if (IsCommand(text))
        {
            return null;
        }

        if (pending?.Kind == TelegramServiceRequestPendingKind.OperatorReply)
        {
            if (attachment is not null)
            {
                return await SendOperatorAttachmentAsync(update, access.User, pending, attachment, cancellationToken);
            }
            if (HasUnsupportedContent(update))
            {
                return new(UnsupportedContentText);
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                return new("Отправьте текстовое сообщение или используйте /cancel.");
            }
            return await SendOperatorTextAsync(update, access.User, pending, text, cancellationToken);
        }

        if (access.Role != TelegramUserRole.Consumer)
        {
            return null;
        }

        var candidates = await GetReplyCandidatesAsync(access.User.Id, cancellationToken);
        if (candidates.Count == 0)
        {
            return null;
        }
        if (attachment is null && HasUnsupportedContent(update))
        {
            return new(UnsupportedContentText);
        }
        if (attachment is null && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (candidates.Count == 1)
        {
            if (attachment is not null)
            {
                return await SendUserAttachmentAsync(update, access.User, candidates[0], attachment, cancellationToken);
            }
            return await SendUserTextAsync(update, access.User, candidates[0], text!, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var pendingPayload = attachment is null
            ? text
            : PendingAttachmentPrefix + JsonSerializer.Serialize(attachment);
        await _dialogStore.SetPendingAsync(
            access.User.Id,
            TelegramServiceRequestPendingKind.UserRequestSelection,
            null,
            pendingPayload,
            now,
            now.Add(PendingLifetime),
            cancellationToken);
        return new(
            "К какой заявке относится сообщение?",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                candidates.Select(item =>
                    (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                    [new($"#{item.Id} {Title(item)}", $"{PickPrefix}{item.Id}")]).ToArray()));
    }

    private async Task<TelegramServiceRequestDialogResult> SendOperatorTextAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot actor,
        TelegramServiceRequestPendingSnapshot pending,
        string text,
        CancellationToken cancellationToken)
    {
        var request = pending.ServiceRequestId is null
            ? null
            : await _requestStore.GetByIdAsync(pending.ServiceRequestId.Value, cancellationToken);
        if (request is null)
        {
            await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
            return new("Заявка не найдена. Режим ответа отменён.");
        }

        var requester = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
        if (requester is null || !requester.IsEnabled || requester.IsBlocked)
        {
            await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
            return new("Пользователь заблокирован. Отправка невозможна.");
        }

        var sent = await _outboundClient.SendMessageAsync(
            requester.TelegramChatId,
            $"💬 Ответ по заявке #{request.Id}\n\n{text}\n\nОтветьте на это сообщение здесь, если нужно дополнить заявку.",
            parseMode: null,
            disableWebPagePreview: true,
            cancellationToken: cancellationToken);
        if (!sent.Succeeded)
        {
            return new("Не удалось отправить ответ пользователю. Попробуйте ещё раз или используйте /cancel.");
        }

        await _dialogStore.AddMessageAsync(new(
            request.Id,
            TelegramServiceRequestMessageDirection.OperatorToUser,
            actor.Id,
            actor.Role,
            text,
            requester.TelegramChatId,
            sent.MessageId,
            DateTimeOffset.UtcNow), cancellationToken);
        await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
        await SendGroupTextAsync(
            $"💬 Ответ отправлен пользователю по заявке #{request.Id}\n\nИнженер: {UserLabel(actor)}\nТекст: {Preview(text)}",
            request.Id,
            cancellationToken);
        return new($"Ответ по заявке #{request.Id} отправлен пользователю.");
    }

    private async Task<TelegramServiceRequestDialogResult> SendUserTextAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot user,
        TelegramServiceRequestSnapshot request,
        string text,
        CancellationToken cancellationToken)
    {
        await _dialogStore.AddMessageAsync(new(
            request.Id,
            TelegramServiceRequestMessageDirection.UserToOperator,
            user.Id,
            user.Role,
            text,
            update.ChatId,
            update.MessageId,
            update.ReceivedAt ?? DateTimeOffset.UtcNow), cancellationToken);
        await SendGroupTextAsync($"💬 Ответ пользователя по заявке #{request.Id}\n\n{text}", request.Id, cancellationToken);
        return new($"Сообщение добавлено к заявке #{request.Id}.");
    }

    private async Task<TelegramServiceRequestDialogResult> SelectUserRequestAsync(
        TelegramUserSnapshot actor,
        long requestId,
        CancellationToken cancellationToken)
    {
        var pending = await _dialogStore.GetPendingAsync(actor.Id, cancellationToken);
        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (pending?.Kind != TelegramServiceRequestPendingKind.UserRequestSelection ||
            string.IsNullOrWhiteSpace(pending.PendingText) ||
            request is null ||
            request.TelegramUserId != actor.Id)
        {
            return new("Выбор устарел. Отправьте сообщение ещё раз.", CallbackAnswerText: "Выбор устарел", SuppressOutbound: true);
        }

        await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
        var syntheticUpdate = new EquipmentDiagnosticTelegramUpdate(0, actor.TelegramChatId, actor.Username, pending.PendingText);
        TelegramServiceRequestDialogResult result;
        if (pending.PendingText.StartsWith(PendingAttachmentPrefix, StringComparison.Ordinal))
        {
            var attachment = JsonSerializer.Deserialize<AttachmentPayload>(
                pending.PendingText[PendingAttachmentPrefix.Length..]);
            if (attachment is null)
            {
                return new("Выбор устарел. Отправьте вложение ещё раз.", CallbackAnswerText: "Выбор устарел", SuppressOutbound: true);
            }
            result = await SendUserAttachmentAsync(syntheticUpdate, actor, request, attachment, cancellationToken);
        }
        else
        {
            result = await SendUserTextAsync(syntheticUpdate, actor, request, pending.PendingText, cancellationToken);
        }
        return result with { CallbackAnswerText = "Сообщение отправлено" };
    }

    private async Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetReplyCandidatesAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        var requests = await _requestStore.GetLatestForTelegramUserAsync(telegramUserId, 20, cancellationToken);
        var result = new List<TelegramServiceRequestSnapshot>();
        foreach (var request in requests.Where(item =>
                     item.Status is TelegramServiceRequestStatus.New or TelegramServiceRequestStatus.InProgress))
        {
            if (await _dialogStore.HasOperatorReplyAsync(request.Id, cancellationToken))
            {
                result.Add(request);
            }
        }
        return result;
    }

    private async Task<string> FormatDialogAsync(
        TelegramServiceRequestSnapshot request,
        CancellationToken cancellationToken)
    {
        var messages = await _dialogStore.GetLatestMessagesAsync(request.Id, 15, cancellationToken);
        if (messages.Count == 0)
        {
            return $"📜 Диалог по заявке #{request.Id}\n\nСообщений пока нет.";
        }

        var builder = new StringBuilder($"📜 Диалог по заявке #{request.Id}\n");
        var index = 1;
        foreach (var message in messages)
        {
            builder.AppendLine();
            builder.AppendLine($"{index++}. {(message.Direction == TelegramServiceRequestMessageDirection.UserToOperator ? "Пользователь" : "Инженер")}, {_timeFormatter.FormatRelative(message.CreatedAt)}:");
            var attachments = await _dialogStore.GetAttachmentsAsync(message.Id, cancellationToken);
            if (attachments.Count > 0)
            {
                builder.Append($"📎 {AttachmentLabel(attachments[0].AttachmentType)}");
                if (!string.IsNullOrWhiteSpace(message.Text))
                {
                    builder.AppendLine();
                    builder.Append(Preview(message.Text, 500));
                }
            }
            else
            {
                builder.Append(Preview(message.Text, 500));
            }
        }
        return builder.ToString();
    }

    private async Task SendGroupTextAsync(string text, long requestId, CancellationToken cancellationToken)
    {
        if (_options.ServiceRequests.NotificationChatId is not { } chatId)
        {
            return;
        }
        await _outboundClient.SendMessageAsync(
            chatId,
            text,
            parseMode: null,
            disableWebPagePreview: true,
            DialogKeyboard(requestId),
            cancellationToken);
    }

    private async Task AuditDeniedAsync(long requestId, long actorId, CancellationToken cancellationToken) =>
        await _eventService.AppendSafeAsync(new(
            requestId,
            TelegramServiceRequestEventType.ActionDenied,
            actorId,
            null,
            null,
            null,
            false,
            null,
            "{\"action\":\"reply\",\"reason\":\"forbidden\"}",
            DateTimeOffset.UtcNow), cancellationToken);

    public static EquipmentDiagnosticTelegramReplyMarkup DialogKeyboard(long requestId) =>
        new(InlineKeyboard:
        [
            [
                new EquipmentDiagnosticTelegramInlineKeyboardButton("💬 Ответить", $"{ReplyPrefix}{requestId}"),
                new EquipmentDiagnosticTelegramInlineKeyboardButton("📜 Диалог", $"{ThreadPrefix}{requestId}")
            ]
        ]);

    private static bool TryParseCallback(string? data, out string action, out long requestId)
    {
        action = string.Empty;
        requestId = 0;
        if (string.IsNullOrWhiteSpace(data) || Encoding.UTF8.GetByteCount(data) > 64)
        {
            return false;
        }
        var prefix = data.StartsWith(ReplyPrefix, StringComparison.Ordinal) ? ReplyPrefix :
            data.StartsWith(ThreadPrefix, StringComparison.Ordinal) ? ThreadPrefix :
            data.StartsWith(PickPrefix, StringComparison.Ordinal) ? PickPrefix : null;
        if (prefix is null || !long.TryParse(data[prefix.Length..], out requestId) || requestId <= 0)
        {
            return false;
        }
        action = prefix == ReplyPrefix ? "reply" : prefix == ThreadPrefix ? "thread" : "pick";
        return true;
    }

    private static bool IsPrivate(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) || string.Equals(chatType, "private", StringComparison.OrdinalIgnoreCase);

    private static bool IsCommand(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        Commands.Any(command => string.Equals(text.Split(' ', 2)[0], command, StringComparison.OrdinalIgnoreCase));

    private static string UserLabel(TelegramUserSnapshot user) =>
        !string.IsNullOrWhiteSpace(user.Username)
            ? $"@{user.Username.Trim().TrimStart('@')}"
            : string.Join(" ", new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string Preview(string text, int maxLength = 240) =>
        text.Length <= maxLength ? text : $"{text[..maxLength]}…";

    private static string Title(TelegramServiceRequestSnapshot request) =>
        string.IsNullOrWhiteSpace(request.Manufacturer) ? request.Code : $"{request.Manufacturer} {request.Code}";

    private async Task<TelegramServiceRequestDialogResult> SendOperatorAttachmentAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot actor,
        TelegramServiceRequestPendingSnapshot pending,
        AttachmentPayload attachment,
        CancellationToken cancellationToken)
    {
        var request = pending.ServiceRequestId is null
            ? null
            : await _requestStore.GetByIdAsync(pending.ServiceRequestId.Value, cancellationToken);
        if (request is null)
        {
            await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
            return new("Заявка не найдена. Режим ответа отменён.");
        }
        var requester = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
        if (requester is null || !requester.IsEnabled || requester.IsBlocked)
        {
            await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
            return new("Пользователь заблокирован. Отправка невозможна.");
        }

        var caption = $"📎 Вложение по заявке #{request.Id}" +
            (string.IsNullOrWhiteSpace(attachment.Caption) ? string.Empty : $"\n\n{attachment.Caption}") +
            "\n\nОтветьте здесь, если нужно дополнить заявку.";
        var sent = await SendAttachmentAsync(
            requester.TelegramChatId,
            attachment,
            caption,
            replyMarkup: null,
            cancellationToken);
        if (!sent.Succeeded)
        {
            return new("Не удалось отправить вложение пользователю. Попробуйте ещё раз или используйте /cancel.");
        }

        var message = await _dialogStore.AddMessageAsync(new(
            request.Id,
            TelegramServiceRequestMessageDirection.OperatorToUser,
            actor.Id,
            actor.Role,
            attachment.Caption ?? string.Empty,
            requester.TelegramChatId,
            sent.MessageId,
            DateTimeOffset.UtcNow), cancellationToken);
        await SaveAttachmentAsync(message.Id, attachment, cancellationToken);
        await _dialogStore.ClearPendingAsync(actor.Id, cancellationToken);
        await SendGroupTextAsync(
            $"📎 Вложение отправлено пользователю по заявке #{request.Id}\n\nТип: {AttachmentLabel(attachment.Type)}" +
            (string.IsNullOrWhiteSpace(attachment.Caption) ? string.Empty : $"\nПодпись: {Preview(attachment.Caption)}"),
            request.Id,
            cancellationToken);
        return new($"Вложение по заявке #{request.Id} отправлено пользователю.");
    }

    private async Task<TelegramServiceRequestDialogResult> SendUserAttachmentAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot user,
        TelegramServiceRequestSnapshot request,
        AttachmentPayload attachment,
        CancellationToken cancellationToken)
    {
        if (_options.ServiceRequests.NotificationChatId is not { } groupChatId)
        {
            return new("Сервисная группа временно недоступна.");
        }

        var caption = $"📎 Вложение пользователя по заявке #{request.Id}" +
            (string.IsNullOrWhiteSpace(attachment.Caption) ? string.Empty : $"\n\n{attachment.Caption}");
        var sent = await SendAttachmentAsync(
            groupChatId,
            attachment,
            caption,
            DialogKeyboard(request.Id),
            cancellationToken);
        if (!sent.Succeeded)
        {
            return new("Не удалось передать вложение в сервисную группу. Попробуйте ещё раз.");
        }

        var message = await _dialogStore.AddMessageAsync(new(
            request.Id,
            TelegramServiceRequestMessageDirection.UserToOperator,
            user.Id,
            user.Role,
            attachment.Caption ?? string.Empty,
            update.ChatId,
            update.MessageId,
            update.ReceivedAt ?? DateTimeOffset.UtcNow), cancellationToken);
        await SaveAttachmentAsync(message.Id, attachment, cancellationToken);
        return new($"Вложение добавлено к заявке #{request.Id}.");
    }

    private Task<EquipmentDiagnosticTelegramOutboundResult> SendAttachmentAsync(
        long chatId,
        AttachmentPayload attachment,
        string caption,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup,
        CancellationToken cancellationToken) =>
        attachment.Type switch
        {
            TelegramServiceRequestAttachmentType.Photo => _outboundClient.SendPhotoAsync(
                chatId, attachment.FileId, caption, replyMarkup, protectContent: true, cancellationToken),
            TelegramServiceRequestAttachmentType.Document => _outboundClient.SendDocumentAsync(
                chatId, attachment.FileId, caption, replyMarkup, protectContent: true, cancellationToken),
            TelegramServiceRequestAttachmentType.Video => _outboundClient.SendVideoAsync(
                chatId, attachment.FileId, caption, replyMarkup, protectContent: true, cancellationToken),
            _ => Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(false, "Unsupported attachment."))
        };

    private async Task SaveAttachmentAsync(
        long messageId,
        AttachmentPayload attachment,
        CancellationToken cancellationToken) =>
        await _dialogStore.AddAttachmentAsync(new(
            messageId,
            attachment.Type,
            attachment.FileId,
            attachment.FileUniqueId,
            attachment.FileName,
            attachment.MimeType,
            attachment.FileSize,
            attachment.Width,
            attachment.Height,
            attachment.Duration,
            DateTimeOffset.UtcNow), cancellationToken);

    private static AttachmentPayload? AttachmentFrom(EquipmentDiagnosticTelegramUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.PhotoFileId))
        {
            return new(TelegramServiceRequestAttachmentType.Photo, update.PhotoFileId,
                update.PhotoFileUniqueId, null, null, update.PhotoFileSize,
                update.PhotoWidth, update.PhotoHeight, null, update.Text);
        }
        if (!string.IsNullOrWhiteSpace(update.DocumentFileId))
        {
            return new(TelegramServiceRequestAttachmentType.Document, update.DocumentFileId,
                update.DocumentFileUniqueId, update.DocumentFileName, update.DocumentMimeType,
                update.DocumentFileSize, null, null, null, update.Text);
        }
        if (!string.IsNullOrWhiteSpace(update.VideoFileId))
        {
            return new(TelegramServiceRequestAttachmentType.Video, update.VideoFileId,
                update.VideoFileUniqueId, update.VideoFileName, update.VideoMimeType,
                update.VideoFileSize, update.VideoWidth, update.VideoHeight, update.VideoDuration, update.Text);
        }
        return null;
    }

    private static bool HasUnsupportedContent(EquipmentDiagnosticTelegramUpdate update) =>
        update.HasVoice || update.HasVideoNote || update.HasAudio ||
        update.HasLocation || update.HasAnimation || update.HasSticker ||
        update.HasPoll || !string.IsNullOrWhiteSpace(update.ContactPhoneNumber);

    private static string AttachmentLabel(TelegramServiceRequestAttachmentType type) =>
        type switch
        {
            TelegramServiceRequestAttachmentType.Photo => "фото",
            TelegramServiceRequestAttachmentType.Document => "файл",
            TelegramServiceRequestAttachmentType.Video => "видео",
            _ => "вложение"
        };

    private sealed record AttachmentPayload(
        TelegramServiceRequestAttachmentType Type,
        string FileId,
        string? FileUniqueId,
        string? FileName,
        string? MimeType,
        long? FileSize,
        int? Width,
        int? Height,
        int? Duration,
        string? Caption);
}
