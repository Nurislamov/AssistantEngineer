using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramAdapter : IEquipmentDiagnosticTelegramAdapter
{
    private const int TelegramTechnicalChunkLength = 3500;
    private const int ConsumerMessageLength = 900;

    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly EquipmentDiagnosticTelegramMessageParser _parser;
    private readonly EquipmentDiagnosticTelegramResponseFormatter _formatter;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramUserAccessService _accessService;
    private readonly ITelegramUserStore _userStore;
    private readonly TelegramDiagnosticConversationService? _conversationService;
    private readonly TelegramDiagnosticHistoryService? _historyService;
    private readonly TelegramServiceRequestService? _serviceRequestService;
    private readonly TelegramServiceRequestQueueService? _serviceRequestQueueService;

    public EquipmentDiagnosticTelegramAdapter(
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options,
        ITelegramUserAccessService accessService,
        ITelegramUserStore userStore,
        TelegramDiagnosticConversationService? conversationService = null,
        TelegramDiagnosticHistoryService? historyService = null,
        TelegramServiceRequestService? serviceRequestService = null,
        TelegramServiceRequestQueueService? serviceRequestQueueService = null)
    {
        _botFacade = botFacade;
        _parser = parser;
        _formatter = formatter;
        _options = options;
        _accessService = accessService;
        _userStore = userStore;
        _conversationService = conversationService;
        _historyService = historyService;
        _serviceRequestService = serviceRequestService;
        _serviceRequestQueueService = serviceRequestQueueService;
    }

    public async Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
        }

        if (!string.IsNullOrWhiteSpace(update.CallbackQueryId))
        {
            var result = _serviceRequestQueueService is null
                ? new TelegramServiceQueueCommandResult("Действие недоступно или устарело.")
                : await _serviceRequestQueueService.HandleCallbackAsync(update, cancellationToken);
            return Response(
                update.ChatId,
                result.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: result.ReplyMarkup);
        }

        if (TelegramServiceRequestQueueService.TryParse(update.Text, out var queueCommand))
        {
            var result = _serviceRequestQueueService is null
                ? new TelegramServiceQueueCommandResult("Команда доступна в сервисной группе.")
                : await _serviceRequestQueueService.HandleAsync(update, queueCommand, cancellationToken);
            return Response(
                update.ChatId,
                result.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: result.ReplyMarkup);
        }

        var access = await _accessService.ResolveAccessAsync(update, cancellationToken);
        if (!access.IsAllowed)
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
        }

        if (!string.IsNullOrWhiteSpace(update.ContactPhoneNumber))
        {
            var verified = update.ContactUserId is not null &&
                update.UserId is not null &&
                update.ContactUserId == update.UserId;
            await _userStore.SavePhoneAsync(
                update.ChatId,
                update.ContactPhoneNumber,
                verified,
                TelegramUserPhoneNumberSource.TelegramContact,
                update.ReceivedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);
            var updatedUser = await _userStore.GetByChatIdAsync(update.ChatId, cancellationToken) ?? access.User;
            var updatedAccess = updatedUser is null
                ? access
                : new TelegramUserAccessResult(access.IsAllowed, updatedUser, updatedUser.Role, access.DenialReason);
            var repeatedPrompt = _conversationService is not null && updatedAccess.User is not null
                ? await _conversationService.RepeatActivePromptAsync(updatedAccess.User, updatedAccess, cancellationToken)
                : null;
            if (repeatedPrompt is not null)
            {
                return Response(
                    update.ChatId,
                    string.Join("\n\n", _formatter.FormatPhoneSaved(_options.MaxMessageLength), repeatedPrompt.Text),
                    repeatedPrompt.ResponseKind,
                    repeatedPrompt.Warnings,
                    repeatedPrompt.ReplyMarkup,
                    repeatedPrompt.Messages);
            }

            return Response(
                update.ChatId,
                _formatter.FormatPhoneSaved(_options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(updatedAccess));
        }

        if (TryHandleMe(update, access, out var meResponse))
        {
            return meResponse;
        }

        var adminResponse = await TryHandleAdminAsync(update, access, cancellationToken);
        if (adminResponse is not null)
        {
            return adminResponse;
        }

        var parseResult = _parser.Parse(update.Text, _options);
        if (_options.EnableChatIdDiscovery &&
            parseResult.Command == EquipmentDiagnosticTelegramCommand.Identity)
        {
            return Response(
                update.ChatId,
                _formatter.FormatIdentity(update, _options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply);
        }

        if (parseResult.Errors.Count > 0)
        {
            var conversation = _conversationService is not null
                ? await _conversationService.HandleTextAsync(update, access, access.User!, cancellationToken)
                : new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, []);
            if (conversation.Handled)
            {
                return FromConversation(update.ChatId, conversation);
            }

            return Response(
                update.ChatId,
                _formatter.FormatValidation(parseResult.Errors, _options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.ValidationError,
                parseResult.Errors,
                replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
        }

        if (parseResult.Command is EquipmentDiagnosticTelegramCommand.Start or EquipmentDiagnosticTelegramCommand.Help)
        {
            var phoneSaved = access.User?.HasPhoneNumber == true;
            return Response(
                update.ChatId,
                _formatter.FormatHelp(access.Role, phoneSaved, Math.Max(_options.MaxMessageLength, ConsumerMessageLength)),
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.History)
        {
            return await FormatHistoryAsync(update, access, cancellationToken);
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.Last)
        {
            return await FormatLastAsync(update, access, cancellationToken);
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.Request)
        {
            return await CreateServiceRequestAsync(update, access, cancellationToken);
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.Requests)
        {
            return await FormatServiceRequestsAsync(update, access, cancellationToken);
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.Identity)
        {
            return _options.EnableChatIdDiscovery
                ? Response(
                    update.ChatId,
                    _formatter.FormatIdentity(update, _options.MaxMessageLength),
                    EquipmentDiagnosticTelegramResponseKind.Reply)
                : Response(
                    update.ChatId,
                    _formatter.FormatUnsupported(_options.MaxMessageLength),
                    EquipmentDiagnosticTelegramResponseKind.Unsupported);
        }

        if (parseResult.Command == EquipmentDiagnosticTelegramCommand.Unsupported ||
            parseResult.DiagnosticRequest is null)
        {
            var conversation = _conversationService is not null
                ? await _conversationService.HandleTextAsync(update, access, access.User!, cancellationToken)
                : new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, []);
            return conversation.Handled
                ? FromConversation(update.ChatId, conversation)
                : Response(
                    update.ChatId,
                    _formatter.FormatUnsupported(_options.MaxMessageLength),
                    EquipmentDiagnosticTelegramResponseKind.Unsupported,
                    replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
        }

        var conversationResponse = _conversationService is not null
            ? await _conversationService.HandleTextAsync(update, access, access.User!, cancellationToken)
            : new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, []);
        if (conversationResponse.Handled)
        {
            return FromConversation(update.ChatId, conversationResponse);
        }

        var diagnosis = await _botFacade.DiagnoseAsync(parseResult.DiagnosticRequest, cancellationToken);
        if (access.UsesTechnicalResponse)
        {
            var technical = _formatter.FormatTechnical(diagnosis);
            var messages = SplitTelegramMessage(technical)
                .Select(chunk => new EquipmentDiagnosticTelegramOutboundMessage(
                    chunk,
                    ReplyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access)))
                .ToArray();

            return Response(
                update.ChatId,
                messages[0].Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                diagnosis.Warnings,
                messages: messages);
        }

        var consumerPhoneSaved = access.User?.HasPhoneNumber == true;
        return Response(
            update.ChatId,
            _formatter.FormatConsumer(diagnosis, consumerPhoneSaved, ConsumerMessageLength),
            EquipmentDiagnosticTelegramResponseKind.Reply,
            diagnosis.Warnings,
            replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
    }

    private bool TryHandleMe(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        out EquipmentDiagnosticTelegramResponse response)
    {
        if (string.Equals(update.Text?.Trim(), "/me", StringComparison.OrdinalIgnoreCase))
        {
            response = Response(
                update.ChatId,
                _formatter.FormatMe(access.User, _options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply);
            return true;
        }

        response = null!;
        return false;
    }

    private async Task<EquipmentDiagnosticTelegramResponse?> TryHandleAdminAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var text = update.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) ||
            !text.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!access.CanUseAdminCommands)
        {
            return Response(
                update.ChatId,
                "Команда недоступна.",
                EquipmentDiagnosticTelegramResponseKind.Unsupported);
        }

        if (string.Equals(text, "/admin_help", StringComparison.OrdinalIgnoreCase))
        {
            return Response(update.ChatId, _formatter.FormatAdminHelp(_options.MaxMessageLength), EquipmentDiagnosticTelegramResponseKind.Reply);
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return Response(update.ChatId, _formatter.FormatAdminHelp(_options.MaxMessageLength), EquipmentDiagnosticTelegramResponseKind.Reply);
        }

        var command = parts[1].ToLowerInvariant();
        if (command == "users")
        {
            var users = await _userStore.ListUsersAsync(20, cancellationToken);
            return Response(update.ChatId, _formatter.FormatAdminUsers(users, _options.MaxMessageLength), EquipmentDiagnosticTelegramResponseKind.Reply);
        }

        if (!TryReadChatId(parts, 2, out var chatId))
        {
            return Response(update.ChatId, "Админ-команде нужен числовой chatId.", EquipmentDiagnosticTelegramResponseKind.ValidationError);
        }

        TelegramUserCommandResult result = command switch
        {
            "allow" => await _userStore.AllowAsync(chatId, TelegramUserRole.Consumer, cancellationToken),
            "block" => await _userStore.SetBlockedAsync(chatId, true, cancellationToken),
            "unblock" => await _userStore.SetBlockedAsync(chatId, false, cancellationToken),
            "disable" => await _userStore.SetEnabledAsync(chatId, false, cancellationToken),
            "enable" => await _userStore.SetEnabledAsync(chatId, true, cancellationToken),
            "role" when parts.Length >= 4 && TryReadRole(parts[3], out var role) =>
                await _userStore.SetRoleAsync(chatId, role, cancellationToken),
            _ => new TelegramUserCommandResult(false, "Unsupported admin command.")
        };

        return Response(
            update.ChatId,
            TranslateAdminCommandResult(command, chatId, result),
            result.Succeeded ? EquipmentDiagnosticTelegramResponseKind.Reply : EquipmentDiagnosticTelegramResponseKind.ValidationError);
    }

    private async Task<EquipmentDiagnosticTelegramResponse> FormatHistoryAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var text = access.User is null || _historyService is null
            ? "История пока пустая. Отправьте код ошибки, например: Gree H5."
            : await _historyService.FormatHistoryAsync(access.User, cancellationToken);

        return Response(
            update.ChatId,
            text,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
    }

    private async Task<EquipmentDiagnosticTelegramResponse> FormatLastAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var text = access.User is null || _historyService is null
            ? "История пока пустая. Отправьте код ошибки, например: Gree H5."
            : await _historyService.FormatLastAsync(access.User, cancellationToken);

        return Response(
            update.ChatId,
            text,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
    }

    private async Task<EquipmentDiagnosticTelegramResponse> CreateServiceRequestAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (_serviceRequestService is null)
        {
            return Response(
                update.ChatId,
                "Сначала отправьте код ошибки, например: Gree H5.",
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
        }

        var result = await _serviceRequestService.CreateFromLatestAsync(update, access, cancellationToken);
        var keyboard = result.Status switch
        {
            TelegramServiceRequestAttemptStatus.PhoneMissing =>
                TelegramDiagnosticConversationService.ServiceRequestPhoneKeyboard(),
            TelegramServiceRequestAttemptStatus.Created or TelegramServiceRequestAttemptStatus.Existing =>
                TelegramDiagnosticConversationService.ServiceRequestCreatedKeyboard(access),
            _ => TelegramDiagnosticConversationService.MainKeyboard(access)
        };
        return Response(
            update.ChatId,
            result.Text,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            replyMarkup: keyboard);
    }

    private async Task<EquipmentDiagnosticTelegramResponse> FormatServiceRequestsAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var text = access.User is null || _serviceRequestService is null
            ? "У вас пока нет сервисных заявок.\n\nОтправьте код ошибки, а после диагностики нажмите «Нужен мастер»."
            : await _serviceRequestService.FormatRequestsAsync(access.User, cancellationToken);
        return Response(
            update.ChatId,
            text,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access));
    }

    private static bool TryReadChatId(
        IReadOnlyList<string> parts,
        int index,
        out long chatId)
    {
        chatId = 0;
        return parts.Count > index &&
            long.TryParse(parts[index], out chatId);
    }

    private static bool TryReadRole(
        string value,
        out TelegramUserRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role);

    private static EquipmentDiagnosticTelegramResponse FromConversation(
        long chatId,
        TelegramDiagnosticConversationResult result) =>
        Response(
            chatId,
            result.Text,
            result.ResponseKind,
            result.Warnings,
            result.ReplyMarkup,
            result.Messages);

    private static IReadOnlyList<string> SplitTelegramMessage(string text)
    {
        if (text.Length <= TelegramTechnicalChunkLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var remaining = text;
        while (remaining.Length > TelegramTechnicalChunkLength)
        {
            var splitAt = remaining.LastIndexOf('\n', TelegramTechnicalChunkLength);
            if (splitAt < TelegramTechnicalChunkLength / 2)
            {
                splitAt = TelegramTechnicalChunkLength;
            }

            chunks.Add(remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].Trim();
        }

        if (remaining.Length > 0)
        {
            chunks.Add(remaining);
        }

        return chunks;
    }

    private static string TranslateAdminCommandResult(
        string command,
        long chatId,
        TelegramUserCommandResult result)
    {
        if (!result.Succeeded)
        {
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                result.Message.Contains("не найден", StringComparison.OrdinalIgnoreCase)
                ? "Пользователь не найден."
                : "Команда не выполнена.";
        }

        return command switch
        {
            "allow" => $"Пользователь {chatId} разрешен с ролью Пользователь.",
            "block" => "Пользователь заблокирован.",
            "unblock" => "Пользователь разблокирован.",
            "disable" => "Доступ пользователя выключен.",
            "enable" => "Доступ пользователя включен.",
            "role" => "Роль пользователя обновлена.",
            _ => "Команда выполнена."
        };
    }

    private static EquipmentDiagnosticTelegramResponse Response(
        long chatId,
        string text,
        EquipmentDiagnosticTelegramResponseKind responseKind,
        IReadOnlyList<string>? warnings = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? messages = null) =>
        new(
            chatId,
            text,
            responseKind,
            ParseMode: null,
            DisableWebPagePreview: true,
            warnings ?? [],
            InternalDecisionTrace: null,
            Messages: messages ??
                (replyMarkup is null
                    ? null
                    :
                    [
                        new EquipmentDiagnosticTelegramOutboundMessage(
                            text,
                            ParseMode: null,
                            DisableWebPagePreview: true,
                            replyMarkup)
                    ]));
}
