using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramAdapter : IEquipmentDiagnosticTelegramAdapter
{
    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly EquipmentDiagnosticTelegramMessageParser _parser;
    private readonly EquipmentDiagnosticTelegramResponseFormatter _formatter;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramUserAccessService _accessService;
    private readonly ITelegramUserStore _userStore;

    public EquipmentDiagnosticTelegramAdapter(
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options,
        ITelegramUserAccessService accessService,
        ITelegramUserStore userStore)
    {
        _botFacade = botFacade;
        _parser = parser;
        _formatter = formatter;
        _options = options;
        _accessService = accessService;
        _userStore = userStore;
    }

    public async Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
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
                update.ReceivedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);
            return Response(
                update.ChatId,
                _formatter.FormatPhoneSaved(_options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply);
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
            return Response(
                update.ChatId,
                _formatter.FormatValidation(parseResult.Errors, _options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.ValidationError,
                parseResult.Errors);
        }

        if (parseResult.Command is EquipmentDiagnosticTelegramCommand.Start or EquipmentDiagnosticTelegramCommand.Help)
        {
            return Response(
                update.ChatId,
                _formatter.FormatHelp(access.Role, access.User?.HasPhoneNumber == true, _options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply);
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
            return Response(
                update.ChatId,
                _formatter.FormatUnsupported(_options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Unsupported);
        }

        var diagnosis = await _botFacade.DiagnoseAsync(parseResult.DiagnosticRequest, cancellationToken);
        return Response(
            update.ChatId,
            access.UsesTechnicalResponse
                ? _formatter.Format(diagnosis, _options.MaxMessageLength)
                : _formatter.FormatConsumer(diagnosis, access.User?.HasPhoneNumber == true, _options.MaxMessageLength),
            EquipmentDiagnosticTelegramResponseKind.Reply,
            diagnosis.Warnings);
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
                "Command is not available.",
                EquipmentDiagnosticTelegramResponseKind.Unsupported);
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
            return Response(update.ChatId, "Admin command requires a numeric chat id.", EquipmentDiagnosticTelegramResponseKind.ValidationError);
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
            result.Message,
            result.Succeeded ? EquipmentDiagnosticTelegramResponseKind.Reply : EquipmentDiagnosticTelegramResponseKind.ValidationError);
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

    private static EquipmentDiagnosticTelegramResponse Response(
        long chatId,
        string text,
        EquipmentDiagnosticTelegramResponseKind responseKind,
        IReadOnlyList<string>? warnings = null) =>
        new(
            chatId,
            text,
            responseKind,
            ParseMode: null,
            DisableWebPagePreview: true,
            warnings ?? [],
            InternalDecisionTrace: null);
}
