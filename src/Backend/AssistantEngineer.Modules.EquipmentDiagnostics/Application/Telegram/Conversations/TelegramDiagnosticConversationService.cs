using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;

public sealed class TelegramDiagnosticConversationService
{
    public const string NewCodeButton = "🔎 Новый код";
    public const string SharePhoneButton = "📞 Поделиться номером";
    public const string UnknownButton = "Не знаю";

    private const int TelegramTechnicalChunkLength = 3500;
    private const int ConsumerMessageLength = 900;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITelegramConversationSessionStore _sessionStore;
    private readonly IEquipmentDiagnosticsService _diagnosticsService;
    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly EquipmentDiagnosticTelegramMessageParser _parser;
    private readonly EquipmentDiagnosticTelegramResponseFormatter _formatter;
    private readonly EquipmentDiagnosticTelegramOptions _options;

    public TelegramDiagnosticConversationService(
        ITelegramConversationSessionStore sessionStore,
        IEquipmentDiagnosticsService diagnosticsService,
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options)
    {
        _sessionStore = sessionStore;
        _diagnosticsService = diagnosticsService;
        _botFacade = botFacade;
        _parser = parser;
        _formatter = formatter;
        _options = options;
    }

    public static bool IsResetText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "новыйкод" or "/new" or "/reset" or "/cancel";
    }

    public async Task<TelegramDiagnosticConversationResult> ResetAsync(
        TelegramUserSnapshot user,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        await _sessionStore.ClearAsync(user.Id, cancellationToken);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Введите код ошибки, например: Gree H5.",
            [],
            MainKeyboard(access));
    }

    public async Task<TelegramDiagnosticConversationResult?> RepeatActivePromptAsync(
        TelegramUserSnapshot user,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        if (session is null || session.State is TelegramConversationState.Idle or TelegramConversationState.ShowingResult)
        {
            return null;
        }

        var candidates = ReadCandidates(session.CandidateOptionsJson);
        return PromptForState(session, candidates, access);
    }

    public async Task<TelegramDiagnosticConversationResult> HandleTextAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        if (IsResetText(update.Text))
        {
            return await ResetAsync(user, access, cancellationToken);
        }

        if (_parser.TryExtractDiagnosticCode(update.Text, out var code))
        {
            return await StartFromCodeAsync(update, access, user, code, cancellationToken);
        }

        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        if (session is null)
        {
            return new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, []);
        }

        return session.State switch
        {
            TelegramConversationState.WaitingForBrand =>
                await SelectBrandAsync(session, access, update.Text, cancellationToken),
            TelegramConversationState.WaitingForEquipmentType =>
                await SelectEquipmentTypeAsync(session, access, update.Text, cancellationToken),
            TelegramConversationState.WaitingForDisplayContext =>
                await SelectDisplayContextAsync(session, access, update.Text, cancellationToken),
            _ => new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, [])
        };
    }

    private async Task<TelegramDiagnosticConversationResult> StartFromCodeAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramUserSnapshot user,
        string code,
        CancellationToken cancellationToken)
    {
        var candidates = await FindCandidatesByCodeAsync(code, cancellationToken);
        if (candidates.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(_options.DefaultManufacturer) &&
                EquipmentDiagnosticBotReferencePolicy.IsReferenceOnlyCode(code))
            {
                var diagnosis = await _botFacade.DiagnoseAsync(
                    new EquipmentDiagnosticBotRequest(
                        _options.DefaultManufacturer,
                        code,
                        FreeText: update.Text,
                        PreferredLanguage: _options.PreferredLanguage),
                    cancellationToken);
                return FormatDiagnosis(diagnosis, access);
            }

            await _sessionStore.ClearAsync(user.Id, cancellationToken);
            return NotFound(access);
        }

        var selectedManufacturer = FindMentionedManufacturer(update.Text, candidates) ??
            SingleOrNull(candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase));

        return await ContinueAsync(
            user.Id,
            access,
            code,
            candidates,
            selectedManufacturer,
            selectedEquipmentType: null,
            selectedDisplayContext: null,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectBrandAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchOption(text, candidates.Select(candidate => candidate.Manufacturer));
        if (selected is null)
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            selected,
            session.SelectedEquipmentType,
            session.SelectedDisplayContext,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectEquipmentTypeAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchEquipmentType(text);
        if (selected is null && !IsUnknown(text))
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            session.SelectedManufacturer,
            selected,
            session.SelectedDisplayContext,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectDisplayContextAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchDisplayContext(text);
        if (selected is null && !IsUnknown(text))
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            session.SelectedManufacturer,
            session.SelectedEquipmentType,
            selected,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> ContinueAsync(
        long telegramUserId,
        TelegramUserAccessResult access,
        string code,
        IReadOnlyList<TelegramDiagnosticCandidate> allCandidates,
        string? selectedManufacturer,
        string? selectedEquipmentType,
        string? selectedDisplayContext,
        CancellationToken cancellationToken)
    {
        var candidates = allCandidates;
        if (!string.IsNullOrWhiteSpace(selectedManufacturer))
        {
            candidates = candidates
                .Where(candidate => string.Equals(candidate.Manufacturer, selectedManufacturer, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(selectedEquipmentType))
        {
            candidates = candidates
                .Where(candidate => string.Equals(candidate.EquipmentType, selectedEquipmentType, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(selectedDisplayContext))
        {
            candidates = candidates
                .Where(candidate => string.Equals(DisplayContextLabel(candidate.DisplayContext), selectedDisplayContext, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (candidates.Count == 0)
        {
            return NotFound(access);
        }

        if (string.IsNullOrWhiteSpace(selectedManufacturer))
        {
            var manufacturers = candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (manufacturers.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForBrand, code, allCandidates, null, null, null, cancellationToken);
                return PromptBrand(manufacturers, access);
            }

            selectedManufacturer = manufacturers[0];
        }

        if (string.IsNullOrWhiteSpace(selectedEquipmentType))
        {
            var equipmentTypes = candidates.Select(candidate => candidate.EquipmentType).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (equipmentTypes.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForEquipmentType, code, allCandidates, selectedManufacturer, null, null, cancellationToken);
                return PromptEquipmentType(equipmentTypes, access);
            }

            selectedEquipmentType = equipmentTypes[0];
        }

        if (string.IsNullOrWhiteSpace(selectedDisplayContext))
        {
            var displayContexts = candidates.Select(candidate => DisplayContextLabel(candidate.DisplayContext)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (displayContexts.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForDisplayContext, code, allCandidates, selectedManufacturer, selectedEquipmentType, null, cancellationToken);
                return PromptDisplayContext(displayContexts, access);
            }

            selectedDisplayContext = displayContexts[0];
        }

        await SaveAsync(
            telegramUserId,
            TelegramConversationState.ShowingResult,
            code,
            allCandidates,
            selectedManufacturer,
            selectedEquipmentType,
            selectedDisplayContext,
            cancellationToken);

        var finalCandidate = candidates[0];
        var diagnosis = await _botFacade.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest(
                selectedManufacturer,
                code,
                FreeText: null,
                Series: finalCandidate.Series,
                ModelCode: finalCandidate.ModelCode,
                Category: finalCandidate.Category,
                EquipmentSide: finalCandidate.EquipmentSide,
                DisplayContext: finalCandidate.DisplayContext,
                PreferredLanguage: _options.PreferredLanguage),
            cancellationToken);

        if (diagnosis.Status == EquipmentDiagnosticBotResponseStatus.NotFound)
        {
            return NotFound(access);
        }

        return FormatDiagnosis(diagnosis, access);
    }

    private async Task<IReadOnlyList<TelegramDiagnosticCandidate>> FindCandidatesByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var matches = await _diagnosticsService.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: null,
                ErrorCode: code,
                Series: null,
                ModelCode: null,
                Category: null),
            cancellationToken);

        return matches
            .Select(match => new TelegramDiagnosticCandidate(
                match.Manufacturer,
                match.SeriesName,
                match.ModelCode,
                match.Code,
                match.Category,
                EquipmentTypeLabel(match.Category),
                EquipmentSide(match.Category),
                DisplayContext(match.Category)))
            .Distinct()
            .OrderBy(candidate => candidate.Manufacturer, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.EquipmentType, StringComparer.Ordinal)
            .ThenBy(candidate => DisplayContextLabel(candidate.DisplayContext), StringComparer.Ordinal)
            .ToArray();
    }

    private async Task SaveAsync(
        long telegramUserId,
        TelegramConversationState state,
        string code,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        string? selectedManufacturer,
        string? selectedEquipmentType,
        string? selectedDisplayContext,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _sessionStore.UpsertAsync(
            new TelegramConversationSessionUpsert(
                telegramUserId,
                state,
                code,
                selectedManufacturer,
                selectedEquipmentType,
                selectedDisplayContext,
                JsonSerializer.Serialize(candidates, JsonOptions),
                LastPromptMessageId: null,
                now,
                now.Add(SessionTtl)),
            cancellationToken);
    }

    private TelegramDiagnosticConversationResult FormatDiagnosis(
        EquipmentDiagnosticBotResponse diagnosis,
        TelegramUserAccessResult access)
    {
        if (access.UsesTechnicalResponse)
        {
            var technical = _formatter.FormatTechnical(diagnosis);
            var messages = SplitTelegramMessage(technical)
                .Select(chunk => new EquipmentDiagnosticTelegramOutboundMessage(
                    chunk,
                    ParseMode: null,
                    DisableWebPagePreview: true,
                    MainKeyboard(access)))
                .ToArray();

            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                messages[0].Text,
                diagnosis.Warnings,
                messages[0].ReplyMarkup,
                messages);
        }

        var phoneSaved = access.User?.HasPhoneNumber == true;
        var text = _formatter.FormatConsumer(diagnosis, phoneSaved, ConsumerMessageLength);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            text,
            diagnosis.Warnings,
            MainKeyboard(access));
    }

    private TelegramDiagnosticConversationResult NotFound(TelegramUserAccessResult access)
    {
        var text = "Я не нашёл точную расшифровку этого кода. Проверьте код или укажите бренд, например: Gree H5.";
        if (access.UsesTechnicalResponse)
        {
            text += "\n\nТехническая заметка: код не найден в runtime catalog.";
        }

        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            text,
            [],
            MainKeyboard(access));
    }

    private TelegramDiagnosticConversationResult PromptForState(
        TelegramConversationSessionSnapshot session,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        TelegramUserAccessResult access) =>
        session.State switch
        {
            TelegramConversationState.WaitingForBrand =>
                PromptBrand(candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            TelegramConversationState.WaitingForEquipmentType =>
                PromptEquipmentType(candidates.Select(candidate => candidate.EquipmentType).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            TelegramConversationState.WaitingForDisplayContext =>
                PromptDisplayContext(candidates.Select(candidate => DisplayContextLabel(candidate.DisplayContext)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            _ => new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, [])
        };

    private static TelegramDiagnosticConversationResult PromptBrand(
        IReadOnlyList<string> manufacturers,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Этот код встречается у нескольких брендов. Выберите бренд оборудования.",
            [],
            ChoiceKeyboard(manufacturers, access));

    private static TelegramDiagnosticConversationResult PromptEquipmentType(
        IReadOnlyList<string> equipmentTypes,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Уточните тип оборудования, на котором показан код.",
            [],
            ChoiceKeyboard(equipmentTypes, access));

    private static TelegramDiagnosticConversationResult PromptDisplayContext(
        IReadOnlyList<string> displayContexts,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Уточните, где отображается код ошибки.",
            [],
            ChoiceKeyboard(displayContexts, access));

    public static EquipmentDiagnosticTelegramReplyMarkup MainKeyboard(TelegramUserAccessResult access)
    {
        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>>
        {
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton) }
        };

        if (access.Role == TelegramUserRole.Consumer && access.User?.HasPhoneNumber != true)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(SharePhoneButton, RequestContact: true)]);
        }

        return new EquipmentDiagnosticTelegramReplyMarkup(rows, ResizeKeyboard: true, OneTimeKeyboard: false);
    }

    private static EquipmentDiagnosticTelegramReplyMarkup ChoiceKeyboard(
        IReadOnlyList<string> options,
        TelegramUserAccessResult access)
    {
        var rows = options
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(option => option, StringComparer.Ordinal)
            .Select(option => (IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>)[new EquipmentDiagnosticTelegramKeyboardButton(option)])
            .ToList();

        rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(UnknownButton)]);
        rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton)]);

        if (access.Role == TelegramUserRole.Consumer && access.User?.HasPhoneNumber != true)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(SharePhoneButton, RequestContact: true)]);
        }

        return new EquipmentDiagnosticTelegramReplyMarkup(rows, ResizeKeyboard: true, OneTimeKeyboard: false);
    }

    private static IReadOnlyList<TelegramDiagnosticCandidate> ReadCandidates(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<TelegramDiagnosticCandidate[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

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

    private static string? FindMentionedManufacturer(
        string? text,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates)
    {
        var normalizedText = NormalizeText(text);
        return candidates
            .Select(candidate => candidate.Manufacturer)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(manufacturer => normalizedText.Contains(NormalizeText(manufacturer), StringComparison.OrdinalIgnoreCase));
    }

    private static string? SingleOrNull(IEnumerable<string> values)
    {
        var distinct = values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }

    private static string? MatchOption(string? text, IEnumerable<string> options)
    {
        if (IsUnknown(text))
        {
            return null;
        }

        var normalized = NormalizeText(text);
        return options.FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static string? MatchEquipmentType(string? text)
    {
        var normalized = NormalizeText(text);
        return EquipmentTypeOptions()
            .FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static string? MatchDisplayContext(string? text)
    {
        var normalized = NormalizeText(text);
        return DisplayContextOptions()
            .FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static bool IsUnknown(string? text) =>
        string.Equals(NormalizeText(text), NormalizeText(UnknownButton), StringComparison.Ordinal);

    private static string NormalizeText(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static IReadOnlyList<string> EquipmentTypeOptions() =>
        ["Внутренний блок", "Наружный блок", "Чиллер", "Контроллер/пульт", "Сплит/полупром"];

    private static IReadOnlyList<string> DisplayContextOptions() =>
        ["Пульт", "Внутренний блок", "Наружный блок", "Приложение/шлюз", "Плата/LED"];

    private static string EquipmentTypeLabel(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => "Внутренний блок",
            EquipmentCategory.VrfOutdoorUnit => "Наружный блок",
            EquipmentCategory.Chiller => "Чиллер",
            EquipmentCategory.Controller => "Контроллер/пульт",
            EquipmentCategory.SplitSystem => "Сплит/полупром",
            _ => category.ToString()
        };

    private static EquipmentDiagnosticBotEquipmentSide EquipmentSide(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotEquipmentSide.Indoor,
            EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotEquipmentSide.Outdoor,
            EquipmentCategory.Chiller => EquipmentDiagnosticBotEquipmentSide.Chiller,
            EquipmentCategory.Controller => EquipmentDiagnosticBotEquipmentSide.Controller,
            _ => EquipmentDiagnosticBotEquipmentSide.Unknown
        };

    private static EquipmentDiagnosticBotDisplayContext DisplayContext(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotDisplayContext.IduDisplay,
            EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotDisplayContext.OduMainBoardLed,
            EquipmentCategory.Controller => EquipmentDiagnosticBotDisplayContext.WiredController,
            _ => EquipmentDiagnosticBotDisplayContext.Unknown
        };

    private static string DisplayContextLabel(EquipmentDiagnosticBotDisplayContext context) =>
        context switch
        {
            EquipmentDiagnosticBotDisplayContext.WiredController => "Пульт",
            EquipmentDiagnosticBotDisplayContext.IduDisplay => "Внутренний блок",
            EquipmentDiagnosticBotDisplayContext.OduMainBoardLed => "Плата/LED",
            EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway => "Приложение/шлюз",
            _ => "Не указано"
        };
}
