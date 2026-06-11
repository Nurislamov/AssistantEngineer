using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramAdapter : IEquipmentDiagnosticTelegramAdapter
{
    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly EquipmentDiagnosticTelegramMessageParser _parser;
    private readonly EquipmentDiagnosticTelegramResponseFormatter _formatter;
    private readonly EquipmentDiagnosticTelegramOptions _options;

    public EquipmentDiagnosticTelegramAdapter(
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options)
    {
        _botFacade = botFacade;
        _parser = parser;
        _formatter = formatter;
        _options = options;
    }

    public async Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
        }

        if (!IsAllowed(update))
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
        }

        var parseResult = _parser.Parse(update.Text, _options);
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
                _formatter.FormatHelp(_options.MaxMessageLength),
                EquipmentDiagnosticTelegramResponseKind.Reply);
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
            _formatter.Format(diagnosis, _options.MaxMessageLength),
            EquipmentDiagnosticTelegramResponseKind.Reply,
            diagnosis.Warnings);
    }

    private bool IsAllowed(EquipmentDiagnosticTelegramUpdate update)
    {
        if (_options.AllowedChatIds.Count > 0 && !_options.AllowedChatIds.Contains(update.ChatId))
        {
            return false;
        }

        return _options.AllowedUsernames.Count == 0 ||
               update.Username is not null &&
               _options.AllowedUsernames.Contains(update.Username, StringComparer.OrdinalIgnoreCase);
    }

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
